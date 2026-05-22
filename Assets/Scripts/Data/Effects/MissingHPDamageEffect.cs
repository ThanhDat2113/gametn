using UnityEngine;

[CreateAssetMenu(fileName = "MissingHPDamageEffect", menuName = "RPG/Effects/Missing HP Damage")]
public class MissingHPDamageEffect : SkillEffect
{
    public float missingHPMultiplier = 0.5f;
    public DamageType damageType = DamageType.Physical;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            int baseDamage = Mathf.Max(1, caster.ATK - (damageType == DamageType.Physical ? target.PDEF : target.MDEF));
            float missingPercent = (target.MaxHP - target.CurrentHP) / (float)target.MaxHP;
            int bonus = Mathf.RoundToInt(target.MaxHP * missingPercent * missingHPMultiplier);
            int totalDamage = baseDamage + bonus;
            totalDamage = Mathf.RoundToInt(totalDamage * caster.GetDamageMultiplier() * target.GetDamageTakenMultiplier());
            target.TakeDamage(caster, totalDamage);
        }
    }
}