using UnityEngine;

/// <summary>
/// Passive của Orc Cỡ Lớn (Map 2):
/// "Cơn Thịnh Nộ" — Khi HP dưới 50%, tăng 30% sát thương vĩnh viễn.
/// </summary>
public class OrcPassive : PassiveAbility
{
    private const float RAGE_HP_THRESHOLD = 0.5f;
    private const float RAGE_DAMAGE_BONUS = 0.30f;
    private bool _isEnraged = false;

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (_isEnraged || Owner == null || !Owner.IsAlive) return;

        float hpPercent = (float)Owner.CurrentHP / Owner.MaxHP;
        if (hpPercent < RAGE_HP_THRESHOLD)
        {
            _isEnraged = true;
            Owner.ApplyBuff(StatType.ATK, 1f + RAGE_DAMAGE_BONUS, 0); // Vĩnh viễn
            Debug.Log($"[OrcPassive] {Owner.UnitName} nổi cơn thịnh nộ! +30% sát thương!");

            // Text hiệu ứng
            var view = CombatManager.Instance?.GetUnitView(Owner);
            if (view != null)
                DamageTextManager.Instance?.ShowStatusText("THỊNH NỘ!", view.GetDamageTextPosition(), DamageTextManager.Instance.rageColor, Vector2.up);
        }
    }
}