using UnityEngine;

[CreateAssetMenu(fileName = "TauntEffect", menuName = "RPG/Effects/Taunt")]
public class TauntEffect : SkillEffect
{
    public int duration = 3;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Kỹ năng này thường chỉ áp dụng cho bản thân caster
        caster.ApplyStatus(StatusEffectType.Taunt, duration);
    }
}