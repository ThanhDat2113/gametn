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