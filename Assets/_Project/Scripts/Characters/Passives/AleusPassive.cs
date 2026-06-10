using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Passive của Aleus: Mỗi khi sử dụng 1 kỹ năng lên đồng đội, Aleus hồi phục cho 1 đồng minh thấp máu nhất 2% máu tối đa.
/// </summary>
public class AleusPassive : PassiveAbility
{
    private const float HEAL_PERCENT = 0.02f;

    public override void Initialize(CombatUnit owner)
        {
            base.Initialize(owner);
            Owner.OnActionConfirmed += OnOwnerActionConfirmed;
            Debug.Log($"[{Owner.UnitName}'s Passive] Đã đăng ký vào sự kiện OnActionConfirmed.");
        }

        public override void Cleanup()
        {
            if (Owner != null)
            {
                Owner.OnActionConfirmed -= OnOwnerActionConfirmed;
                Debug.Log($"[{Owner.UnitName}'s Passive] Đã hủy đăng ký khỏi sự kiện OnActionConfirmed.");
            }
            base.Cleanup();
        }

        private void OnOwnerActionConfirmed(CombatUnit caster, SkillData skill, List<CombatUnit> targets)
        {
            // Chỉ kích hoạt khi chính Aleus thực hiện hành động
            if (caster != Owner) return;

            Debug.Log($"[{Owner.UnitName}'s Passive] Nhận được sự kiện OnActionConfirmed! Đang xử lý kỹ năng: {skill.skillName}.");

            // 1. Kiểm tra xem kỹ năng có mục tiêu là đồng đội không
            if (skill == null || (skill.targetType != TargetType.SingleAlly && skill.targetType != TargetType.AllAllies))
            {
                Debug.Log($"[{Owner.UnitName}'s Passive] Kỹ năng '{skill.skillName}' không phải là kỹ năng hỗ trợ đồng minh. Bỏ qua.");
                return;
            }

        // 2. Tìm tất cả đồng minh đã mất máu
        var injuredAllies = CombatManager.Instance.PlayerUnits
            .Where(u => u.IsAlive && u.CurrentHP < u.MaxHP)
            .ToList();

        if (injuredAllies.Count == 0)
        {
            Debug.Log($"[{Owner.UnitName}'s Passive] Kỹ năng hỗ trợ được sử dụng, nhưng không có đồng minh nào cần hồi máu.");
            return; // Không có ai bị thương, không làm gì cả
        }

        // 3. Sắp xếp để tìm người có %HP thấp nhất
        var lowestHpAlly = injuredAllies.OrderBy(u => (float)u.CurrentHP / u.MaxHP).FirstOrDefault();

        if (lowestHpAlly != null)
        {
            int healAmount = Mathf.RoundToInt(lowestHpAlly.MaxHP * HEAL_PERCENT);
            Debug.Log($"[{Owner.UnitName}'s Passive] Kỹ năng hỗ trợ được sử dụng. Hồi {healAmount} HP cho đồng minh thấp máu nhất: {lowestHpAlly.UnitName}.");
            lowestHpAlly.Heal(healAmount);
        }
    }
}