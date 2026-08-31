using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Nội tại Charlotte (Gió Tiên):
/// Khi Charlotte HAY một đồng minh áp hiệu ứng xấu (debuff) lên kẻ địch,
/// Charlotte sẽ NHẢY LƯỢT và ngay lập tức dùng skill 1 (Cắt Gió) vào
/// đúng kẻ địch vừa nhận hiệu ứng xấu đó.
/// Lưu ý: Khi chính Charlotte gây debuff (vd: skill 3 Bão Cắt có kèm ATK debuff),
/// follow-up sẽ xảy ra SAU khi skill hoàn tất (không nhảy giữa chừng) - nhờ cơ chế
/// _charlotteFollowUpPending được xử lý sau ResolveAction.
/// </summary>
public class CharlottePassive : PassiveAbility
{
    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);

        // Đăng ký sự kiện debuff toàn cục từ CombatManager
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnDebuffApplied += OnDebuffApplied;
        }
        Debug.Log($"[{Owner.UnitName}'s Passive] Zafkiel kích hoạt! Khi một kẻ địch nhận hiệu ứng xấu, Kurumi lập tức tấn công kẻ đó bằng skill 1 (tối đa 2 lần/lượt).");
    }

    public override void Cleanup()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnDebuffApplied -= OnDebuffApplied;
        }
        base.Cleanup();
    }

private void OnDebuffApplied(CombatUnit caster, CombatUnit target, StatusEffectType status)
    {
        // Charlotte không hoạt động nếu đã chết
        if (Owner == null || !Owner.IsAlive) return;
        if (caster == null) return;

        // Kích hoạt khi Charlotte HOẶC đồng minh áp debuff lên kẻ địch.
        // Khi chính Charlotte gây debuff (vd: skill 3 Bão Cắt), follow-up chỉ xảy ra
        // SAU khi resolve skill hiện tại hoàn tất - nhờ _charlotteFollowUpPending trong
        // DoPlayerTurn được xử lý sau ResolveAction. Điều này tránh nhảy lượt giữa chừng.
        if (caster.IsAlly(Owner) && target != null && !target.IsAlly(Owner))
        {
            Debug.Log($"[{Owner.UnitName}'s Passive] {caster.UnitName} áp {status} lên {target.UnitName}. Charlotte nhảy lượt tấn công!");
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.RequestCharlotteFollowUp(target);
            }
        }
    }
}
