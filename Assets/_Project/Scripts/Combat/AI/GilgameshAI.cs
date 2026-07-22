using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI của Gilgamesh (Đã điều chỉnh):
/// - Skill 1: Gate of Babylon - tấn công 1 mục tiêu
/// - Skill 2: Enkidu - làm choáng 1 đối tượng
/// - Skill 3: Enuma Elish - gây sát thương cho toàn đội hình
/// 
/// Phase 1 (HP > 40%): Skill1 45%, Skill2 35%, Skill3 15%, chờ 5%
/// Phase 2 (HP < 40%): Skill1 40%, Skill2 40%, Skill3 15%, chờ 5%
/// </summary>
public class GilgameshAI : EnemyAI
{
    public override void PlanTurn(CombatUnit enemy, List<CombatUnit> playerUnits)
    {
        var alive = playerUnits.Where(p => p.IsAlive).ToList();
        if (alive.Count == 0) return;

        SkillData skill = ChooseSkillByPhase(enemy);
        if (skill == null) return;

        List<CombatUnit> targets = ChooseTargetsByPhase(skill, alive, enemy);
        enemy.SelectSkill(skill, targets);

        string targetNames = string.Join(", ", targets.Select(t => t.UnitName));
        Debug.Log($"[GilgameshAI] Phase {(GetCurrentPhase(enemy))} - {enemy.UnitName} chuẩn bị [{skill.skillName}] → [{targetNames}]");
    }

    private int GetCurrentPhase(CombatUnit enemy)
    {
        if (enemy == null || !enemy.IsAlive) return 1;
        float hpPercent = (float)enemy.CurrentHP / enemy.MaxHP;
        return hpPercent < 0.40f ? 2 : 1;
    }

    private SkillData ChooseSkillByPhase(CombatUnit enemy)
    {
        var skillList = enemy.AvailableSkills;
        if (skillList == null || skillList.Count == 0) return null;

        int phase = GetCurrentPhase(enemy);
        float roll = Random.value;

        switch (phase)
        {
            case 1: // HP > 40%
                if (roll < 0.45f) return skillList[0]; // Skill 1 - 45%
                if (roll < 0.80f) return skillList[Mathf.Min(1, skillList.Count - 1)]; // Skill 2 - 35%
                if (roll < 0.95f) return skillList[Mathf.Min(2, skillList.Count - 1)]; // Skill 3 - 15%
                return null; // Chờ - 5%

            case 2: // HP < 40%
                if (roll < 0.40f) return skillList[0]; // Skill 1 - 40%
                if (roll < 0.80f) return skillList[Mathf.Min(1, skillList.Count - 1)]; // Skill 2 - 40%
                if (roll < 0.95f) return skillList[Mathf.Min(2, skillList.Count - 1)]; // Skill 3 - 15%
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

        // Skill 3 (Enuma Elish - AoE) - tất cả kẻ địch
        if (skill.targetType == TargetType.AllEnemies)
        {
            return alive;
        }

        // Skill 2 (Enkidu - Stun) - ưu tiên player hàng sau (healer/mage)
        if (enemy.AvailableSkills != null && enemy.AvailableSkills.Count >= 2 && skill == enemy.AvailableSkills[1])
        {
            var backRow = alive.Where(p => p.GridRow == 0).ToList();
            if (backRow.Count > 0)
                return new List<CombatUnit> { backRow[Random.Range(0, backRow.Count)] };
        }

        // Skill 1 (tấn công thường) - weighted random ưu tiên back row
        return new List<CombatUnit> { WeightedRandomTarget(alive) };
    }

    private CombatUnit WeightedRandomTarget(List<CombatUnit> units)
    {
        // Gilgamesh ưu tiên back row hơn (healer/mage)
        float[] RowWeights = { 0.40f, 0.35f, 0.25f }; // Back=40%, Mid=35%, Front=25%
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