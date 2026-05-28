using UnityEngine;

[CreateAssetMenu(fileName = "StunSelfEffect", menuName = "RPG/Effects/Stun Self")]
public class StunSelfEffect : SkillEffect
{
    public int duration = 3;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        caster.ApplyStatus(StatusEffectType.Stun, duration);
    }
}