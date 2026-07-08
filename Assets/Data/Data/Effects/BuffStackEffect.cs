using UnityEngine;

[CreateAssetMenu(fileName = "BuffStackEffect", menuName = "RPG/Effects/Buff Stack")]
public class BuffStackEffect : SkillEffect
{
    public StatusEffectType statusType;
    public int duration = 1;
    public float valuePerStack = 0.1f;
    public int stacks = 1;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyStatus(statusType, duration, valuePerStack, stacks);
        }
    }
}