using UnityEngine;

/// <summary>
/// Cập nhật: dùng cơ chế charge giảm sát thương theo đòn.
/// Mỗi lần bị tấn công hoặc tấn công → +1 charge giảm sát thương.
/// Tối đa 5 charges, mỗi charge giảm 5% sát thương 1 đòn.
/// </summary>
[CreateAssetMenu(fileName = "CelinePassive", menuName = "RPG/Passives/CelinePassive")]
public class CelinePassive : PassiveAbility
{
    private const int MAX_CHARGES = 5;
    private const float REDUCTION_PER_CHARGE = 0.05f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        // Đăng ký với delegate mới
        Owner.OnDamageTaken += OnOwnerTakeDamage;
        Owner.OnDealDamage += OnOwnerDealDamage;
    }

    // Sửa: thêm tham số DamageType (không dùng)
    private void OnOwnerTakeDamage(CombatUnit attacker, int damage, DamageType damageType)
    {
        ApplyCharge();
    }

    private void OnOwnerDealDamage(CombatUnit target, int damage)
    {
        ApplyCharge();
    }

    private void ApplyCharge()
    {
        if (Owner != null && Owner.IsAlive)
        {
            int currentCharges = Owner.DamageReductionChargesRemaining;
            if (currentCharges < MAX_CHARGES)
            {
                Owner.AddDamageReductionCharges(1, REDUCTION_PER_CHARGE);
                Debug.Log($"[CelinePassive] {Owner.UnitName} tích lũy giáp! ({currentCharges+1}/{MAX_CHARGES})");
            }
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnDamageTaken -= OnOwnerTakeDamage;
            Owner.OnDealDamage -= OnOwnerDealDamage;
        }
    }
}