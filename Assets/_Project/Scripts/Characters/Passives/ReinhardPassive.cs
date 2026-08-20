using UnityEngine;

/// <summary>
/// Passive của Reinhard (Mini Boss):
/// Huyết Mạch Kiếm Thánh — 
/// - 40% khi bị tấn công → buff ATK +10% trong 2 lượt.
/// - Khi bị tấn công → 100% phản đòn ngay lập tức (Interrupt), đánh trả kẻ tấn công.
/// Reinhard có 2 actions/turn (MaxActionsPerTurn = 2) + IgnoreTaunt.
/// Khi dùng skill mạnh nhất (skill 3) sẽ ưu tiên target yếu máu nhất (AI đã xử lý).
/// </summary>
public class ReinhardPassive : PassiveAbility
{
    private const float ATK_BUFF_CHANCE = 0.40f;
    private const float ATK_BUFF_MULTIPLIER = 1.10f;
    private const int BUFF_DURATION = 2;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        // Reinhard có 2 actions/turn (giống Edward)
        owner.MaxActionsPerTurn = 2;
        Debug.Log($"[{Owner.UnitName}'s Passive] Huyết Mạch Kiếm Thánh kích hoạt! 40% buff ATK +10%, 100% phản đòn ngay lập tức khi bị đánh. (2 actions/turn, IgnoreTaunt)");
    }

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (Owner == null || !Owner.IsAlive || attacker == null) return;
        if (!attacker.IsPlayer) return; // Chỉ phản đòn player

        // 40% buff ATK +10%
        if (Random.value < ATK_BUFF_CHANCE)
        {
            Owner.ApplyBuff(StatType.ATK, ATK_BUFF_MULTIPLIER, BUFF_DURATION);
            Debug.Log($"[{Owner.UnitName}'s Passive] Huyết Mạch Kiếm Thánh thức tỉnh! ATK +10% trong {BUFF_DURATION} lượt.");
        }

        if (CombatManager.Instance != null)
        {
            // 1. Phản đòn ngay lập tức (Interrupt) - đánh trả attacker giữa lượt player
            CombatManager.Instance.RequestInterrupt(Owner, attacker);

            // 2. Cộng thêm action cho lượt enemy của Reinhard
            CombatManager.Instance.GrantExtraAction(Owner);

            Debug.Log($"[ReinhardPassive] Huyết Mạch Kiếm Thánh phản đòn ngay lập tức! Đánh trả {attacker.UnitName}. (Extra action +1)");

            // Text hiệu ứng
            var view = CombatManager.Instance?.GetUnitView(Owner);
            if (view != null)
                DamageTextManager.Instance?.ShowStatusText("HUYẾT MẠCH!", view.GetDamageTextPosition(), DamageTextManager.Instance.bloodColor, Vector2.up);
        }
    }
}
