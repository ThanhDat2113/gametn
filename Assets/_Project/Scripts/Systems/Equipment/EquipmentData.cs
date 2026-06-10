using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Inventory/Equipment")]
public class EquipmentData : ItemData
{
    public EquipmentSlot slot;
    public int atkBonus;
    public int defBonus;
    public int hpBonus;
    public int critRateBonus;
    public int critDamageBonus;

    // Đảm bảo itemType là Equipment khi khởi tạo
    void OnEnable()
    {
        itemType = ItemType.Equipment;
    }
}

public enum EquipmentSlot
{
    Weapon,
    Helmet,
    Armor,
    Accessory
}