
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton quản lý việc tính toán và trao thưởng EXP sau khi chiến thắng.
/// </summary>
public class CombatExperienceManager : MonoBehaviour
{
    public static CombatExperienceManager Instance { get; private set; }

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
    /// Được gọi khi chiến thắng để bắt đầu quá trình trao thưởng EXP.
    /// </summary>
    public void OnVictory(List<CombatUnit> playerUnits, List<CombatUnit> enemyUnits)
    {
        int totalExp = CalculateTotalEXPFromEnemies(enemyUnits);
        if (totalExp > 0)
        {
            AwardEXPToPlayers(playerUnits, totalExp);
        }
        else
        {
            Debug.Log("[EXP] Không có EXP nào được thưởng từ trận này.");
        }
    }

    /// <summary>
    /// Tính tổng EXP thưởng từ tất cả kẻ địch bị đánh bại.
    /// </summary>
    private int CalculateTotalEXPFromEnemies(List<CombatUnit> enemyUnits)
    {
        int totalExp = 0;
        foreach (var enemy in enemyUnits)
        {
            if (enemy.Data.characterType == CharacterType.Enemy)
            {
                // Công thức: baseEXP + bonus cho mỗi level trên 1
                int baseExp = enemy.Data.expReward;
                int levelBonus = (enemy.Level - 1) * 10; // Ví dụ: +10 EXP cho mỗi level
                totalExp += baseExp + levelBonus;
            }
        }
        Debug.Log($"[EXP] Tổng EXP tính được từ kẻ địch: {totalExp}");
        return totalExp;
    }

    /// <summary>
    /// Phân phát tổng EXP cho các nhân vật người chơi còn sống.
    /// </summary>
    private void AwardEXPToPlayers(List<CombatUnit> playerUnits, int totalExp)
    {
        var alivePlayers = playerUnits.Where(p => p.IsAlive).ToList();
        if (alivePlayers.Count == 0) return;

        int expPerPlayer = totalExp / alivePlayers.Count;
        Debug.Log($"[EXP] Chia đều cho {alivePlayers.Count} người chơi, mỗi người nhận {expPerPlayer} EXP.");

        foreach (var playerUnit in alivePlayers)
        {
            var progress = PlayerProgressData.Instance.GetOrCreateProgress(playerUnit.Data);
            progress.GainEXP(expPerPlayer);
        }
    }

    /// <summary>
    /// Lấy thông tin chuỗi hiển thị EXP thưởng (dùng cho UI).
    /// </summary>
    public string GetEXPRewardInfo(List<CombatUnit> enemyUnits)
    {
        int totalExp = CalculateTotalEXPFromEnemies(enemyUnits);
        return $"EXP Reward: {totalExp}";
    }
}