using UnityEngine;

/// <summary>
/// Lớp cơ sở cho tất cả các nội tại (Passive Abilities).
/// KHÔNG phải ScriptableObject - không cần tạo asset.
/// Kéo file .cs trực tiếp vào CharacterData.passiveScript.
/// </summary>
public abstract class PassiveAbility
{
    [TextArea]
    public string description;

    protected CombatUnit Owner { get; private set; }

    public virtual void Initialize(CombatUnit owner)
    {
        this.Owner = owner;
    }

    public virtual void Cleanup()
    {
    }

    public virtual void OnTurnStart() { }
    public virtual void OnDealDamage(CombatUnit target, int damage) { }
    public virtual void OnTakeDamage(CombatUnit attacker, int damage) { }
    public virtual void OnHeal(int amount) { }
    public virtual void OnKill(CombatUnit target) { }
    public virtual void OnSpendAP(int amount) { }
    public virtual void OnDied() { }
}