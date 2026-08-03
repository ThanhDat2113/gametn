using UnityEngine;

[CreateAssetMenu(fileName = "BuffStatEffect", menuName = "RPG/Effects/Buff Stat")]
public class BuffStatEffect : SkillEffect
{
    public StatType stat;
    public float multiplier = 1.1f;
    public int duration;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Buff lợi (multiplier >= 1) KHÔNG nên áp lên kẻ địch.
        // Một số skill "buff ATK bản thân" bị cấu hình targetType = SingleEnemy
        // (vd: Nicholas "Slash" tăng 5% ATK cho bản thân nhưng targetType lại là enemy),
        // khiến BuffStatEffect áp buff lên enemy thay vì caster → buff không tăng sát thương.
        //
        // Fix tổng quát: nếu target là kẻ địch của caster (không phải đồng minh),
        // coi như self-buff → áp buff lên caster.
        if (targets == null || targets.Length == 0)
        {
            caster.ApplyBuff(stat, multiplier, duration);
            return;
        }

        // Chỉ redirect self-buff khi đây là buff lợi (multiplier >= 1).
        // Nếu multiplier < 1 (debuff giảm stat), giữ nguyên áp lên target như cũ.
        bool isBeneficial = multiplier >= 1f;

        foreach (var target in targets)
        {
            if (target == null) continue;
            bool isEnemy = !caster.IsAlly(target);
            if (isEnemy && isBeneficial)
            {
                // Buff ích lợi không áp lên kẻ địch — áp cho caster (self-buff).
                caster.ApplyBuff(stat, multiplier, duration);
            }
            else
            {
                target.ApplyBuff(stat, multiplier, duration);
            }
        }
    }
}