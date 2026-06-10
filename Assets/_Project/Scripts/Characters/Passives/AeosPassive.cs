using UnityEngine;

/// <summary>
/// Nội tại của Aeos: Aeos bỏ qua 30% phòng thủ của toàn bộ kẻ địch và mỗi khi tiêu diệt được 1 kẻ địch anh nhận thêm 10% xuyên giáp (tối đa 20% cộng thêm).
/// </summary>
public class AeosPassive : PassiveAbility
{
    private const float BASE_PENETRATION = 0.3f;
    private const float BONUS_PER_KILL = 0.1f;
    private const float MAX_BONUS = 0.2f;

    private float bonusArmorPenetration = 0f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        if (Owner == null) return;

        // Gán giá trị xuyên giáp cơ bản
        UpdateArmorPenetration();

        // Đăng ký sự kiện OnKill
        Owner.OnKill += OnOwnerKill;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner == null) return;

        // Reset chỉ số khi combat kết thúc
        Owner.ArmorPenetration -= (BASE_PENETRATION + bonusArmorPenetration);

        // Hủy đăng ký sự kiện
        Owner.OnKill -= OnOwnerKill;
    }

    private void OnOwnerKill(CombatUnit target)
    {
        // Chỉ tăng khi hạ gục kẻ địch
        if (!target.IsAlly(Owner))
        {
            if (bonusArmorPenetration < MAX_BONUS)
            {
                float penetrationToAdd = Mathf.Min(BONUS_PER_KILL, MAX_BONUS - bonusArmorPenetration);
                bonusArmorPenetration += penetrationToAdd;
                
                Debug.Log($"[AeosPassive] Aeos hạ gục {target.UnitName}! Xuyên giáp cộng thêm tăng lên {bonusArmorPenetration * 100}%.");

                // Cập nhật lại tổng chỉ số xuyên giáp
                UpdateArmorPenetration();
            }
        }
    }

    private void UpdateArmorPenetration()
    {
        if (Owner == null) return;
        // Đặt lại giá trị để tránh cộng dồn lỗi
        Owner.ArmorPenetration = BASE_PENETRATION + bonusArmorPenetration;
        Debug.Log($"[AeosPassive] Tổng xuyên giáp của Aeos được cập nhật: {Owner.ArmorPenetration * 100}%.");
    }
}