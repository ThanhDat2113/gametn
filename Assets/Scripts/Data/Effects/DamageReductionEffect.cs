using UnityEngine;

[CreateAssetMenu(fileName = "DamageReductionEffect", menuName = "RPG/Effects/Damage Reduction")]
public class DamageReductionEffect : SkillEffect
{
    public float reductionPercent = 0.2f;
    public int duration = 2;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyStatus(StatusEffectType.GiamSatThuong, duration, reductionPercent, 1);
        }
    }
}