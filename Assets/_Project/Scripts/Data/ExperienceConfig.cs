using UnityEngine;

[CreateAssetMenu(fileName = "ExperienceConfig", menuName = "RPG/Experience Config")]
public class ExperienceConfig : ScriptableObject
{
    [Header("Experience Configuration")]
    [Tooltip("Base experience needed to reach level 2")]
    public int baseExpToLevelUp = 100;

    [Tooltip("Multiplier for each level (exp *= multiplier per level)")]
    [Range(1f, 3f)]
    public float levelExpMultiplier = 1.2f;

    [Header("Maximum Level")]
    [Range(1, 100)]
    public int maxLevel = 50;

    [Header("Combat Rewards")]
    [Tooltip("Base experience gained per enemy defeated")]
    public int baseEnemyDefeatExp = 50;

    [Tooltip("Experience bonus per enemy level")]
    public int expPerEnemyLevel = 10;

    [Header("Quest Rewards")]
    [Tooltip("Base experience from quest completion")]
    public int baseQuestExp = 200;

    /// <summary>
    /// Calculate experience needed to reach a specific level (from level 1)
    /// </summary>
    public int GetTotalExpForLevel(int level)
    {
        if (level <= 1) return 0;

        int totalExp = 0;
        for (int i = 2; i <= level; i++)
        {
            totalExp += GetExpNeededForLevelUp(i - 1);
        }
        return totalExp;
    }

    /// <summary>
    /// Calculate experience needed to go from one level to the next
    /// </summary>
    public int GetExpNeededForLevelUp(int currentLevel)
    {
        if (currentLevel < 1) return 0;
        return Mathf.RoundToInt(baseExpToLevelUp * Mathf.Pow(levelExpMultiplier, currentLevel - 1));
    }

    /// <summary>
    /// Calculate experience reward for defeating enemy
    /// </summary>
    public int GetExpForEnemyDefeat(int enemyLevel)
    {
        return baseEnemyDefeatExp + (enemyLevel * expPerEnemyLevel);
    }
}
