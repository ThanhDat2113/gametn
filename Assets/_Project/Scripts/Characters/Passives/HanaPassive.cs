using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nội tại của Hana: Mỗi khi một đồng minh (bao gồm cả Hana) sử dụng kỹ năng tiêu tốn từ 2AP trở lên, họ nhận thêm 10% sát thương cho hành động đó.
/// </summary>
public class HanaPassive : PassiveAbility
{
    private const float DAMAGE_BONUS = 0.1f; // 10% bonus damage

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        if (CombatManager.Instance == null) return;

        // Lắng nghe sự kiện của tất cả các đồng minh hiện tại
        List<CombatUnit> allies = owner.IsPlayer ? CombatManager.Instance.PlayerUnits : CombatManager.Instance.EnemyUnits;
        foreach (var ally in allies)
        {
            ally.OnActionConfirmed += OnAllyActionConfirmed;
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (CombatManager.Instance == null) return;

        // Hủy lắng nghe sự kiện
        List<CombatUnit> allies = Owner.IsPlayer ? CombatManager.Instance.PlayerUnits : CombatManager.Instance.EnemyUnits;
        foreach (var ally in allies)
        {
            // Thêm kiểm tra null để tránh lỗi nếu unit đã bị hủy
            if (ally != null)
            {
                ally.OnActionConfirmed -= OnAllyActionConfirmed;
            }
        }
    }

    private void OnAllyActionConfirmed(CombatUnit caster, SkillData skill, List<CombatUnit> targets)
    {
        // Nội tại của Hana không kích hoạt nếu Hana đã chết
        if (!Owner.IsAlive) return;

        // Kiểm tra điều kiện: caster là đồng minh và skill tốn >= 2 AP
        if (caster.IsAlly(Owner) && skill.apCost >= 2)
        {
            Debug.Log($"[HanaPassive] Đồng minh {caster.UnitName} dùng skill '{skill.skillName}' tốn {skill.apCost}AP. Kích hoạt nội tại của Hana!");
            
            // Áp dụng buff Empower. Buff này sẽ được ActionResolver sử dụng và xóa ngay sau đó.
            caster.ApplyStatus(StatusEffectType.Empowered, 1, DAMAGE_BONUS, 1); // Duration 1, 1 stack, 10% bonus
        }
    }
}