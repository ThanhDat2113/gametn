using UnityEngine;

namespace Game.Effects
{
    [CreateAssetMenu(fileName = "New ApplyDebuffEffect", menuName = "Effects/Apply Debuff")]
    public class ApplyDebuffEffect : SkillEffect
    {
        public StatusEffectType debuffType;
        public int duration;
        public float value;

        public override void Apply(CombatUnit caster, CombatUnit[] targets)
        {
            foreach (var target in targets)
            {
                Debug.Log($"{caster.UnitName} applies {debuffType} to {target.UnitName} for {duration} turns.");
                target.ApplyStatus(debuffType, duration, value);
            }
        }
    }
}