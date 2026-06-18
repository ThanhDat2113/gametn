using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAI
{
    // Tỉ lệ bị nhắm theo hàng: Front=60%, Mid=25%, Back=15%
    // GridPosition.Row: 0=back, 1=mid, 2=front
    private static readonly float[] RowWeights = { 0.15f, 0.25f, 0.60f };

    // Chọn skill và target cho 1 kẻ địch
    public void PlanTurn(CombatUnit enemy, List<CombatUnit> playerUnits)
    {
        SkillData skill = ChooseSkill(enemy);
        if (skill == null) return;

        List<CombatUnit> targets = ChooseTargets(skill, playerUnits, enemy);
        enemy.SelectSkill(skill, targets);

        string targetNames = string.Join(", ", targets.Select(t => t.UnitName));
        Debug.Log($"[AI] {enemy.UnitName} chuẩn bị [{skill.skillName}] → [{targetNames}]");
    }

    // ── Chọn skill sẵn sàng ───────────────────────────────────
    private SkillData ChooseSkill(CombatUnit enemy)
    {
        // AI giờ sẽ chọn ngẫu nhiên từ tất cả các skill có sẵn
        if (enemy.Data.skills.Length == 0) return null;

        int chosenIndex = Random.Range(0, enemy.Data.skills.Length);
        return enemy.Data.skills[chosenIndex];
    }

    // ── Chọn target theo trọng số hàng ───────────────────────
    private List<CombatUnit> ChooseTargets(SkillData skill,
                                            List<CombatUnit> players,
                                            CombatUnit enemy)
    {
        var alive = players.Where(p => p.IsAlive).ToList();
        if (alive.Count == 0) return new List<CombatUnit>();

        // 1. Ưu tiên mục tiêu bị Taunt
        var tauntingUnits = alive.Where(p => p.HasStatus(StatusEffectType.Taunt)).ToList();
        if (tauntingUnits.Count > 0)
        {
            // Nếu có nhiều mục tiêu Taunt, chọn ngẫu nhiên trong số đó
            return new List<CombatUnit> { tauntingUnits[Random.Range(0, tauntingUnits.Count)] };
        }

        // 2. Nếu không có ai Taunt, chọn như bình thường
        switch (skill.targetType)
        {
            case TargetType.AllEnemies:  // từ góc nhìn AI, "enemy" = player
                return alive;

            case TargetType.SingleEnemy:
            default:
                return new List<CombatUnit> { WeightedRandomTarget(alive) };
        }
    }

    // Weighted random dựa vào GridPosition.Row
    private CombatUnit WeightedRandomTarget(List<CombatUnit> units)
    {
        float total = units.Sum(u => RowWeights[
            Mathf.Clamp(u.GridRow, 0, RowWeights.Length - 1)]);

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