using UnityEngine;

/// <summary>
/// Passive của Người Cây (Map 3):
/// "Sinh Khí Dồi Dào" — Hồi 5% máu tối đa mỗi đầu lượt của bản thân.
/// </summary>
public class TreantPassive : PassiveAbility
{
    private const float REGEN_PERCENT = 0.05f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        if (Owner == null) return;

        owner.OnTurnStart += OnTurnStart;
        Debug.Log($"[TreantPassive] {Owner.UnitName} Sinh Khí Dồi Dào! Hồi 5% máu mỗi lượt.");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnTurnStart -= OnTurnStart;
        }
    }

    private void OnTurnStart()
    {
        if (Owner == null || !Owner.IsAlive) return;

        int heal = Mathf.Max(1, Mathf.RoundToInt(Owner.MaxHP * REGEN_PERCENT));
        Owner.Heal(heal);
        Debug.Log($"[TreantPassive] {Owner.UnitName} hồi {heal} HP (Sinh Khí Dồi Dào).");

        // Text hiệu ứng hồi máu xanh lá
        var view = CombatManager.Instance?.GetUnitView(Owner);
        if (view != null)
        {
            FloatingTextController.Instance?.ShowFloatingText($"+{heal}", view.transform.position + Vector3.up * 1.5f, Color.green);
            DamageTextManager.Instance?.ShowStatusText("SINH KHÍ!", view.GetDamageTextPosition(), DamageTextManager.Instance.healColor, Vector2.up);
        }
    }
}