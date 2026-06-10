using UnityEngine;

[CreateAssetMenu(fileName = "BuffStatEffect", menuName = "RPG/Effects/Buff Stat")]
public class BuffStatEffect : SkillEffect
{
    public StatType stat;
    public float multiplier = 1.1f;
    public int duration;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyBuff(stat, multiplier, duration);
        }
    }
}