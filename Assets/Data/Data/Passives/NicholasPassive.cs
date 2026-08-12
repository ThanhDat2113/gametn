using UnityEngine;

[CreateAssetMenu(fileName = "NicholasPassive", menuName = "RPG/Passives/NicholasPassive")]
public class NicholasPassive : PassiveAbility
{
    private const float CRIT_CHANCE_BONUS = 0.2f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        Owner.CritChance += CRIT_CHANCE_BONUS;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.CritChance -= CRIT_CHANCE_BONUS;
        }
    }

    /// <summary>
    /// Khi kết liễu (kill) 1 kẻ địch → được hành động thêm 1 lượt ngay lập tức.
    /// Khớp với mô tả trong Nicholas_Passive.asset:
    /// "Mỗi khi kết liễu 1 kẻ địch ngay lập tức hành động thêm 1 lượt"
    /// </summary>
    public override void OnKill(CombatUnit target)
    {
        if (Owner == null || !Owner.IsAlive) return;
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.GrantExtraAction(Owner);
            Debug.Log($"[{Owner.UnitName}'s Passive] Kết liễu {target?.UnitName}! Được hành động thêm 1 lượt.");
        }
    }
}
