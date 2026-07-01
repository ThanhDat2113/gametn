using UnityEngine;

namespace Game.Effects
{
    [CreateAssetMenu(fileName = "HanaSkill3Effect", menuName = "RPG/Effects/Custom/Hana Skill 3")]
    public class HanaSkill3Effect : SkillEffect
    {
        public override void Apply(CombatUnit caster, CombatUnit[] targets)
        {
            // targets[0] là đồng minh được chọn: cho act thêm 1 lần
            if (targets.Length > 0 && targets[0] != null && targets[0].IsAlive)
            {
                if (CombatManager.Instance != null)
                {
                    CombatManager.Instance.GrantExtraAction(targets[0]);
                    Debug.Log($"[HanaSkill3] {targets[0].UnitName} được act thêm 1 lần!");
                }
            }
        }
    }
}