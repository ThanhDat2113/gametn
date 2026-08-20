using UnityEngine;

/// <summary>
/// Passive của Edward (Mini Boss):
/// Automail Phản Giáp — 25% khi bị tấn công cận chiến → làm choáng kẻ tấn công 1 lượt.
/// Edward có thể hành động 2 lần mỗi lượt (MaxActionsPerTurn = 2).
/// Khi dùng skill mạnh nhất sẽ ưu tiên target yếu máu (AI đã xử lý).
/// </summary>
public class EdwardPassive : PassiveAbility
{
    private const float PROC_CHANCE = 0.25f;
    private const int STUN_DURATION = 1;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        // Edward có thể act 2 lần mỗi turn
        owner.MaxActionsPerTurn = 2;
        Debug.Log($"[{Owner.UnitName}'s Passive] Automail Phản Giáp kích hoạt! 25% cơ hội làm choáng kẻ tấn công cận chiến. (2 actions/turn)");
    }

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (Owner == null || !Owner.IsAlive || attacker == null) return;

        // Chỉ phản đòn cận chiến (defaultCombatStyle = Melee)
        if (attacker.Data != null && attacker.Data.defaultCombatStyle == CombatStyle.Melee)
        {
            if (Random.value < PROC_CHANCE)
            {
                attacker.ApplyStatus(StatusEffectType.Stun, STUN_DURATION);
                Debug.Log($"[{Owner.UnitName}'s Passive] Automail phản đòn! {attacker.UnitName} bị choáng trong {STUN_DURATION} lượt.");

                // Text hiệu ứng
                var view = CombatManager.Instance?.GetUnitView(attacker);
                if (view != null)
                    DamageTextManager.Instance?.ShowStatusText("PHẢN GIÁP!", view.GetDamageTextPosition(), DamageTextManager.Instance.ironColor, Vector2.up);
            }
        }
    }
}