using UnityEngine;

[CreateAssetMenu(fileName = "EmpowerNextAttackEffect", menuName = "RPG/SkillEffect/Empower Next Attack")]
public class EmpowerNextAttackEffect : SkillEffect
{
    [Tooltip("Lượng sát thương cộng thêm cho mỗi stack. 0.1 = 10%")]
    public float damageBonusPerStack = 0.1f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Skill này chỉ áp dụng cho người cast
        caster.ApplyStatus(StatusEffectType.Empowered, 99, damageBonusPerStack, 1); // duration 99, 1 stack
    }
}