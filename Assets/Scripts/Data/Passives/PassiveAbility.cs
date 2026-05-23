using UnityEngine;

/// <summary>
/// Lớp cơ sở trừu tượng cho tất cả các nội tại (Passive Abilities).
/// Kế thừa từ ScriptableObject để có thể tạo và quản lý dưới dạng asset trong Unity.
/// </summary>
public abstract class PassiveAbility : ScriptableObject
{
    [Header("Mô tả nội tại")]
    [TextArea]
    public string description;

    protected CombatUnit Owner { get; private set; }

    /// <summary>
    /// Được gọi khi nội tại được khởi tạo cho một CombatUnit.
    /// Đây là nơi để đăng ký các sự kiện (events) của CombatUnit.
    /// </summary>
    /// <param name="owner">Đơn vị sở hữu nội tại này.</param>
    public virtual void Initialize(CombatUnit owner)
    {
        this.Owner = owner;
    }

    /// <summary>
    /// Dọn dẹp các đăng ký sự kiện khi không cần thiết nữa.
    /// </summary>
    public virtual void Cleanup()
    {
        // Hủy đăng ký các sự kiện ở đây nếu cần
    }

    // Các phương thức ảo (virtual) để các lớp con có thể override
    // Đây là những "hook" vào các sự kiện của CombatUnit

    public virtual void OnTurnStart() { }
    public virtual void OnDealDamage(CombatUnit target, int damage) { }
    public virtual void OnTakeDamage(CombatUnit attacker, int damage) { }
    public virtual void OnHeal(int amount) { }
    public virtual void OnKill(CombatUnit target) { }
    public virtual void OnSpendAP(int amount) { }
    public virtual void OnDied() { }
}