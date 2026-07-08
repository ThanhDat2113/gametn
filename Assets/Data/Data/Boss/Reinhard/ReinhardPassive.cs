using UnityEngine;

/// <summary>
/// Passive của Reinhard (Mini Boss):
/// Huyết Mạch Kiếm Thánh — 
/// - 20% khi bị tấn công → buff ATK +15% trong 2 lượt.
/// - 10% khi bị tấn công → được act thêm 1 lần nữa.
/// </summary>
public class ReinhardPassive : PassiveAbility
{
    private const float ATK_BUFF_CHANCE = 0.20f;
    private const float ATK_BUFF_MULTIPLIER = 1.15f;
    private const int BUFF_DURATION = 2;
    private const float EXTRA_ACTION_CHANCE = 0.10f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        Debug.Log($"[{Owner.UnitName}'s Passive] Huyết Mạch Kiếm Thánh kích hoạt! 20% buff ATK, 10% extra action khi bị đánh.");
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

        // 10% extra action
        if (Random.value < EXTRA_ACTION_CHANCE)
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.GrantExtraAction(Owner);
                Debug.Log($"[{Owner.UnitName}'s Passive] Huyết Mạch Kiếm Thánh phản đòn! Được act thêm 1 lần.");
            }
        }
    }
}
