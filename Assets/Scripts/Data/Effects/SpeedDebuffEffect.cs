using UnityEngine;

[CreateAssetMenu(fileName = "SpeedDebuffEffect", menuName = "RPG/Effects/Speed Debuff")]
public class SpeedDebuffEffect : SkillEffect
{
    public float reductionMultiplier = 0.8f;
    public int duration = 2;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyBuff(StatType.Speed, reductionMultiplier, duration);
        }
    }
}