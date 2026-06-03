using UnityEngine;

[CreateAssetMenu(fileName = "CelinePassive", menuName = "RPG/Passives/CelinePassive")]
public class CelinePassive : PassiveAbility
{
    private const StatusEffectType EFFECT_TYPE = StatusEffectType.GiamSatThuong;
    private const float VALUE_PER_STACK = 0.05f;
    private const int MAX_STACKS = 5;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        // Đăng ký vào các sự kiện cần thiết
        Owner.OnDamageTaken += OnOwnerTakeDamage;
        Owner.OnDealDamage += OnOwnerDealDamage;
    }

    private void OnOwnerTakeDamage(CombatUnit attacker, int damage)
    {
        ApplyStack();
    }

    private void OnOwnerDealDamage(CombatUnit target, int damage)
    {
        ApplyStack();
    }

    private void ApplyStack()
    {
        var existingStatus = Owner.GetActiveStatus(EFFECT_TYPE);
        if (existingStatus == null || existingStatus.Stacks < MAX_STACKS)
        {
            Owner.ApplyStatus(EFFECT_TYPE, 999, VALUE_PER_STACK, 1);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        // Hủy đăng ký để tránh memory leak
        if (Owner != null)
        {
            Owner.OnDamageTaken -= OnOwnerTakeDamage;
            Owner.OnDealDamage -= OnOwnerDealDamage;
        }
    }
}