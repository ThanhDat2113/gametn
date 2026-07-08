using UnityEngine;

[CreateAssetMenu(fileName = "NewArmorPenetrationEffect", menuName = "Skill/Effects/ArmorPenetration")]
public class ArmorPenetrationEffect : SkillEffect
{
    [Tooltip("Phần trăm xuyên giáp cơ bản")]
    public float basePercentage = 0.3f;
    [Tooltip("Phần trăm cộng thêm mỗi khi hạ gục")]
    public float bonusPerKill = 0.1f;
    [Tooltip("Tối đa cộng thêm")]
    public float maxBonus = 0.2f;

    public override void Apply(CombatUnit user, CombatUnit[] targets)
    {
        // Logic được xử lý trong AeosPassive.cs
    }
}