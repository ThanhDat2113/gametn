using UnityEngine;

/// <summary>
/// Nội tại của Klaris: Mỗi khi một đồng minh nhận sát thương, Klaris chịu thay cho họ 30% số sát thương đấy.
/// </summary>
public class KlarisPassive : PassiveAbility
{
    private int _stunCooldown = 0;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnDamageCalculation += OnDamageCalculation;
        }
        // ✅ ĐÃ SỬA: khớp với delegate Action<CombatUnit, int, DamageType>
        Owner.OnDamageTaken += HandleTakeDamage;
        Owner.OnTurnStart += OnTurnStart;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnDamageCalculation -= OnDamageCalculation;
        }
        Owner.OnDamageTaken -= HandleTakeDamage;
        Owner.OnTurnStart -= OnTurnStart;
    }

    private void OnTurnStart()
    {
        if (_stunCooldown > 0)
            _stunCooldown--;
    }

    // ✅ ĐÃ SỬA: thêm tham số DamageType (không sử dụng trong logic này)
    private void HandleTakeDamage(CombatUnit attacker, int damage, DamageType damageType)
    {
        if (attacker != null && Owner.HasStatus(StatusEffectType.ThuThe) && _stunCooldown == 0)
        {
            Debug.Log($"[KlarisPassive] {Owner.UnitName} đang ở trạng thái Thủ Thế, phản công choáng!");
            attacker.ApplyStatus(StatusEffectType.Stun, 1);
            Owner.ClearStatus(StatusEffectType.ThuThe);
            _stunCooldown = 3;
        }
    }

    private void OnDamageCalculation(ActionOutcome outcome, CombatUnit actor)
    {
        if (!Owner.IsAlive || outcome.Target == Owner || !outcome.Target.IsAlly(Owner))
            return;

        if (actor == Owner)
            return;

        int originalDamage = outcome.Damage;
        int redirectedDamage = Mathf.FloorToInt(originalDamage * 0.3f);

        if (redirectedDamage > 0)
        {
            outcome.Damage -= redirectedDamage;
            Owner.TakeDamage(null, redirectedDamage, DamageType.True);
            Debug.Log($"[KlarisPassive] Chuyển hướng {redirectedDamage} sát thương từ {outcome.Target.UnitName} sang cho {Owner.UnitName}. Sát thương mới: {outcome.Damage}");
        }
    }
}