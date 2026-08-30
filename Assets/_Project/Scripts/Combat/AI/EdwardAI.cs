using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI của Edward (Boss 2) — tăng tần suất Skill 3 (AoE):
/// - Skill 1 "Automail Strike" (đơn mục tiêu) — 30%
/// - Skill 2 "Alchemy Blast"  (đơn mục tiêu) — 30%
/// - Skill 3 "Stone Wall"     (AoE toàn team) — 40%  (trước đây random đều ≈ 33%)
/// Đơn mục tiêu: tôn trọng Taunt → weighted theo hàng (Front 60% / Mid 25% / Back 15%).
/// Skill 3 (AoE) → toàn bộ player còn sống.
/// </summary>
public class EdwardAI : EnemyAI
{
    private const float SKILL3_WEIGHT = 0.40f; // Stone Wall (AoE) — tăng từ ~33% lên 40%
    private const float SKILL1_WEIGHT = 0.30f; // Automail Strike
    private const float SKILL2_WEIGHT = 0.30f; // Alchemy Blast

    // RowWeights: GridRow 0=back, 1=mid, 2=front
    private static readonly float[] RowWeights = { 0.15f, 0.25f, 0.60f };

    public override void PlanTurn(CombatUnit enemy, List<CombatUnit> playerUnits)
    {
        var alive = playerUnits.Where(p => p.IsAlive).ToList();
        if (alive.Count == 0) return;

        SkillData skill = ChooseSkillWeighted(enemy);
        if (skill == null) return;

        List<CombatUnit> targets = ChooseTargets(skill, alive, enemy);
        enemy.SelectSkill(skill, targets);

        string targetNames = string.Join(", ", targets.Select(t => t.UnitName));
        Debug.Log($"[EdwardAI] {enemy.UnitName} chuẩn bị [{skill.skillName}] → [{targetNames}]");
    }

    private SkillData ChooseSkillWeighted(CombatUnit enemy)
    {
        var skillList = enemy.AvailableSkills;
        if (skillList == null || skillList.Count == 0) return null;

        float roll = Random.value;
        if (roll < SKILL3_WEIGHT && skillList.Count >= 3)
            return skillList[2];                                // Skill 3 AoE — 40%
        if (roll < SKILL3_WEIGHT + SKILL1_WEIGHT || skillList.Count < 2)
            return skillList[0];                                // Skill 1 — 30%
        return skillList[Mathf.Min(1, skillList.Count - 1)];    // Skill 2 — 30%
    }

    private List<CombatUnit> ChooseTargets(SkillData skill, List<CombatUnit> alive, CombatUnit enemy)
    {
        if (alive.Count == 0) return new List<CombatUnit>();

        // AoE → toàn bộ player còn sống
        if (skill.targetType == TargetType.AllEnemies)
            return alive;

        // Đơn mục tiêu: tôn trọng Taunt
        if (!enemy.IgnoreTaunt)
        {
            var taunting = alive.Where(p => p.HasStatus(StatusEffectType.Taunt)).ToList();
            if (taunting.Count > 0)
                return new List<CombatUnit> { taunting[Random.Range(0, taunting.Count)] };
        }

        return new List<CombatUnit> { WeightedRandomTarget(alive) };
    }

    private CombatUnit WeightedRandomTarget(List<CombatUnit> units)
    {
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