using UnityEngine;

[CreateAssetMenu(fileName = "AtkDebuffEffect", menuName = "RPG/Effects/ATK Debuff")]
public class AtkDebuffEffect : SkillEffect
{
    public float reductionMultiplier = 0.8f;
    public int duration = 2;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyBuff(StatType.ATK, reductionMultiplier, duration);
        }
    }
}