using UnityEngine;

/// <summary>
/// Passive của Edward (Boss 2) — "Giả kim thuật hút sinh":
/// Mỗi lần gây sát thương, Edward hồi máu bằng 2% sát thương gây ra (lifesteal).
/// Edward có thể hành động 3 lần mỗi lượt (MaxActionsPerTurn = 3).
/// Mọi skill của Edward đều giảm 20% ATK mục tiêu trúng đòn trong 2 lượt (AtkDebuff.asset).
/// Khi dùng skill mạnh nhất sẽ ưu tiên target yếu máu (AI generic đã xử lý).
/// </summary>
public class EdwardPassive : PassiveAbility
{
    // % sát thương gây ra được hồi thành máu (2% = 0.02f).
    // LƯU Ý CÂN BẰNG: ATK 70 × 1.5x ≈ 105 dmg/hit → chỉ hồi ~2 HP/hit.
    // Nếu muốn lifesteal cảm nhận rõ, tăng lên 0.25f - 0.5f.
    private const float HEAL_PERCENT = 0.02f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        // Edward có thể act 3 lần mỗi turn
        owner.MaxActionsPerTurn = 3;
        Debug.Log($"[{Owner.UnitName}'s Passive] Giả kim hút sinh kích hoạt! Hồi {HEAL_PERCENT * 100}% sát thương gây ra. (3 actions/turn)");
    }

    public override void OnDealDamage(CombatUnit target, int damage)
    {
        if (Owner == null || !Owner.IsAlive || damage <= 0) return;

        int heal = Mathf.RoundToInt(damage * HEAL_PERCENT);
        if (heal > 0)
        {
            Owner.Heal(heal);
            Debug.Log($"[{Owner.UnitName}'s Passive] Lifesteal: heal {heal} HP from {damage} damage dealt to {target?.UnitName}.");
        }
    }
}
