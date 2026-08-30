using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Effects/Damage")]
public class DamageEffect : SkillEffect
{
    [Range(0.1f, 5f)] public float multiplier = 1f;
    public DamageType damageType = DamageType.Physical;

    [Tooltip("Giới hạn sát thương TỔNG lên 1 mục tiêu. 0 = không giới hạn (mặc định).")]
    public int maxDamage = 0;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            var hits = CalculateHits(caster, target, 1);
            foreach (var hit in hits)
                target.TakeDamage(caster, hit.Damage, damageType);
        }
    }

    public List<HitData> CalculateHits(CombatUnit caster, CombatUnit target, int hitCount)
    {
        var hits = new List<HitData>();

        int raw = Mathf.RoundToInt(caster.ATK
                       * multiplier
                       * caster.GetStatMultiplier(StatType.ATK)
                       * caster.GetDamageMultiplier());

        float defenseStat = damageType == DamageType.Physical ? target.PDEF : target.MDEF;
        float effectiveDefense = defenseStat * (1f - caster.ArmorPenetration);
        
        int baseDamage;
        if (!caster.IsPlayer)
        {
            // Quái vật & boss: min damage ngẫu nhiên 9-12
            int minDamage = Random.Range(9, 13); // 9, 10, 11, 12
            baseDamage = Mathf.Max(minDamage, raw - Mathf.RoundToInt(effectiveDefense));
        }
        else
        {
            // Player: giữ nguyên min damage = 1
            baseDamage = Mathf.Max(1, raw - Mathf.RoundToInt(effectiveDefense));
        }

        bool isCritical = Random.value < caster.CritChance;
        if (isCritical)
            baseDamage = Mathf.RoundToInt(baseDamage * caster.CritDamage);

        int totalDmg = Mathf.RoundToInt(baseDamage * target.GetDamageTakenMultiplier());
        if (maxDamage > 0 && totalDmg > maxDamage)
            totalDmg = maxDamage;

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