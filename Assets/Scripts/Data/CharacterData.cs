using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "RPG/Character")]
public class CharacterData : ScriptableObject
{
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
    public int baseLuck = 10;

    [Header("Growth Per Level")]
    public int hpPerLevel = 5;
    public int atkPerLevel = 2;
    public int pdefPerLevel = 1;
    public int mdefPerLevel = 1;
    public int luckPerLevel = 1;

    [Header("Skills (tối đa 5)")]
    [Tooltip("Kéo SkillData vào đây")]
    public SkillData[] skills;

    // Tính stat theo level
    public int GetHP(int level) => baseHP + hpPerLevel * (level - 1);
    public int GetATK(int level) => baseATK + atkPerLevel * (level - 1);
    public int GetPDEF(int level) => basePDEF + pdefPerLevel * (level - 1);
    public int GetMDEF(int level) => baseMDEF + mdefPerLevel * (level - 1);
    public int GetLuck(int level) => baseLuck + luckPerLevel * (level - 1);
}