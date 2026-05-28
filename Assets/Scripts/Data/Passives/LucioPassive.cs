using UnityEngine;

[CreateAssetMenu(fileName = "LucioPassive", menuName = "RPG/Passives/LucioPassive")]
public class LucioPassive : PassiveAbility
{
    private const StatusEffectType BUFF_TYPE = StatusEffectType.SieuViet;
    private const StatusEffectType TARGET_DEBUFF = StatusEffectType.DiemYeu;
    private const float VALUE_PER_STACK = 0.10f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        Owner.OnDealDamage += OnOwnerDealDamage;
    }

    private void OnOwnerDealDamage(CombatUnit target, int damage)
    {
        if (target != null && target.HasStatus(TARGET_DEBUFF))
        {
            Owner.ApplyStatus(BUFF_TYPE, 999, VALUE_PER_STACK, 1);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnDealDamage -= OnOwnerDealDamage;
        }
    }
}