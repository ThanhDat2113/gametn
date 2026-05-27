using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MissingHPDamageEffect", menuName = "RPG/Effects/Missing HP Damage")]
public class MissingHPDamageEffect : DamageEffect
{
    public float missingHPMultiplier = 0.5f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            int missingHP = target.MaxHP - target.CurrentHP;
            int damage = Mathf.RoundToInt(missingHP * multiplier);
            target.TakeDamage(caster, damage);
        }
    }

    public new List<HitData> CalculateHits(CombatUnit caster, CombatUnit target, int hitCount)
    {
        var hits = new List<HitData>();

        int raw = Mathf.RoundToInt(caster.ATK * multiplier * caster.GetStatMultiplier(StatType.ATK) * caster.GetDamageMultiplier());
        int defend = damageType == DamageType.Physical ? target.PDEF : target.MDEF;
        int baseDamage = Mathf.Max(1, raw - defend);

        // Bonus damage dựa trên HP đã mất của mục tiêu
        float missingPercent = (target.MaxHP - target.CurrentHP) / (float)target.MaxHP;
        int bonus = Mathf.RoundToInt(target.MaxHP * missingPercent * missingHPMultiplier);
        int totalDamage = baseDamage + bonus;
        totalDamage = Mathf.RoundToInt(totalDamage * target.GetDamageTakenMultiplier());

        bool isCritical = Random.value < caster.CritChance;
        if (isCritical)
            totalDamage = Mathf.RoundToInt(totalDamage * caster.CritDamage);

        for (int i = 0; i < hitCount; i++)
        {
            int dmg = (i == hitCount - 1)
                ? totalDamage - (totalDamage / hitCount) * (hitCount - 1)
                : totalDamage / hitCount;
            hits.Add(new HitData { Damage = dmg, HitIndex = i });
        }
        return hits;
    }
}