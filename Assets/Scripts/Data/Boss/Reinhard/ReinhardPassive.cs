using UnityEngine;

/// <summary>
/// Passive của Reinhard (Mini Boss):
/// Huyết Mạch Kiếm Thánh — 
/// - 20% khi bị tấn công → buff ATK +15% trong 2 lượt.
/// - 10% khi bị tấn công → tự đẩy lượt, hành động ngay lập tức.
/// </summary>
public class ReinhardPassive : PassiveAbility
{
    private const float ATK_BUFF_CHANCE = 0.20f;
    private const float ATK_BUFF_MULTIPLIER = 1.15f;
    private const int BUFF_DURATION = 2;

    private const float TURN_PUSH_CHANCE = 0.10f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        Debug.Log($"[{Owner.UnitName}'s Passive] Huyết Mạch Kiếm Thánh kích hoạt! 20% buff ATK, 10% đẩy lượt khi bị đánh.");
    }

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (Owner == null || !Owner.IsAlive) return;

        // 20% buff ATK
        if (Random.value < ATK_BUFF_CHANCE)
        {
            Owner.ApplyBuff(StatType.ATK, ATK_BUFF_MULTIPLIER, BUFF_DURATION);
            Debug.Log($"[{Owner.UnitName}'s Passive] Huyết Mạch Kiếm Thánh thức tỉnh! ATK +15% trong {BUFF_DURATION} lượt.");
        }

        // 10% đẩy lượt
        if (Random.value < TURN_PUSH_CHANCE)
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.GrantImmediateTurn(Owner);
                Debug.Log($"[{Owner.UnitName}'s Passive] Huyết Mạch Kiếm Thánh phản đòn! Đẩy lượt hành động ngay lập tức.");
            }
        }
    }
}
