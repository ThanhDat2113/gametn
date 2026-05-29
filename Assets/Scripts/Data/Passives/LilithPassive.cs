using UnityEngine;

[CreateAssetMenu(fileName = "LilithPassive", menuName = "RPG/Passives/LilithPassive")]
public class LilithPassive : PassiveAbility
{
    private const StatusEffectType EFFECT_TYPE = StatusEffectType.BuiSao;
    private const float VALUE_PER_STACK = 0.05f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        Owner.OnSpendAP += OnOwnerSpendAP;
    }

    private void OnOwnerSpendAP(int amount)
    {
        if (amount > 0)
        {
            Owner.ApplyStatus(EFFECT_TYPE, 999, VALUE_PER_STACK, amount);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnSpendAP -= OnOwnerSpendAP;
        }
    }
}