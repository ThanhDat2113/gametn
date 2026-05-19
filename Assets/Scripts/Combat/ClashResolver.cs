using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Lớp chứa kết quả của một hành động, thay thế cho ClashResult
public class ActionResult
{
    public CombatUnit Actor { get; set; }
    public SkillData Skill { get; set; }
    public List<CombatUnit> InitialTargets { get; set; }
    public List<ActionOutcome> Outcomes { get; set; } = new List<ActionOutcome>();

    // Phương thức này sẽ áp dụng tất cả các kết quả đã được tính toán
    public void ApplyOutcomes()
    {
        foreach (var outcome in Outcomes)
        {
            if (outcome.Target.IsAlive)
            {
                outcome.Target.TakeDamage(Actor, outcome.Damage);
                Debug.Log($"{outcome.Target.UnitName} takes {outcome.Damage} damage. HP left: {outcome.Target.CurrentHP}");
            }
        }
    }

    // Áp dụng các hiệu ứng không phải sát thương (ví dụ: buff, debuff)
    public void ApplyNonDamageOutcomes()
    {
        // Hiện tại chưa có logic này, nhưng để sẵn cấu trúc
        Debug.Log("[ActionResult] Applying non-damage outcomes (Not Implemented).");
    }
}

// Lớp con chứa kết quả cho một mục tiêu cụ thể
public class ActionOutcome
{
    public CombatUnit Target { get; set; }
    public int Damage { get; set; }
    // Có thể thêm các hiệu ứng, hồi máu, v.v. ở đây
}


// Đổi tên từ ClashResolver thành ActionResolver
public class ActionResolver
{
    // Viết lại phương thức Resolve để phù hợp với turn-based
    public ActionResult Resolve(CombatUnit actor, SkillData skill, List<CombatUnit> targets)
    {
        var result = new ActionResult
        {
            Actor = actor,
            Skill = skill,
            InitialTargets = targets
        };

        Debug.Log($"[ActionResolver] {actor.UnitName} uses '{skill.skillName}' on {targets.Count} target(s).");

        foreach (var target in targets)
        {
            if (!target.IsAlive) continue;

            // Logic tính sát thương cơ bản
            // TODO: Mở rộng với các loại sát thương, hiệu ứng, v.v.
            int damage = Mathf.Max(1, actor.ATK - target.PDEF);

            result.Outcomes.Add(new ActionOutcome
            {
                Target = target,
                Damage = damage
            });
        }

        return result;
    }
}