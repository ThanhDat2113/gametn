using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffEffect", menuName = "RPG/Effects/Buff")]
public class BuffEffect : SkillEffect
{
    [Tooltip("Thời gian hiệu lực của hiệu ứng (số lượt của mục tiêu)")]
    public int duration = 2;

    [Tooltip("Các chỉ số được buff/debuff. Multiplier > 1 là buff, < 1 là debuff.")]
    public List<Buff> buffs = new List<Buff>();

    [Tooltip("Các trạng thái đặc biệt được áp dụng.")]
    public List<StatusEffectType> statusEffects = new List<StatusEffectType>();

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            // Áp dụng buff/debuff chỉ số
            foreach (var buff in buffs)
            {
                target.ApplyBuff(buff.stat, buff.multiplier, duration);
            }

            // Áp dụng trạng thái đặc biệt
            foreach (var status in statusEffects)
            {
                target.ApplyStatus(status, duration);
            }
        }
    }

    [System.Serializable]
    public class Buff
    {
        public StatType stat;
        [Tooltip("Giá trị nhân. VD: 1.2 = +20%, 0.8 = -20%")]
        public float multiplier = 1.0f;
    }
}