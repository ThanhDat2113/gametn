using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Inventory/Equipment")]
public class EquipmentData : ItemData
{
    public EquipmentSlot slot;
    
    [Header("Stat Bonuses")]
    public int hpBonus;
    public int atkBonus;
    public int pdefBonus;
    public int mdefBonus;
    public int speedBonus;

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