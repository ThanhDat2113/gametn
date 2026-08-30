using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI của Madara Uchiha (Đã điều chỉnh tỉ lệ skill):
/// - Skill 1: Đấm đơn mục tiêu
/// - Skill 2: Mộc Độn + Stun 1 mục tiêu
/// - Skill 3: Thiêu đốt toàn bộ đội hình (TỐI ĐA 1 LẦN MỖI LƯỢT)
/// 
/// Phase 1 (HP > 50%): Skill1 40%, Skill2 40%, Skill3 15%, chờ 5%
/// Phase 2 (HP < 50%): Skill1 35%, Skill2 40%, Skill3 20%, chờ 5%
/// Phase 3 (Sau Izanagi): Skill1 30%, Skill2 40%, Skill3 25%, chờ 5%
/// Khi Skill 3 đã dùng trong lượt → bị khóa, phần tỉ lệ của nó dồn vào Skill 2.
/// </summary>
public class MadaraAI : EnemyAI
{
    // ── Giới hạn Skill 3 (AoE): tối đa 1 lần mỗi lượt ─────────
    // Counter được reset mỗi đầu lượt Madara qua MadaraPassive.OnTurnStart().
    // Định danh Skill 3 = skill có targetType AllEnemies (AoE toàn màn).
    private static int _skill3UsesThisTurn = 0;
    public static void NotifySkill3Used() => _skill3UsesThisTurn++;
    public static void ResetSkill3UsageThisTurn() => _skill3UsesThisTurn = 0;
    public static bool Skill3UsedThisTurn => _skill3UsesThisTurn > 0;

    public override void PlanTurn(CombatUnit enemy, List<CombatUnit> playerUnits)
    {
        var alive = playerUnits.Where(p => p.IsAlive).ToList();
        if (alive.Count == 0) return;

        SkillData skill = ChooseSkillByPhase(enemy);
        if (skill == null) return;

        List<CombatUnit> targets = ChooseTargetsByPhase(skill, alive, enemy);
        enemy.SelectSkill(skill, targets);

        // Ghi nhận lần dùng Skill 3 (AoE) → các action còn lại trong turn không được chọn lại
        if (skill.targetType == TargetType.AllEnemies) NotifySkill3Used();

        string targetNames = string.Join(", ", targets.Select(t => t.UnitName));
        Debug.Log($"[MadaraAI] Phase {(GetCurrentPhase(enemy))} - {enemy.UnitName} chuẩn bị [{skill.skillName}] → [{targetNames}]");
    }

    private int GetCurrentPhase(CombatUnit enemy)
    {
        if (enemy == null || !enemy.IsAlive) return 1;
        float hpPercent = (float)enemy.CurrentHP / enemy.MaxHP;

        // Kiểm tra Izanagi đã dùng chưa (Phase 3) - dựa vào MaxActionsPerTurn
        if (enemy.MaxActionsPerTurn >= 3) return 3;
        if (hpPercent < 0.50f) return 2;
        return 1;
    }

    private SkillData ChooseSkillByPhase(CombatUnit enemy)
    {
        var skillList = enemy.AvailableSkills;
        if (skillList == null || skillList.Count == 0) return null;

        int phase = GetCurrentPhase(enemy);
        float roll = Random.value;

        switch (phase)
        {
            case 1: // HP > 50%
                if (roll < 0.40f) return skillList[0]; // Skill 1 - 40%
                if (roll < (Skill3UsedThisTurn ? 0.95f : 0.80f)) return skillList[Mathf.Min(1, skillList.Count - 1)]; // Skill 2 - 40% (hoặc 55% khi Skill 3 đã khóa)
                if (!Skill3UsedThisTurn && roll < 0.95f) return skillList[Mathf.Min(2, skillList.Count - 1)]; // Skill 3 - 15%
                return null; // Chờ - 5%

            case 2: // HP 20-50%
                if (roll < 0.35f) return skillList[0]; // Skill 1 - 35%
                if (roll < (Skill3UsedThisTurn ? 0.95f : 0.75f)) return skillList[Mathf.Min(1, skillList.Count - 1)]; // Skill 2 - 40% (hoặc 60% khi Skill 3 đã khóa)
                if (!Skill3UsedThisTurn && roll < 0.95f) return skillList[Mathf.Min(2, skillList.Count - 1)]; // Skill 3 - 20%
                return null; // Chờ - 5%

            case 3: // HP < 20% - sau Izanagi
                if (roll < 0.30f) return skillList[0]; // Skill 1 - 30%
                if (roll < (Skill3UsedThisTurn ? 0.95f : 0.70f)) return skillList[Mathf.Min(1, skillList.Count - 1)]; // Skill 2 - 40% (hoặc 65% khi Skill 3 đã khóa)
                if (!Skill3UsedThisTurn && roll < 0.95f) return skillList[Mathf.Min(2, skillList.Count - 1)]; // Skill 3 - 25%
                return null; // Chờ - 5%

            default:
                return skillList[0];
        }
    }

    private List<CombatUnit> ChooseTargetsByPhase(SkillData skill, List<CombatUnit> alive, CombatUnit enemy)
    {
        if (alive.Count == 0) return new List<CombatUnit>();

        // Kiểm tra Taunt
        if (!enemy.IgnoreTaunt)
        {
            var taunting = alive.Where(p => p.HasStatus(StatusEffectType.Taunt)).ToList();
            if (taunting.Count > 0)
                return new List<CombatUnit> { taunting[Random.Range(0, taunting.Count)] };
        }

        // Skill 3 (Thiêu đốt AoE) - tất cả kẻ địch
        if (skill.targetType == TargetType.AllEnemies)
        {
            return alive;
        }

        // Skill 1 & 2 (đơn mục tiêu) - weighted random
        return new List<CombatUnit> { WeightedRandomTarget(alive) };
    }

    private CombatUnit WeightedRandomTarget(List<CombatUnit> units)
    {
        float[] RowWeights = { 0.15f, 0.25f, 0.60f };
        float total = units.Sum(u => RowWeights[Mathf.Clamp(u.GridRow, 0, RowWeights.Length - 1)]);
        float roll = Random.Range(0f, total);
        float running = 0f;

        foreach (var unit in units)
        {
            running += RowWeights[Mathf.Clamp(unit.GridRow, 0, RowWeights.Length - 1)];
            if (roll <= running) return unit;
        }

        return units[^1];
    }
}