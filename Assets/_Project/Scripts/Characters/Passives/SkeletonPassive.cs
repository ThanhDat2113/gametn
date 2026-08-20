using UnityEngine;

/// <summary>
/// Nội tại của Skeleton: Hồi sinh sau lần chết đầu tiên với 30% máu tối đa.
/// Khi hồi sinh, nháy màu vàng để hiển thị hiệu ứng.
/// </summary>
public class SkeletonPassive : PassiveAbility
{
    private bool _hasRevived = false;
    private const float REVIVE_HP_PERCENT = 0.3f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        if (Owner == null) return;

        Owner.OnDied += OnOwnerDied;
        Debug.Log($"[SkeletonPassive] {Owner.UnitName} sẽ hồi sinh sau lần chết đầu tiên!");
    }

    private void OnOwnerDied()
    {
        if (_hasRevived) return;
        if (Owner == null) return;

        _hasRevived = true;

        // Hồi sinh với 30% máu tối đa (dùng Heal vì CurrentHP có private set)
        int reviveHP = Mathf.RoundToInt(Owner.MaxHP * REVIVE_HP_PERCENT);
        reviveHP = Mathf.Max(1, reviveHP); // Đảm bảo ít nhất 1 HP
        Owner.Heal(reviveHP);

        Debug.Log($"<color=yellow>[SkeletonPassive] {Owner.UnitName} hồi sinh với {reviveHP} HP!</color>");

        // Text hiệu ứng hồi sinh
        var view = CombatManager.Instance?.GetUnitView(Owner);
        if (view != null)
            DamageTextManager.Instance?.ShowStatusText("HỒI SINH!", view.GetDamageTextPosition(), DamageTextManager.Instance.reviveColor, Vector2.up);

        // Ngăn DeathFade của UnitView và hiển thị revive
        if (view != null)
        {
            // Dừng DeathFade coroutine, reset về trạng thái sống
            view.StopAllCoroutines();
            view.gameObject.SetActive(true);
            view.spriteRenderer.color = Color.white;
            view.SetAlpha(1f);
            view.UpdateHealthBar();
            view.TriggerReviveFlash();
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnDied -= OnOwnerDied;
        }
    }
}