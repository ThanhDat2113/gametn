using UnityEngine;

[CreateAssetMenu(fileName = "NicholasPassive", menuName = "RPG/Passives/NicholasPassive")]
public class NicholasPassive : PassiveAbility
{
    private const float CRIT_CHANCE_BONUS = 0.2f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        Owner.CritChance += CRIT_CHANCE_BONUS;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.CritChance -= CRIT_CHANCE_BONUS;
        }
    }
}