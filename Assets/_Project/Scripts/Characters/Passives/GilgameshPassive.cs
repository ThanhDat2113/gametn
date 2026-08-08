using UnityEngine;
using System.Linq;

/// <summary>
/// Passive của Gilgamesh (Boss) - Đã sửa theo yêu cầu:
/// 1. Mỗi lần gây sát thương → +1% sát thương vĩnh viễn (stack vô hạn)
/// 2. Khi tấn công → 20% tấn công thêm bằng Skill 1 vào mục tiêu hiện tại
/// 3. Khi bị tấn công → 100% phản đòn ngay lập tức (Interrupt) + cộng thêm lượt hành động (nhảy lượt), KHÔNG giới hạn
/// 4. Death Trigger: Khi chết, kích hoạt Enuma Elish - Final
/// </summary>
public class GilgameshPassive : PassiveAbility
{
    private const float DMG_INCREASE_PER_HIT = 0.01f;     // +1% mỗi lần gây sát thương
    private const float PROC_CHANCE = 0.20f;                // 20% proc
    private const float FINAL_ENUMA_ELISH_DAMAGE_PERCENT = 0.25f;

    private float _bonusDamageMultiplier = 1f;              // Tích lũy +1% mỗi lần
    private bool _deathTriggerActivated = false;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        owner.MaxActionsPerTurn = 2;
        Debug.Log($"[{Owner.UnitName}'s Passive] King's Accumulation! +1% sát thương vĩnh viễn mỗi lần gây sát thương. 20% proc Skill 1 khi tấn công. 100% phản đòn + nhảy lượt khi bị tấn công.");
    }

    public override void OnDealDamage(CombatUnit target, int damage)
    {
        if (Owner == null || !Owner.IsAlive) return;

        // 1. Mỗi lần gây sát thương → +1% sát thương vĩnh viễn
        _bonusDamageMultiplier += DMG_INCREASE_PER_HIT;
        Debug.Log($"[{Owner.UnitName}'s Passive] Sát thương tích lũy: +{((_bonusDamageMultiplier - 1f) * 100):F1}% (x{_bonusDamageMultiplier:F3})");

        // 2. Khi tấn công → 20% tấn công thêm bằng Skill 1
        if (Random.value < PROC_CHANCE)
        {
            // Lấy skill 1 của Gilgamesh (Gate of Babylon)
            var skill1 = Owner.AvailableSkills?.FirstOrDefault();
            if (skill1 != null && target != null && target.IsAlive)
            {
                Owner.SelectSkill(skill1, new System.Collections.Generic.List<CombatUnit> { target });
                Owner.ExecuteSelectedSkill(0);
                Debug.Log($"[{Owner.UnitName}'s Passive] 20% proc! Tấn công thêm [{skill1.skillName}] vào {target.UnitName}!");
            }
        }
    }

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (Owner == null || !Owner.IsAlive || attacker == null) return;
        if (!attacker.IsPlayer) return; // Chỉ phản đòn player

        // 3. Khi bị tấn công → 100% phản đòn ngay lập tức (Interrupt) + cộng thêm lượt hành động (nhảy lượt).
        //    Hoạt động giống Reinhard: phản đòn đúng kẻ tấn công + extra action, KHÔNG giới hạn số lần.
        if (CombatManager.Instance != null)
        {
            // 100% phản đòn ngay lập tức (Interrupt) - đánh trả attacker giữa lượt player
            CombatManager.Instance.RequestInterrupt(Owner, attacker);

            // Cộng thêm extra action cho lượt enemy của Gilgamesh
            CombatManager.Instance.GrantExtraAction(Owner);

            Debug.Log($"[{Owner.UnitName}'s Passive] Vua Anh Hùng phản đòn ngay lập tức! Đánh trả {attacker.UnitName}. (Extra action +1, không giới hạn nhảy lượt)");
        }
    }

    public override void OnDied()
    {
        if (_deathTriggerActivated) return;
        _deathTriggerActivated = true;

        Debug.Log($"[{Owner.UnitName}'s Passive] ENUMA ELISH - FINAL! Kích hoạt khi chết!");
        TriggerFinalEnumaElish();
    }

    /// <summary>
    /// Final Enuma Elish: gây sát thương chuẩn = 25% maxHP toàn bộ player
    /// </summary>
    private void TriggerFinalEnumaElish()
    {
        if (CombatManager.Instance == null) return;

        var targetUnits = CombatManager.Instance.PlayerUnits.Where(u => u.IsAlive).ToList();
        if (targetUnits.Count == 0) return;

        Debug.Log($"[{Owner.UnitName}] ENUMA ELISH - FINAL! Vụ nổ hủy diệt cuối cùng!");

        foreach (var target in targetUnits)
        {
            int damage = Mathf.RoundToInt(target.MaxHP * FINAL_ENUMA_ELISH_DAMAGE_PERCENT);
            damage = Mathf.Max(1, damage);

            // True damage - xuyên qua mọi phòng thủ
            target.TakeDamage(Owner, damage, DamageType.True);

            Debug.Log($"  Final Enuma Elish gây {damage} sát thương chuẩn lên {target.UnitName} (HP: {target.CurrentHP}/{target.MaxHP})");

            var targetView = CombatManager.Instance.GetUnitView(target);
            if (targetView != null)
            {
                targetView.UpdateHealthBar();
                targetView.TriggerHitFlash();
            }
        }

        CombatManager.Instance.CheckForCombatEnd();
    }
}