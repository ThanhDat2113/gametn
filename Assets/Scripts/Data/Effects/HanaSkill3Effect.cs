using UnityEngine;

namespace Game.Effects
{
    [CreateAssetMenu(fileName = "HanaSkill3Effect", menuName = "RPG/Effects/Custom/Hana Skill 3")]
    public class HanaSkill3Effect : SkillEffect
    {
        public override void Apply(CombatUnit caster, CombatUnit[] targets)
        {
            // targets[0] là đồng minh được chọn
            if (targets.Length > 0)
            {
                CombatManager.Instance.GrantImmediateTurn(targets[0]);
            }
        }
    }
}