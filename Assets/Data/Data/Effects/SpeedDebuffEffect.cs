using UnityEngine;

[CreateAssetMenu(fileName = "AtkDebuffEffect", menuName = "RPG/Effects/ATK Debuff")]
public class AtkDebuffEffect : SkillEffect
{
    public float reductionMultiplier = 0.8f;
    public int duration = 2;

public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyBuff(StatType.ATK, reductionMultiplier, duration);

            // Kích hoạt sự kiện debuff cho Charlotte passive (Gió Tiên)
            if (CombatManager.Instance != null && target != null && !target.IsAlly(caster))
            {
                CombatManager.Instance.TriggerDebuffApplied(caster, target, StatusEffectType.DiemYeu);
            }
        }
    }
}
