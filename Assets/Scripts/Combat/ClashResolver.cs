using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Lớp chứa kết quả của một hành động
public class ActionResult
{
    public CombatUnit Actor { get; set; }
    public SkillData Skill { get; set; }
    public List<CombatUnit> InitialTargets { get; set; }
    public List<ActionOutcome> Outcomes { get; set; } = new List<ActionOutcome>();

    /// <summary>
    /// Áp dụng outcomes damage vào target (fallback khi không có animation)
    /// </summary>
    public void ApplyOutcomes()
    {
        foreach (var outcome in Outcomes)
        {
            if (outcome.Target.IsAlive)
            {
                outcome.Target.TakeDamage(Actor, outcome.Damage);
                string logMessage = $"{outcome.Target.UnitName} takes {outcome.Damage} damage.";
                if (outcome.EmpowerMultiplier > 1f)
                {
                    logMessage += $" ({outcome.EmpowerMultiplier:F1}x)";
                }
                logMessage += $" HP left: {outcome.Target.CurrentHP}";
                Debug.Log(logMessage);
            }
        }
    }

    public void ApplyNonDamageOutcomes()
    {
        Debug.Log("[ActionResult] Applying non-damage outcomes (Not Implemented).");
    }
}

// Lớp con chứa kết quả cho một mục tiêu cụ thể
public class ActionOutcome
{
    public CombatUnit Target { get; set; }
    public int Damage { get; set; }
    public float EmpowerMultiplier { get; set; } = 1f;
}

/// <summary>
/// ActionResolver: tính toán damage preview và lưu vào Outcomes.
/// </summary>
public class ActionResolver
{
    public ActionResult Resolve(CombatUnit actor, SkillData skill, List<CombatUnit> targets)
    {
        var result = new ActionResult
        {
            Actor = actor,
            Skill = skill,
            InitialTargets = targets
        };

        Debug.Log($"[ActionResolver] {actor.UnitName} uses '{skill.skillName}' on {targets.Count} target(s).");

        // Lấy hệ số nhân sát thương từ Empowered stacks
        float empowerMultiplier = actor.GetEmpowerMultiplier();
        if (empowerMultiplier > 1f)
        {
            Debug.Log($"[{actor.UnitName}] được cường hóa! Sát thương x{empowerMultiplier}.");
        }

        // Populate Outcomes từ skill effects
        bool hasEffects = skill.effects != null && skill.effects.Length > 0;
        if (hasEffects)
        {
            foreach (var effect in skill.effects)
            {
                if (effect is DamageEffect damageEffect)
                {
                    foreach (var target in targets)
                    {
                        if (!target.IsAlive) continue;
                        // Tính tổng damage (hitCount = 1 để lấy tổng)
                        var hits = damageEffect.CalculateHits(actor, target, 1);
                        foreach (var hit in hits)
                        {
                            int finalDamage = Mathf.RoundToInt(hit.Damage * empowerMultiplier);
                            result.Outcomes.Add(new ActionOutcome
                            {
                                Target = target,
                                Damage = finalDamage,
                                EmpowerMultiplier = empowerMultiplier
                            });
                            if (empowerMultiplier > 1f)
                            {
                                Debug.Log($"  -> Sát thương gốc: {hit.Damage}, Sát thương cường hóa: {finalDamage}");
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // Fallback: nếu skill không có effects, dùng công thức ATK - PDEF
            Debug.LogWarning($"[ActionResolver] Skill '{skill.skillName}' không có effects! Dùng công thức fallback.");
            foreach (var target in targets)
            {
                if (!target.IsAlive) continue;
                int damage = Mathf.Max(1, actor.ATK - target.PDEF);
                int finalDamage = Mathf.RoundToInt(damage * empowerMultiplier);
                result.Outcomes.Add(new ActionOutcome
                {
                    Target = target,
                    Damage = finalDamage
                });
                if (empowerMultiplier > 1f)
                {
                    Debug.Log($"  -> Sát thương gốc: {damage}, Sát thương cường hóa: {finalDamage}");
                }
            }
        }

        // Tiêu thụ Empowered stacks sau khi tính toán xong
        if (empowerMultiplier > 1f)
        {
            actor.ClearEmpowerStacks();
        }

        return result;
    }
}