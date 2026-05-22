using UnityEngine;

[CreateAssetMenu(fileName = "ReflectDamageEffect", menuName = "RPG/Effects/Reflect Damage")]
public class ReflectDamageEffect : SkillEffect
{
    [Tooltip("Tỉ lệ sát thương phản lại. 0.1 = 10%")]
    public float reflectMultiplier = 0.1f;
    public int duration = 3;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Hiệu ứng này thường chỉ áp dụng cho bản thân caster
        caster.ApplyStatus(StatusEffectType.ReflectDamage, duration, reflectMultiplier);
    }
}