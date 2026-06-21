using UnityEngine;

/// <summary>
/// Tạo N lớp giáp hấp thu sát thương.
/// Mỗi lớp giảm X% sát thương của 1 đòn nhận vào, sau đó biến mất.
/// Universal cho cả sát thương vật lý và phép thuật.
/// </summary>
[CreateAssetMenu(fileName = "DamageReductionChargeEffect", menuName = "RPG/Effects/Damage Reduction Charge")]
public class DamageReductionChargeEffect : SkillEffect
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
                Debug.Log($"[{target.UnitName}] Nhận {charges} lớp giáp, giảm {reductionPercent*100}% sát thương mỗi đòn.");
            }
        }
    }
}