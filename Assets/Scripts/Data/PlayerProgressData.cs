
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lưu trữ và quản lý tiến trình (level, EXP) của một nhân vật cụ thể.
/// </summary>
public class CharacterProgress
{
    public CharacterData Data { get; }
    public int CurrentLevel { get; private set; }
    public int CurrentEXP { get; private set; }

    private readonly List<int> _levelUpThresholds = new();

    public event System.Action<int, int> OnEXPGained; // newEXP, newTotalEXP
    public event System.Action<int> OnLevelUp; // newLevel

    public CharacterProgress(CharacterData data)
    {
        Data = data;
        CurrentLevel = data.baseLevel;
        CurrentEXP = 0;
        GenerateLevelUpThresholds();
    }

    /// <summary>
    /// Tính toán và cache lại lượng EXP cần cho mỗi cấp.
    /// </summary>
    private void GenerateLevelUpThresholds()
    {
        _levelUpThresholds.Clear();
        // Thêm một giá trị placeholder cho level 1
        _levelUpThresholds.Add(0); 
        for (int i = 2; i <= 100; i++) // Giả sử max level là 100
        {
            int threshold = Data.baseExpThreshold + Data.expIncrementPerLevel * (i - 2);
            _levelUpThresholds.Add(threshold);
        }
    }

    /// <summary>
    /// Nhận một lượng EXP và kiểm tra xem có lên cấp không.
    /// </summary>
    public void GainEXP(int amount)
    {
        if (amount <= 0) return;

        CurrentEXP += amount;
        OnEXPGained?.Invoke(amount, CurrentEXP);
        Debug.Log($"[Progress] {Data.characterName} nhận được {amount} EXP. Tổng EXP: {CurrentEXP}.");

        CheckLevelUp();
    }

    /// <summary>
    /// Tự động lên cấp nếu đủ EXP. Có thể lên nhiều cấp một lúc.
    /// </summary>
    private void CheckLevelUp()
    {
        while (CurrentLevel < _levelUpThresholds.Count && CurrentEXP >= GetEXPToNextLevel())
        {
            int expNeeded = GetEXPToNextLevel();
            CurrentEXP -= expNeeded;
            CurrentLevel++;
            OnLevelUp?.Invoke(CurrentLevel);
            Debug.Log($"<color=green>[LEVEL UP!] {Data.characterName} đã lên Level {CurrentLevel}!</color>");
        }
    }

    /// <summary>
    /// Lấy lượng EXP cần để lên cấp tiếp theo.
    /// </summary>
    public int GetEXPToNextLevel()
    {
        if (CurrentLevel >= _levelUpThresholds.Count)
        {
            return int.MaxValue; // Đã max level
        }
        return _levelUpThresholds[CurrentLevel];
    }
    
    /// <summary>
    /// Thiết lập cấp độ và EXP trực tiếp (dùng cho debug hoặc load game).
    /// </summary>
    public void SetLevel(int newLevel, int newExp = 0)
    {
        CurrentLevel = Mathf.Clamp(newLevel, 1, 100);
        CurrentEXP = newExp;
        Debug.Log($"[Progress] {Data.characterName} được set thành Level {CurrentLevel} với {CurrentEXP} EXP.");
        CheckLevelUp();
    }

    /// <summary>
    /// Reset tiến trình về trạng thái ban đầu.
    /// </summary>
    public void Reset()
    {
        CurrentLevel = Data.baseLevel;
        CurrentEXP = 0;
    }
}

/// <summary>
/// Singleton quản lý tiến trình của tất cả các nhân vật trong game.
/// Dữ liệu này sẽ không bị hủy khi chuyển scene.
/// </summary>
public class PlayerProgressData : MonoBehaviour
{
    public static PlayerProgressData Instance { get; private set; }

    private readonly Dictionary<CharacterData, CharacterProgress> _characterProgressDict = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lấy hoặc tạo mới tiến trình cho một nhân vật.
    /// </summary>
    public CharacterProgress GetOrCreateProgress(CharacterData characterData)
    {
        if (!_characterProgressDict.ContainsKey(characterData))
        {
            _characterProgressDict[characterData] = new CharacterProgress(characterData);
        }
        return _characterProgressDict[characterData];
    }

    /// <summary>
    /// Thiết lập tiến trình cho một nhân vật (dùng khi load game).
    /// </summary>
    public void SetProgress(CharacterData characterData, int level, int exp)
    {
        var progress = GetOrCreateProgress(characterData);
        progress.SetLevel(level, exp);
    }

    /// <summary>
    /// Reset tiến trình của tất cả các nhân vật (dùng khi bắt đầu game mới).
    /// </summary>
    public void ResetAll()
    {
        foreach (var progress in _characterProgressDict.Values)
        {
            progress.Reset();
        }
        Debug.Log("[PlayerProgressData] Đã reset tiến trình của tất cả nhân vật.");
    }

    /// <summary>
    /// Lấy toàn bộ dữ liệu tiến trình (dùng để lưu game).
    /// </summary>
    public Dictionary<CharacterData, CharacterProgress> GetAllProgress()
    {
        return _characterProgressDict;
    }

    // Các phương thức Import/Export có thể được thêm vào sau để lưu/tải game
    // public List<SerializableProgress> ExportProgress() { ... }
    // public void ImportProgress(List<SerializableProgress> data) { ... }
}