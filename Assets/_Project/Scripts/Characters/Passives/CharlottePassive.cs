using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Nội tại Charlotte (Gió Tiên):
/// Khi Charlotte hoặc một đồng minh áp hiệu ứng xấu (debuff) lên kẻ địch,
/// Charlotte sẽ NHẢY LƯỢT và ngay lập tức dùng skill 1 (Cắt Gió) vào
/// đúng kẻ địch vừa nhận hiệu ứng xấu đó.
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
        Debug.Log($"[{Owner.UnitName}'s Passive] Gió Tiên kích hoạt! Khi đồng minh áp debuff lên kẻ địch, Charlotte nhảy lượt và dùng skill 1 ngay lập tức.");
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

        // Nếu đồng minh (hoặc chính Charlotte) áp debuff lên kẻ địch → Charlotte nhảy lượt tấn công ngay
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
