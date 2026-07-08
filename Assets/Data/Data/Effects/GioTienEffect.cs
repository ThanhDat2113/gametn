using UnityEngine;

[CreateAssetMenu(fileName = "GioTienEffect", menuName = "RPG/Effects/Gio Tien")]
public class GioTienEffect : SkillEffect
{
    public int duration = 3;
    public float atkBonus = 0.1f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            target.ApplyBuff(StatType.ATK, 1 + atkBonus, duration);
            target.ApplyStatus(StatusEffectType.GioTien, duration);
        }
    }
}