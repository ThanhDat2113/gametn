using UnityEngine;

[CreateAssetMenu(fileName = "EmpowerNextAttackEffect", menuName = "RPG/Effects/Empower Next Attack")]
public class EmpowerNextAttackEffect : SkillEffect
{
    public float damageBonus = 0.1f;
    public int duration = 1;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        caster.ApplyStatus(StatusEffectType.Empowered, duration, damageBonus);
    }
}