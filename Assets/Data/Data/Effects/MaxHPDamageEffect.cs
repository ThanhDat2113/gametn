using UnityEngine;

[CreateAssetMenu(fileName = "MaxHPDamageEffect", menuName = "RPG/Effects/Max HP Damage")]
public class MaxHPDamageEffect : SkillEffect
{
    public float maxHPMultiplier = 0.1f;
    public DamageType damageType = DamageType.Magical;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            int damage = Mathf.RoundToInt(target.MaxHP * maxHPMultiplier);
            damage = Mathf.RoundToInt(damage * caster.GetDamageMultiplier() * target.GetDamageTakenMultiplier());
            target.TakeDamage(caster, damage);
        }
    }
}