using UnityEngine;

[CreateAssetMenu(fileName = "HealEffect", menuName = "RPG/Effects/Heal")]
public class HealEffect : SkillEffect
{
    [Range(0f, 1f)] public float healPercent = 0.3f;  // % of MaxHP

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            int amount = Mathf.RoundToInt(target.MaxHP * healPercent);
            target.Heal(amount);
        }
    }
}