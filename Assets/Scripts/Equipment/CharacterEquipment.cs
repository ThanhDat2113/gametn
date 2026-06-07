using System;
using System.Collections.Generic;
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

    // Tính toán stat bonus từ trang bị
    public int GetATKBonus() => (weapon?.atkBonus ?? 0) + (helmet?.atkBonus ?? 0) + (armor?.atkBonus ?? 0) + (accessory?.atkBonus ?? 0);
    public int GetDEFBonus() => (weapon?.defBonus ?? 0) + (helmet?.defBonus ?? 0) + (armor?.defBonus ?? 0) + (accessory?.defBonus ?? 0);
    public int GetHPBonus() => (weapon?.hpBonus ?? 0) + (helmet?.hpBonus ?? 0) + (armor?.hpBonus ?? 0) + (accessory?.hpBonus ?? 0);
}