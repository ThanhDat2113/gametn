using UnityEngine;

[CreateAssetMenu(fileName = "ApplyBurnStacksEffect", menuName = "RPG/Effects/Apply Burn Stacks")]
public class ApplyBurnStacksEffect : SkillEffect
{
    public int stacks = 1;
    public int duration = 2;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyStatus(StatusEffectType.ThieuDot, duration, 0f, stacks);
        }
    }
}