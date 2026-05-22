using UnityEngine;

[CreateAssetMenu(fileName = "BurnEffect", menuName = "RPG/Effects/Burn")]
public class BurnEffect : SkillEffect
{
    [Tooltip("Sát thương mỗi lượt, tính theo % ATK của caster. 0.2 = 20% ATK.")]
    public float damagePerTurnMultiplier = 0.2f;
    public int duration = 2;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Tính sát thương dựa trên ATK của caster
        int damagePerTurn = Mathf.RoundToInt(caster.ATK * damagePerTurnMultiplier);

        foreach (var target in targets)
        {
            target.ApplyStatus(StatusEffectType.ThieuDot, duration, damagePerTurn);
        }
    }
}