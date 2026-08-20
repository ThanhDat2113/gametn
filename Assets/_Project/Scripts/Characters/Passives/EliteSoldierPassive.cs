using UnityEngine;

/// <summary>
/// Passive của Quân Lính Tinh Nhuệ (Map 4):
/// "Kỷ Luật Sắt" — Khi HP dưới 30%, tăng 50% sát thương vĩnh viễn.
/// </summary>
public class EliteSoldierPassive : PassiveAbility
{
    private const float LAST_STAND_HP_THRESHOLD = 0.30f;
    private const float LAST_STAND_DAMAGE_BONUS = 0.50f;
    private bool _lastStandActivated = false;

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (_lastStandActivated || Owner == null || !Owner.IsAlive) return;

        float hpPercent = (float)Owner.CurrentHP / Owner.MaxHP;
        if (hpPercent < LAST_STAND_HP_THRESHOLD)
        {
            _lastStandActivated = true;
            Owner.ApplyBuff(StatType.ATK, 1f + LAST_STAND_DAMAGE_BONUS, 0); // Vĩnh viễn
            Debug.Log($"[EliteSoldierPassive] {Owner.UnitName} kích hoạt Kỷ Luật Sắt! +50% sát thương!");

            // Text hiệu ứng bạc
            var view = CombatManager.Instance?.GetUnitView(Owner);
            if (view != null)
                DamageTextManager.Instance?.ShowStatusText("KỶ LUẬT SẮT!", view.GetDamageTextPosition(), DamageTextManager.Instance.ironColor, Vector2.up);
        }
    }
}