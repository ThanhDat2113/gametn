using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [SerializeField] private List<CharacterEquipment> allEquipment = new List<CharacterEquipment>();

    public event System.Action<CharacterData> OnEquipmentChanged;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    // Lấy trang bị của nhân vật (nếu chưa có thì tạo mới)
    public CharacterEquipment GetEquipment(CharacterData character)
    {
        var eq = allEquipment.Find(e => e.character == character);
        if (eq == null)
        {
            eq = new CharacterEquipment { character = character };
            allEquipment.Add(eq);
        }
        return eq;
    }

    // Gắn trang bị vào slot (bất kỳ loại trang bị nào cũng có thể gắn vào bất kỳ slot nào)
    public bool Equip(CharacterData character, EquipmentSlot slot, EquipmentData equip)
    {
        if (equip == null) return false;

        var eq = GetEquipment(character);
        var old = eq.GetEquipment(slot);
        
        // Nếu có trang bị cũ, trả về inventory
        if (old != null)
        {
            InventoryManager.Instance.AddItem(old);
        }

        eq.SetEquipment(slot, equip);
        // Xóa trang bị khỏi inventory
        InventoryManager.Instance.RemoveItem(equip);

        OnEquipmentChanged?.Invoke(character);
        Debug.Log($"[Equipment] {character.characterName} equipped {equip.itemName} to {slot}");
        return true;
    }

    // Tháo trang bị về inventory
    public void Unequip(CharacterData character, EquipmentSlot slot)
    {
        var eq = GetEquipment(character);
        var equip = eq.GetEquipment(slot);
        if (equip == null) return;

        eq.SetEquipment(slot, null);
        InventoryManager.Instance.AddItem(equip);
        OnEquipmentChanged?.Invoke(character);
        Debug.Log($"[Equipment] {character.characterName} unequipped {equip.itemName}");
    }
}