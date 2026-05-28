using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Nội tại Charlotte: mỗi khi Charlotte hoặc 1 đồng đội tấn công vào kẻ địch bị dính hiệu ứng xấu,
/// cô tấn công thêm vào kẻ địch đấy bằng skill 1 với lượng sát thương bằng 50% sát thương gốc của skill 1.
/// </summary>
public class CharlottePassive : PassiveAbility
{
    private const float EXTRA_ATTACK_MULTIPLIER = 0.5f;
    private SkillData skill1;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        // Tìm skill 1 của Charlotte để tính sát thương
        skill1 = Owner.AvailableSkills.FirstOrDefault(s => s.skillName == "Cắt Gió");

        var allies = CombatManager.Instance.GetTeam(Owner.IsPlayer);
        foreach (var ally in allies)
        {
            ally.OnDealDamage += OnAllyDealDamage;
        }
    }

    public override void Cleanup()
    {
        if (CombatManager.Instance != null)
        {
            var allies = CombatManager.Instance.GetTeam(Owner.IsPlayer);
            foreach (var ally in allies)
            {
                if (ally != null)
                {
                    ally.OnDealDamage -= OnAllyDealDamage;
                }
            }
        }
        base.Cleanup();
    }

    private void OnAllyDealDamage(CombatUnit target, int damage)
    {
        // Nội tại không kích hoạt nếu Charlotte đã chết, hoặc người tấn công là Charlotte (tránh lặp vô hạn)
        if (!Owner.IsAlive) return;

        // Kiểm tra xem mục tiêu có phải là kẻ địch và có hiệu ứng xấu không
        if (target.IsAlly(Owner) || !target.HasAnyDebuff())
        {
            return;
        }

        Debug.Log($"[CharlottePassive] Đồng minh tấn công {target.UnitName} có debuff. Kích hoạt nội tại.");

        // Tính toán sát thương cho đòn đánh thêm
        // Giả sử skill 1 là DamageEffect và có multiplier
        int baseSkill1Damage = Owner.ATK; // Fallback
        if (skill1 != null)
        {
            var damageEffect = skill1.effects.OfType<DamageEffect>().FirstOrDefault();
            if (damageEffect != null)
            {
                baseSkill1Damage = Mathf.RoundToInt(Owner.ATK * damageEffect.multiplier);
            }
        }

        int extraDamage = Mathf.RoundToInt(baseSkill1Damage * EXTRA_ATTACK_MULTIPLIER);

        Debug.Log($"[CharlottePassive] Charlotte tấn công thêm vào {target.UnitName}, gây {extraDamage} sát thương.");
        target.TakeDamage(Owner, extraDamage);
    }
}