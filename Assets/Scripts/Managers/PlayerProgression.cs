using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Quản lý cấp độ và kinh nghiệm cho từng nhân vật trong team.
/// Singleton — tự tạo nếu chưa có trong scene.
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    [Header("Experience Config")]
    [SerializeField] private ExperienceConfig expConfig;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Lưu level & exp riêng cho từng nhân vật (key = characterName)
    private Dictionary<string, int> characterLevels = new Dictionary<string, int>();
    private Dictionary<string, int> characterExp = new Dictionary<string, int>();

    // Events
    public event Action<CharacterData, int> OnCharacterLevelUp; // (character, newLevel)
    public event Action<CharacterData, int> OnExperienceGained; // (character, amountGained)

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (expConfig == null)
        {
            // Tự động load ExperienceConfig từ Resources nếu chưa gán
            expConfig = Resources.Load<ExperienceConfig>("ExperienceConfig");
            if (expConfig == null)
            {
                Debug.LogWarning("[PlayerProgression] Không tìm thấy ExperienceConfig asset! Tạo instance mặc định.");
                expConfig = ScriptableObject.CreateInstance<ExperienceConfig>();
            }
        }
    }

    /// <summary>Đặt lại toàn bộ dữ liệu progression.</summary>
    public void ResetAllProgress()
    {
        characterLevels.Clear();
        characterExp.Clear();
        Debug.Log("[PlayerProgression] Đã reset toàn bộ tiến trình.");
    }

    /// <summary>Lấy level hiện tại của nhân vật (mặc định 1).</summary>
    public int GetLevel(CharacterData character)
    {
        if (character == null) return 1;
        string key = character.characterName;
        if (!characterLevels.ContainsKey(key))
        {
            characterLevels[key] = character.baseLevel;
            characterExp[key] = 0;
        }
        return characterLevels[key];
    }

    /// <summary>Lấy exp hiện tại của nhân vật.</summary>
    public int GetCurrentExp(CharacterData character)
    {
        if (character == null) return 0;
        string key = character.characterName;
        if (!characterExp.ContainsKey(key))
        {
            characterLevels[key] = 1;
            characterExp[key] = 0;
        }
        return characterExp[key];
    }

    /// <summary>Lấy tổng exp cần để lên cấp tiếp theo.</summary>
    public int GetExpToNextLevel(CharacterData character)
    {
        if (character == null) return 0;
        int currentLevel = GetLevel(character);
        // Dùng per-character thresholds từ CharacterData nếu có
        if (character.baseExpThreshold > 0)
        {
            if (currentLevel <= 1) return character.baseExpThreshold;
            return character.baseExpThreshold + character.expIncrementPerLevel * (currentLevel - 1);
        }
        // Fallback về ExperienceConfig
        if (expConfig != null) return expConfig.GetExpNeededForLevelUp(currentLevel);
        return 100;
    }

    /// <summary>Tiến trình exp (0.0 → 1.0) để lên cấp tiếp theo.</summary>
    public float GetLevelProgress(CharacterData character)
    {
        if (character == null || expConfig == null) return 0f;
        int needed = GetExpToNextLevel(character);
        if (needed <= 0) return 1f;
        return (float)GetCurrentExp(character) / needed;
    }

    /// <summary>
    /// Cộng exp cho một nhân vật cụ thể.
    /// Tự động xử lý level-up nếu đủ exp.
    /// </summary>
    public void AddExperience(CharacterData character, int amount)
    {
        if (character == null || amount <= 0) return;

        string key = character.characterName;

        // Khởi tạo nếu chưa có
        if (!characterLevels.ContainsKey(key))
        {
            characterLevels[key] = 1;
            characterExp[key] = 0;
        }

        characterExp[key] += amount;
        OnExperienceGained?.Invoke(character, amount);

        Debug.Log($"[Exp] <color=yellow>{character.characterName}</color> nhận <color=#FFD700>{amount}</color> EXP. Tổng: <color=cyan>{characterExp[key]}</color> (Level {characterLevels[key]})");

        // Xử lý level-up (có thể lên nhiều cấp cùng lúc)
        int newLevel = ProcessLevelUps(character);

        // Cập nhật level cho CombatManager nếu đang trong combat
        if (newLevel > 0)
        {
            UpdateCombatUnitLevel(character, newLevel);
        }
    }

    /// <summary>
    /// Cộng exp đồng đều cho toàn bộ party (alive units).
    /// </summary>
    public void AddPartyExperience(int totalExp)
    {
        if (totalExp <= 0) return;

        // Lấy danh sách player units hiện tại (nếu đang trong combat)
        List<CharacterData> partyMembers = new List<CharacterData>();

        if (CombatManager.Instance != null)
        {
            // Trong combat: chỉ cộng cho unit còn sống
            partyMembers = CombatManager.Instance.PlayerUnits
                .Where(u => u.IsAlive)
                .Select(u => u.Data)
                .Where(d => d != null)
                .ToList();
        }

        if (partyMembers.Count == 0)
        {
            Debug.LogWarning("[PlayerProgression] Không có party members để cộng exp!");
            return;
        }

        int expPerMember = totalExp / partyMembers.Count;
        if (expPerMember <= 0) expPerMember = totalExp;

        foreach (var member in partyMembers)
        {
            AddExperience(member, expPerMember);
        }

        Log($"[Exp] Party nhận {totalExp} EXP → chia {expPerMember} cho {partyMembers.Count} thành viên.");
    }

    /// <summary>Xử lý level-up, trả về level mới nếu có thay đổi, -1 nếu không.</summary>
    private int ProcessLevelUps(CharacterData character)
    {
        if (character == null) return -1;

        string key = character.characterName;
        int currentLevel = characterLevels[key];
        int newLevel = currentLevel;

        // maxLevel từ CharacterData hoặc ExperienceConfig
        int maxLevel = 50;
        if (expConfig != null) maxLevel = expConfig.maxLevel;

        while (newLevel < maxLevel)
        {
            int needed = GetExpToNextLevel(character, newLevel);
            if (characterExp[key] >= needed)
            {
                characterExp[key] -= needed;
                newLevel++;
                Debug.Log($"[Level Up] <color=green>{character.characterName}</color> lên cấp <color=yellow>{newLevel}</color>! Exp còn: <color=cyan>{characterExp[key]}</color> (cần {GetExpToNextLevel(character, newLevel)} cho cấp tiếp)");

                // Trigger event
                OnCharacterLevelUp?.Invoke(character, newLevel);
            }
            else
            {
                break;
            }
        }

        characterLevels[key] = newLevel;

        return (newLevel != currentLevel) ? newLevel : -1;
    }

    /// <summary>Overload: tính exp cần để lên từ một level cụ thể.</summary>
    private int GetExpToNextLevel(CharacterData character, int fromLevel)
    {
        if (character == null) return 100;
        if (character.baseExpThreshold > 0)
        {
            if (fromLevel <= 1) return character.baseExpThreshold;
            return character.baseExpThreshold + character.expIncrementPerLevel * (fromLevel - 1);
        }
        if (expConfig != null) return expConfig.GetExpNeededForLevelUp(fromLevel);
        return 100;
    }

    /// <summary>
    /// Cập nhật level cho CombatUnit tương ứng nếu đang trong combat.
    /// </summary>
    private void UpdateCombatUnitLevel(CharacterData character, int newLevel)
    {
        if (CombatManager.Instance == null) return;

        var unit = CombatManager.Instance.PlayerUnits
            .FirstOrDefault(u => u.Data == character || u.UnitName == character.characterName);

        if (unit != null)
        {
            Debug.Log($"[PlayerProgression] {character.characterName} lên cấp {newLevel} trong combat!");
            // Ghi nhận level mới (CombatUnit sẽ refresh stats ở lần combat sau)
        }
    }

    /// <summary>Lưu dữ liệu progression.</summary>
    public void SaveProgress()
    {
        // Sử dụng PlayerPrefs để lưu đơn giản (có thể nâng cấp sau)
        foreach (var kvp in characterLevels)
        {
            PlayerPrefs.SetInt($"Prog_Level_{kvp.Key}", kvp.Value);
            PlayerPrefs.SetInt($"Prog_Exp_{kvp.Key}", characterExp.ContainsKey(kvp.Key) ? characterExp[kvp.Key] : 0);
        }
        PlayerPrefs.Save();
        Log("[Progression] Đã lưu tiến trình.");
    }

    /// <summary>Tải dữ liệu progression.</summary>
    public void LoadProgress()
    {
        characterLevels.Clear();
        characterExp.Clear();
        // Dữ liệu sẽ được load khi GetLevel/GetCurrentExp được gọi lần đầu
        Log("[Progression] Đã tải tiến trình.");
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void Log(string message)
    {
        if (debugMode) Debug.Log(message);
    }
}