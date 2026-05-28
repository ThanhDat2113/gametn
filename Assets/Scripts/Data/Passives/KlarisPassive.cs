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
        {
            _stunCooldown--;
        }
    }

    private void HandleTakeDamage(CombatUnit attacker, int damage)
    {
        if (attacker != null && Owner.HasStatus(StatusEffectType.ThuThe) && _stunCooldown == 0)
        {
            Debug.Log($"[KlarisPassive] {Owner.UnitName} đang ở trạng thái Thủ Thế, phản công choáng!");

            // Gây choáng cho kẻ tấn công
            attacker.ApplyStatus(StatusEffectType.Stun, 1);

            // Xóa trạng thái Thủ Thế
            Owner.ClearStatus(StatusEffectType.ThuThe);

            // Đặt thời gian hồi chiêu
            _stunCooldown = 3;
        }
    }


    private void OnDamageCalculation(ActionOutcome outcome, CombatUnit actor)
    {
        // Bỏ qua nếu Klaris đã chết, hoặc mục tiêu là chính Klaris, hoặc mục tiêu không phải đồng minh
        if (!Owner.IsAlive || outcome.Target == Owner || !outcome.Target.IsAlly(Owner))
        {
            return;
        }

        // Bỏ qua nếu sát thương đến từ chính Klaris (tránh các trường hợp phức tạp)
        if (actor == Owner)
        {
            return;
        }

        int originalDamage = outcome.Damage;
        int redirectedDamage = Mathf.FloorToInt(originalDamage * 0.3f);

        if (redirectedDamage > 0)
        {
            // Giảm sát thương trên mục tiêu
            outcome.Damage -= redirectedDamage;

            // Gây sát thương cho Klaris (sát thương chuẩn, không qua tính toán giáp)
            Owner.TakeDamage(null, redirectedDamage, isTrueDamage: true);

            Debug.Log($"[KlarisPassive] Chuyển hướng {redirectedDamage} sát thương từ {outcome.Target.UnitName} sang cho {Owner.UnitName}. Sát thương mới: {outcome.Damage}");
        }
    }
}