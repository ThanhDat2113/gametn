using UnityEngine;

[CreateAssetMenu(fileName = "BuffEffect", menuName = "RPG/Effects/Buff")]
public class BuffEffect : SkillEffect
{
    public StatType stat = StatType.ATK;
    public float multiplier = 1.2f;   // 1.2 = +20%
    public int duration = 2;      // số round

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
            target.AddBuff(stat, multiplier, duration);
    }
}