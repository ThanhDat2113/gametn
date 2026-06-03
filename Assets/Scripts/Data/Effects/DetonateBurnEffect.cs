using UnityEngine;

[CreateAssetMenu(fileName = "DetonateBurnEffect", menuName = "RPG/Effects/Detonate Burn")]
public class DetonateBurnEffect : SkillEffect
{
    public float damagePerStackPercent = 0.005f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            var burn = target.GetActiveStatus(StatusEffectType.ThieuDot);
            if (burn != null && burn.Stacks > 0)
            {
                int damage = Mathf.RoundToInt(target.MaxHP * damagePerStackPercent * burn.Stacks);
                damage = Mathf.RoundToInt(damage * caster.GetDamageMultiplier() * target.GetDamageTakenMultiplier());
                target.TakeDamage(caster, damage);
                burn.Stacks = 0; // reset
            }
        }
    }
}