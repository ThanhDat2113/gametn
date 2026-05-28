using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Effects/Damage")]
public class DamageEffect : SkillEffect
{
    [Range(0.1f, 5f)] public float multiplier = 1f;
    public DamageType damageType = DamageType.Physical;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Cần biết hitCount – nhưng Apply không có thông tin này.
        // Cách tốt nhất là không dùng Apply cho multi-hit, mà để ActionResolver xử lý.
        // Ở đây tôi giữ nguyên logic 1 hit.
        foreach (var target in targets)
        {
            var hits = CalculateHits(caster, target, 1);
            foreach (var hit in hits)
                target.TakeDamage(caster, hit.Damage);
        }
    }

    public List<HitData> CalculateHits(CombatUnit caster, CombatUnit target, int hitCount)
    {
        var hits = new List<HitData>();

        int raw = Mathf.RoundToInt(caster.ATK
                       * multiplier
                       * caster.GetStatMultiplier(StatType.ATK)
                       * caster.GetDamageMultiplier());

        // Tính toán phòng thủ hiệu quả sau khi trừ đi xuyên giáp
        float defenseStat = damageType == DamageType.Physical ? target.PDEF : target.MDEF;
        float effectiveDefense = defenseStat * (1f - caster.ArmorPenetration);
        
        int baseDamage = Mathf.Max(1, raw - Mathf.RoundToInt(effectiveDefense));

        bool isCritical = Random.value < caster.CritChance;
        if (isCritical)
            baseDamage = Mathf.RoundToInt(baseDamage * caster.CritDamage);

        int totalDmg = Mathf.RoundToInt(baseDamage * target.GetDamageTakenMultiplier());

        for (int i = 0; i < hitCount; i++)
        {
            int dmg = (i == hitCount - 1)
                ? totalDmg - (totalDmg / hitCount) * (hitCount - 1)
                : totalDmg / hitCount;
            hits.Add(new HitData { Damage = dmg, HitIndex = i });
        }
        return hits;
    }
}