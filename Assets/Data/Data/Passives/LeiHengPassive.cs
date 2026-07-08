using UnityEngine;

[CreateAssetMenu(fileName = "LeiHengPassive", menuName = "RPG/Passives/LeiHengPassive")]
public class LeiHengPassive : PassiveAbility
{
    private const StatusEffectType EFFECT_TYPE = StatusEffectType.YChi;
    private const float VALUE_PER_STACK = 0.05f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        Owner.OnDamageTaken += OnOwnerTakeDamage;
        Owner.OnDealDamage += OnOwnerDealDamage;
    }

    private void OnOwnerTakeDamage(CombatUnit attacker, int damage)
    {
        Owner.ApplyStatus(EFFECT_TYPE, 999, VALUE_PER_STACK, 1);
    }

    private void OnOwnerDealDamage(CombatUnit target, int damage)
    {
        var yChi = Owner.GetActiveStatus(EFFECT_TYPE);
        if (yChi != null)
        {
            yChi.Stacks = 0; // Reset stacks
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnDamageTaken -= OnOwnerTakeDamage;
            Owner.OnDealDamage -= OnOwnerDealDamage;
        }
    }
}