using UnityEngine;

/// <summary>
/// Tạo lá chắn hấp thu sát thương — dùng cơ chế charge.
/// Thay vì shield hấp thu tuyệt đối, lá chắn giảm % sát thương theo số đòn.
/// (Đồng bộ với cơ chế Damage Reduction Charge mới)
/// </summary>
[CreateAssetMenu(fileName = "ShieldEffect", menuName = "RPG/Effects/Shield")]
public class ShieldEffect : SkillEffect
{
    [Tooltip("Số đòn được chắn")]
    public int charges = 3;

    [Tooltip("% sát thương giảm mỗi đòn (0.5 = 50%)")]
    [Range(0f, 1f)]
    public float reductionPercent = 0.5f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            if (target != null && target.IsAlive)
            {
                target.AddDamageReductionCharges(charges, reductionPercent);
                Debug.Log($"[Shield] {target.UnitName} nhận lá chắn ({charges} đòn, giảm {reductionPercent*100}% sát thương mỗi đòn).");
            }
        }
    }
}
