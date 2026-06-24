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
[Tooltip("Chọn loại: Player (nhân vật của ta) hay Enemy (quái)")]
public CharacterType characterType = CharacterType.Player;

[Header("Behavior")]
    public CombatStyle defaultCombatStyle = CombatStyle.Melee;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Identity")]
    public string characterName;
    public Sprite portrait;
    public Sprite battleSprite;
    [TextArea]
    public string lore;

    [Header("Base Stats (Level 1)")]
    public int baseHP = 100;
    public int baseATK = 20;
    public int basePDEF = 10;
    public int baseMDEF = 10;
    public int baseSpeed = 10;

    [Header("Level Settings")]
[Tooltip("Level mặc định khi character này được tạo")]
public int baseLevel = 1;

[Tooltip("Chỉ dùng nếu characterType = Enemy. EXP mà toàn team nhận khi quái này bị đánh bại")]
public int expReward = 100;

[Header("Level-Up Configuration")]
[Tooltip("EXP cần để lên từ level 1→2")]
public int baseExpThreshold = 100;

[Tooltip("Mỗi level sẽ thêm thêm X EXP cần")]
public int expIncrementPerLevel = 50;

[Header("Growth Per Level")]
    public int hpPerLevel = 5;
    public int atkPerLevel = 2;
    public int pdefPerLevel = 1;
    public int mdefPerLevel = 1;
    public int speedPerLevel = 1;

    [Header("Skills (tối đa 5)")]
    [Tooltip("Kéo SkillData vào đây")]
    public SkillData[] skills;

    [Header("Passive Script")]
    [Tooltip("Kéo file .cs của passive vào đây (VD: AleusPassive.cs)")]
    public Object passiveScript;

    [Header("Audio")]
    [Tooltip("Giọng khi bị đánh (random)")]
    public AudioClip[] hitVoiceClips;
    [Tooltip("Giọng khi tấn công (random)")]
    public AudioClip[] attackVoiceClips;
    [Tooltip("Giọng khi chết")]
    public AudioClip[] deathVoiceClips;
    [Tooltip("Tiếng bước chân (overworld)")]
    public AudioClip footstepSound;

    // Tính stat theo level
    public int GetHP(int level) => baseHP + hpPerLevel * (level - 1);
    public int GetATK(int level) => baseATK + atkPerLevel * (level - 1);
    public int GetPDEF(int level) => basePDEF + pdefPerLevel * (level - 1);
    public int GetMDEF(int level) => baseMDEF + mdefPerLevel * (level - 1);
    public int GetSpeed(int level) => baseSpeed + speedPerLevel * (level - 1);
}