using UnityEngine;

[CreateAssetMenu(fileName = "ExtraTurnEffect", menuName = "RPG/Effects/Extra Action")]
public class ExtraTurnEffect : SkillEffect
{
    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Cấp lượt cho CÁC ĐỐI TƯỢNG ĐƯỢC SKILL CHỌN (targets) — hỗ trợ đồng thời:
        // - Kurumi skill Self (target = chính mình) → Kurumi được act thêm.
        // - Eugeo skill SingleAlly (target = đồng minh khác) → đồng minh đó được act thêm.
        // Deferred: chỉ đánh dấu qua RequestExtraAction — CombatManager.DoPlayerTurn sẽ grant
        // ngay sau ResolveAction (grant giữa animation dễ bị nuốt vì thứ tự cập nhật UI/state).
        if (CombatManager.Instance == null || targets == null) return;
        CombatManager.Instance.RequestExtraAction(targets);
        Debug.Log($"[ExtraAction] Đã đánh dấu {targets.Length} mục tiêu được cấp thêm lượt (skill của {caster.UnitName}).");
    }
}