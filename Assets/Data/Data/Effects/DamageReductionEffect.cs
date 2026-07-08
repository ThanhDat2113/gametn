using UnityEngine;

/// <summary>
/// Cập nhật: dùng cơ chế charge giảm sát thương theo đòn thay vì status GiamSatThuong.
/// Tạo N lớp giáp, mỗi lớp giảm X% sát thương của 1 đòn.
/// </summary>
[CreateAssetMenu(fileName = "DamageReductionEffect", menuName = "RPG/Effects/Damage Reduction")]
public class DamageReductionEffect : SkillEffect
{
    [Tooltip("Số lớp giáp (số đòn được giảm sát thương)")]
    public int charges = 2;

    [Tooltip("% sát thương giảm mỗi lớp (0.25 = 25%)")]
    [Range(0f, 1f)]
    public float reductionPercent = 0.25f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            if (target != null && target.IsAlive)
            {
                target.AddDamageReductionCharges(charges, reductionPercent);
                Debug.Log($"[DamageReductionEffect] {target.UnitName} nhận {charges} lớp giáp, giảm {reductionPercent*100}% sát thương mỗi đòn.");
            }
        }
    }
}
