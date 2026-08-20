using UnityEngine;

/// <summary>
/// Passive của Nhện (Map 2):
/// "Nọc Độc" — 30% khi bị tấn công cận chiến → làm kẻ tấn công trúng Thiêu Đốt (ThieuDot) 2 lượt.
/// </summary>
public class SpiderPassive : PassiveAbility
{
    private const float PROC_CHANCE = 0.30f;
    private const int POISON_DURATION = 2;

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (Owner == null || !Owner.IsAlive || attacker == null) return;
        if (!attacker.IsPlayer) return;
        if (attacker.Data != null && attacker.Data.defaultCombatStyle != CombatStyle.Melee) return;

        if (Random.value < PROC_CHANCE)
        {
            attacker.ApplyStatus(StatusEffectType.ThieuDot, POISON_DURATION, 0.05f, 1);
            Debug.Log($"[SpiderPassive] {attacker.UnitName} trúng Nọc Độc!");

            // Text hiệu ứng
            var view = CombatManager.Instance?.GetUnitView(attacker);
            if (view != null)
                DamageTextManager.Instance?.ShowStatusText("NỌC ĐỘC!", view.GetDamageTextPosition(), DamageTextManager.Instance.poisonColor, Vector2.up);
        }
    }
}