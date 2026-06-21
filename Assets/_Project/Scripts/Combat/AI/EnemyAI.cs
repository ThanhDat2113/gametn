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
        // Lấy danh sách skills từ AvailableSkills (đã clone) thay vì Data.skills
        var skillList = enemy.AvailableSkills;
        if (skillList == null || skillList.Count == 0) return null;

        // Kiểm tra nếu enemy có buff ATK (thường là do passive hoặc skill2 kích hoạt)
        bool hasAtkBuff = enemy.GetStatMultiplier(StatType.ATK) > 1f;

        // Nếu có buff ATK, ưu tiên skill cuối cùng (Skill 3 - Sword Saint Descends) 
        // bằng cách tăng trọng số
        if (hasAtkBuff && skillList.Count >= 3)
        {
            // 50% chọn skill cuối, 50% chia đều cho các skill còn lại
            if (Random.value < 0.5f)
                return skillList[skillList.Count - 1];
        }

        // Mặc định: random đều
        int chosenIndex = Random.Range(0, skillList.Count);
        return skillList[chosenIndex];
    }

    // ── Chọn target theo trọng số hàng ───────────────────────
    private List<CombatUnit> ChooseTargets(SkillData skill,
                                            List<CombatUnit> players,
                                            CombatUnit enemy)
    {
        var alive = players.Where(p => p.IsAlive).ToList();
        if (alive.Count == 0) return new List<CombatUnit>();

        // 1. Nếu enemy IgnoreTaunt, bỏ qua bước Taunt
        if (!enemy.IgnoreTaunt)
        {
            var tauntingUnits = alive.Where(p => p.HasStatus(StatusEffectType.Taunt)).ToList();
            if (tauntingUnits.Count > 0)
            {
                return new List<CombatUnit> { tauntingUnits[Random.Range(0, tauntingUnits.Count)] };
            }
        }

        // 2. Nếu không có ai Taunt (hoặc IgnoreTaunt), chọn như bình thường
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