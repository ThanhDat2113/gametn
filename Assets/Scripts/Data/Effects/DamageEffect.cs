using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Effects/Damage")]
public class DamageEffect : SkillEffect
{
    [Range(0.1f, 5f)] public float multiplier = 1f;
    public DamageType damageType = DamageType.Physical;

    // ── Apply trực tiếp (dùng khi không có animation) ────────
    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            // Vì đã bỏ hitCount, ta coi như chỉ có 1 hit
            var hits = CalculateHits(caster, target, 1);
            foreach (var hit in hits)
                target.TakeDamage(caster, hit.Damage, hit.HitIndex);
        }
    }

    // ── Tính trước damage cho từng hit (dùng với animation) ──
    public List<HitData> CalculateHits(CombatUnit caster,
                                        CombatUnit target,
                                        int hitCount)
    {
        var hits = new List<HitData>();

        int raw = Mathf.RoundToInt(caster.ATK
                       * multiplier
                       * caster.GetStatMultiplier(StatType.ATK)
                       * caster.GetDamageMultiplier()); // <--- Tăng sát thương từ hiệu ứng của caster

        int defend = damageType == DamageType.Physical ? target.PDEF : target.MDEF;
        int baseDamage = Mathf.Max(hitCount, raw - defend);

        // Tính toán chí mạng
        bool isCritical = Random.value < caster.CritChance;
        if (isCritical)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * caster.CritDamage);
        }
        
        // Áp dụng hiệu ứng tăng sát thương nhận vào của target
        int totalDmg = Mathf.RoundToInt(baseDamage * target.GetDamageTakenMultiplier());

        for (int i = 0; i < hitCount; i++)
        {
            // Hit cuối nhận phần dư để tổng chính xác
            int dmg = (i == hitCount - 1)
                ? totalDmg - (totalDmg / hitCount) * (hitCount - 1)
                : totalDmg / hitCount;

            hits.Add(new HitData { Damage = dmg, HitIndex = i });
        }

        return hits;
    }
}