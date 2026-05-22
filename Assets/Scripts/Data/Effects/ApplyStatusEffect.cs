using UnityEngine;

[CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "RPG/Effects/Apply Status")]
public class ApplyStatusEffect : SkillEffect
{
    public StatusEffectType status;
    public int duration = 1;
    public float value = 0f;
    public int stacks = 1;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyStatus(status, duration, value, stacks);
        }
    }
}