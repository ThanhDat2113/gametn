using UnityEngine;

/// <summary>
/// Passive của Edward (Boss 2) — "Giả kim thuật hút sinh":
/// Mỗi lần gây sát thương, Edward hồi máu bằng 25% sát thương gây ra (lifesteal).
/// Edward có thể hành động 3 lần mỗi lượt (MaxActionsPerTurn = 3).
/// Mọi skill của Edward đều giảm 20% ATK mục tiêu trúng đòn trong 2 lượt (AtkDebuff.asset).
/// EdwardAI riêng: Skill 3 AoE chiếm 40% lựa chọn, Skill 1/2 mỗi skill 30%.
/// </summary>
public class EdwardPassive : PassiveAbility
{
    // % sát thương gây ra được hồi thành máu (25% = 0.25f).
    // ⚠ Từng để 2% → chỉ ~2 HP/hit trên pool 4000 HP = vô hình (feedback "chưa heal được").
    // 25% ≈ 26 HP/hit, ~79 HP/turn với 3 actions — thấy rõ mà chưa quá tay.
    private const float HEAL_PERCENT = 0.60f;

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
