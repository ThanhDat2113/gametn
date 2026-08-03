using UnityEngine;

[CreateAssetMenu(fileName = "ApplyBurnStacksEffect", menuName = "RPG/Effects/Apply Burn Stacks")]
public class ApplyBurnStacksEffect : SkillEffect
{
public int stacks = 1;
    public int duration = 2;
    [Tooltip("Sát thương burn mỗi stack mỗi lượt. Tổng sát thương mỗi lượt = damagePerStack * stacks.")]
    public int damagePerStack = 5;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Chỉ áp dụng burn cho mục tiêu chính (target đầu tiên)
        // để tránh burn lan sang toàn bộ kẻ địch trong combat.
        if (targets == null || targets.Length == 0) return;
        var primaryTarget = targets[0];
        if (primaryTarget == null || !primaryTarget.IsAlive) return;

        primaryTarget.ApplyStatus(StatusEffectType.ThieuDot, duration, damagePerStack, stacks);
    }
}
