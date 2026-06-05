using UnityEngine;

namespace Game.Effects
{
    [CreateAssetMenu(fileName = "HanaSkill2Effect", menuName = "RPG/Effects/Custom/Hana Skill 2")]
    public class HanaSkill2Effect : SkillEffect
    {
        [Header("Ally Buff")]
        public float allyAtkMultiplier = 1.2f;
        public float allySpeedMultiplier = 1.1f;
        public int allyBuffDuration = 2;

        [Header("Self Buff")]
        public float selfSpeedMultiplier = 1.3f;
        public int selfBuffDuration = 3;

        public override void Apply(CombatUnit caster, CombatUnit[] targets)
        {
            // targets[0] là đồng minh được chọn
            if (targets.Length > 0)
            {
                targets[0].ApplyBuff(StatType.ATK, allyAtkMultiplier, allyBuffDuration);
                targets[0].ApplyBuff(StatType.Speed, allySpeedMultiplier, allyBuffDuration);
            }
            
            // Buff cho bản thân Hana
            caster.ApplyBuff(StatType.Speed, selfSpeedMultiplier, selfBuffDuration);
        }
    }
}