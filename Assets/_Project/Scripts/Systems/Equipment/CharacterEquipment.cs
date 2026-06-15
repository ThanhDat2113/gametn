using System;
using UnityEngine;

[Serializable]
public class CharacterEquipment
{
    public CharacterData character;
    public EquipmentData weapon;
    public EquipmentData helmet;
    public EquipmentData armor;
    public EquipmentData accessory;

    public EquipmentData GetEquipment(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => weapon,
            EquipmentSlot.Helmet => helmet,
            EquipmentSlot.Armor => armor,
            EquipmentSlot.Accessory => accessory,
            _ => null
        };
    }

    public void SetEquipment(EquipmentSlot slot, EquipmentData equip)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: weapon = equip; break;
            case EquipmentSlot.Helmet: helmet = equip; break;
            case EquipmentSlot.Armor: armor = equip; break;
            case EquipmentSlot.Accessory: accessory = equip; break;
        }
    }

    public int GetHPBonus() => (weapon?.hpBonus ?? 0) + (helmet?.hpBonus ?? 0) + (armor?.hpBonus ?? 0) + (accessory?.hpBonus ?? 0);
    public int GetATKBonus() => (weapon?.atkBonus ?? 0) + (helmet?.atkBonus ?? 0) + (armor?.atkBonus ?? 0) + (accessory?.atkBonus ?? 0);
    public int GetPDEFBonus() => (weapon?.pdefBonus ?? 0) + (helmet?.pdefBonus ?? 0) + (armor?.pdefBonus ?? 0) + (accessory?.pdefBonus ?? 0);
    public int GetMDEFBonus() => (weapon?.mdefBonus ?? 0) + (helmet?.mdefBonus ?? 0) + (armor?.mdefBonus ?? 0) + (accessory?.mdefBonus ?? 0);
    public int GetSpeedBonus() => (weapon?.speedBonus ?? 0) + (helmet?.speedBonus ?? 0) + (armor?.speedBonus ?? 0) + (accessory?.speedBonus ?? 0);
}