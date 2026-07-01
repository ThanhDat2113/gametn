using UnityEngine;

namespace Game.Effects
{
    [CreateAssetMenu(fileName = "HanaSkill3Effect", menuName = "RPG/Effects/Custom/Hana Skill 3")]
    public class HanaSkill3Effect : SkillEffect
    {
        public override void Apply(CombatUnit caster, CombatUnit[] targets)
        {
            // targets[0] là đồng minh được chọn
            // Trong side-based system, effect này không còn ý nghĩa đẩy lượt
            // Có thể thay bằng buff hành động khác nếu cần
            Debug.Log($"[HanaSkill3] Không còn hỗ trợ GrantImmediateTurn trong side-based system.");
        }
    }
}