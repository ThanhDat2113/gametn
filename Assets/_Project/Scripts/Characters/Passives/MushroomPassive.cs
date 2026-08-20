using System.Linq;
using UnityEngine;

/// <summary>
/// Passive của Nấm Lùn (Map 3):
/// "Bào Tử Nổ" — Khi chết, phát nổ gây 10% máu tối đa sát thương chuẩn lên toàn bộ đối thủ.
/// </summary>
public class MushroomPassive : PassiveAbility
{
    private const float EXPLOSION_DAMAGE_PERCENT = 0.10f;

    public override void OnDied()
    {
        if (CombatManager.Instance == null) return;

        var targets = CombatManager.Instance.PlayerUnits.Where(u => u.IsAlive).ToList();
        if (targets.Count == 0) return;

        Debug.Log($"[MushroomPassive] {Owner.UnitName} BÀO TỬ NỔ! Gây sát thương lên toàn bộ đối thủ!");

        foreach (var target in targets)
        {
            int dmg = Mathf.Max(1, Mathf.RoundToInt(target.MaxHP * EXPLOSION_DAMAGE_PERCENT));
            target.TakeDamage(Owner, dmg, DamageType.True);
            Debug.Log($"[MushroomPassive] Bào Tử Nổ gây {dmg} sát thương lên {target.UnitName}!");

            // Text hiệu ứng nổ đỏ thẫm
            var view = CombatManager.Instance?.GetUnitView(target);
            if (view != null)
                DamageTextManager.Instance?.ShowStatusText("BÀO TỬ NỔ!", view.GetDamageTextPosition(), DamageTextManager.Instance.explosionColor, Vector2.up);
        }
    }
}