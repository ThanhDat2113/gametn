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
        var ready = new List<(int index, SkillData skill)>();

        for (int i = 0; i < enemy.Data.skills.Length; i++)
        {
            if (enemy.IsSkillReady(i))
                ready.Add((i, enemy.Data.skills[i]));
        }

        if (ready.Count == 0) return null;

        // Random trong các skill sẵn sàng
        var chosen = ready[Random.Range(0, ready.Count)];
        return chosen.skill;
    }

    // ── Chọn target theo trọng số hàng ───────────────────────
    private List<CombatUnit> ChooseTargets(SkillData skill,
                                            List<CombatUnit> players,
                                            CombatUnit enemy)
    {
        var alive = players.Where(p => p.IsAlive).ToList();
        if (alive.Count == 0) return new List<CombatUnit>();

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