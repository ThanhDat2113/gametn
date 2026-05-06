using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "Inventory/Equipment")]
public class EquipmentData : ItemData
{
    public EquipmentSlot slot;
    public int atkBonus;
    public int defBonus;
    public int hpBonus;
    public int critRateBonus;   // %
    public int critDamageBonus; // %
}

public enum EquipmentSlot
{
    Weapon,
    Armor,
    Helmet,
    Gloves,
    Boots,
    Accessory
}