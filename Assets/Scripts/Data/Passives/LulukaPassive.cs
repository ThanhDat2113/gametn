using UnityEngine;

/// <summary>
/// Nội tại của Luluka: Mọi đòn đánh từ nhân vật này sẽ hồi phục cho bản thân bằng 20% sát thương gây ra.
/// </summary>
public class LulukaPassive : PassiveAbility
{
    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        owner.OnDealDamage += OnOwnerDealDamage;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnDealDamage -= OnOwnerDealDamage;
        }
    }

    private void OnOwnerDealDamage(CombatUnit target, int damageAmount)
    {
        int healAmount = Mathf.FloorToInt(damageAmount * 0.2f);
        Owner.Heal(healAmount);
    }
}