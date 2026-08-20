using UnityEngine;

/// <summary>
/// Nội tại của Sói: Luôn hành động đầu tiên trong combat, trước cả player.
/// </summary>
public class WolfPassive : PassiveAbility
{
    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        if (Owner == null) return;

        Owner.AlwaysActsFirst = true;
        Debug.Log($"[WolfPassive] {Owner.UnitName} sẽ luôn hành động đầu tiên trong combat!");

        // Text hiệu ứng
        var view = CombatManager.Instance?.GetUnitView(Owner);
        if (view != null)
            DamageTextManager.Instance?.ShowStatusText("SÓI ĐI SĂN!", view.GetDamageTextPosition(), DamageTextManager.Instance.wolfColor, Vector2.up);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.AlwaysActsFirst = false;
        }
    }
}