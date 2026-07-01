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
    public Sprite portrait;
    public Sprite battleSprite;
    [TextArea]
    public string lore;

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

    // Tính stat theo level
    public int GetHP(int level) => baseHP + hpPerLevel * (level - 1);
    public int GetATK(int level) => baseATK + atkPerLevel * (level - 1);
    public int GetPDEF(int level) => basePDEF + pdefPerLevel * (level - 1);
    public int GetMDEF(int level) => baseMDEF + mdefPerLevel * (level - 1);
}