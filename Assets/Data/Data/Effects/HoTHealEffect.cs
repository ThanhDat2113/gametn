using UnityEngine;

[CreateAssetMenu(fileName = "HoTHealEffect", menuName = "RPG/Effects/HoT Heal")]
public class HoTHealEffect : SkillEffect
{
    public float healPercent = 0.1f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        int healAmount = Mathf.RoundToInt(caster.MaxHP * healPercent);
        foreach (var target in targets)
        {
            target.Heal(healAmount);
        }
    }
}