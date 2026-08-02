using UnityEngine;

public enum CombatStyle
{
    Melee, // Lao vào tấn công
    Ranged // Đứng từ xa
}

[CreateAssetMenu(fileName = "NewCharacter", menuName = "RPG/Character")]
public class CharacterData : ScriptableObject
{
    [Header("Type & Role")]
    public CharacterType characterType = CharacterType.Player;

    [Header("Behavior")]
    public CombatStyle defaultCombatStyle = CombatStyle.Melee;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Identity")]
    public string characterName;
    
    [Tooltip("Ảnh đại diện (dùng trong UI roster, formation, v.v.)")]
    public Sprite portrait;
    
    [Tooltip("Ảnh chiến đấu (dùng trong battle scene)")]
    public Sprite battleSprite;
    
    [Tooltip("Ảnh nhân vật (dùng trong Character Info Panel) – có thể là full-body, illustration, v.v.")]
    public Sprite characterImage;
    
    [TextArea]
    public string lore;

    [Header("Identity Details")]
    [Tooltip("Danh hiệu của nhân vật (vd: 'Nhà thám hiểm', 'Hiệp sĩ ánh sáng')")]
    public string title;

    [Tooltip("Vai trò chiến đấu (vd: 'Chiến binh', 'Pháp sư', 'Cung thủ') – tự điền theo ý muốn")]
    public string role;

    [Tooltip("Animation clip dùng để preview trong UI (nếu có)")]
    public AnimationClip previewAnimation;

    [Header("Base Stats (Level 1)")]
    public int baseHP = 100;
    public int baseATK = 20;
    public int basePDEF = 10;
    public int baseMDEF = 10;

    [Header("Level Settings")]
    public int baseLevel = 1;
    public int expReward = 100;

    [Header("Level-Up Configuration")]
    public int baseExpThreshold = 100;
    public int expIncrementPerLevel = 50;

    [Header("Growth Per Level")]
    public int hpPerLevel = 5;
    public int atkPerLevel = 2;
    public int pdefPerLevel = 1;
    public int mdefPerLevel = 1;

    [Header("Skills")]
    public SkillData[] skills;

    [Header("Passive Script")]
    public Object passiveScript;

    [Header("Audio")]
    public AudioClip[] hitVoiceClips;
    public AudioClip[] attackVoiceClips;
    public AudioClip[] deathVoiceClips;
    public AudioClip footstepSound;

    [Header("Sprite Flip")]
    [Tooltip("Nếu true, sprite sẽ bị flip khi spawn (mặc định enemy bị flip).\n" +
             "Set false để giữ nguyên hướng sprite gốc.")]
    public bool flipOnSpawn = true;

    // ─── METHODS ─────────────────────────────────────────────────

    public int GetHP(int level) => baseHP + hpPerLevel * (level - 1);
    public int GetATK(int level) => baseATK + atkPerLevel * (level - 1);
    public int GetPDEF(int level) => basePDEF + pdefPerLevel * (level - 1);
    public int GetMDEF(int level) => baseMDEF + mdefPerLevel * (level - 1);

    /// <summary>Lấy danh hiệu, fallback nếu rỗng.</summary>
    public string GetTitleOrDefault(string fallback = "—")
    {
        return string.IsNullOrEmpty(title) ? fallback : title;
    }

    /// <summary>Lấy vai trò, fallback nếu rỗng.</summary>
    public string GetRoleOrDefault(string fallback = "—")
    {
        return string.IsNullOrEmpty(role) ? fallback : role;
    }
}