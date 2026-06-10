using UnityEngine;

[CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "RPG/Effects/Apply Status")]
public class ApplyStatusEffect : SkillEffect
{
    public StatusEffectType status;
    public int duration = 1;
    public float value = 0f;
    public int stacks = 1;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        int finalStacks = stacks;

        // "Hack" để đọc charge level từ description của skill instance
        if (int.TryParse(caster.SelectedSkill.description, out int chargeLevel) && chargeLevel > 0)
        {
            finalStacks *= chargeLevel;
            Debug.Log($"<color=yellow>[Chargeable] Skill được sạc {chargeLevel} lần. Áp dụng {finalStacks} stacks của {status}.</color>");
        }

        foreach (var target in targets)
        {
            if (target.IsAlive)
            {
                target.ApplyStatus(status, duration, value, finalStacks);
            }
        }
    }
}