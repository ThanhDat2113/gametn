using UnityEngine;

[CreateAssetMenu(fileName = "HealOnKillEffect", menuName = "RPG/Effects/Heal on Kill")]
public class HealOnKillEffect : SkillEffect
{
    public float healPercent = 0.02f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        int healAmount = Mathf.RoundToInt(caster.MaxHP * healPercent);
        caster.Heal(healAmount);
    }
}