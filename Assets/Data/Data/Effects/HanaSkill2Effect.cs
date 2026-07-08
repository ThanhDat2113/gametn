using UnityEngine;

namespace Game.Effects
{
    [CreateAssetMenu(fileName = "HanaSkill2Effect", menuName = "RPG/Effects/Custom/Hana Skill 2")]
    public class HanaSkill2Effect : SkillEffect
    {
        [Header("Ally Buff")]
        public float allyAtkMultiplier = 1.2f;
        public int allyBuffDuration = 2;

        [Header("Self Buff")]
        public float selfEmpowerValue = 0.5f;
        public int selfEmpowerDuration = 3;

        public override void Apply(CombatUnit caster, CombatUnit[] targets)
        {
            // targets[0] là đồng minh được chọn: buff ATK
            if (targets.Length > 0)
            {
                targets[0].ApplyBuff(StatType.ATK, allyAtkMultiplier, allyBuffDuration);
            }
            
            // Buff cho bản thân Hana: Empower (tăng sát thương đòn đánh tiếp theo)
            caster.ApplyStatus(StatusEffectType.Empowered, selfEmpowerDuration, selfEmpowerValue, 1);
        }
    }
}