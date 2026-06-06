using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentListUI : MonoBehaviour
{
    [Header("Tab Buttons")]
    public UnityEngine.UI.Button weaponTab;
    public UnityEngine.UI.Button helmetTab;
    public UnityEngine.UI.Button armorTab;
    public UnityEngine.UI.Button accessoryTab;

    [Header("Container")]
    public Transform itemContainer;
    public GameObject itemPrefab;

    private EquipmentSlot currentTab = EquipmentSlot.Weapon;
    private List<EquipmentDragItem> currentItems = new List<EquipmentDragItem>();
    private EquipmentPanel parentPanel;

    public void Initialize(EquipmentPanel panel)
    {
        parentPanel = panel;
        weaponTab.onClick.AddListener(() => ShowTab(EquipmentSlot.Weapon));
        helmetTab.onClick.AddListener(() => ShowTab(EquipmentSlot.Helmet));
        armorTab.onClick.AddListener(() => ShowTab(EquipmentSlot.Armor));
        accessoryTab.onClick.AddListener(() => ShowTab(EquipmentSlot.Accessory));
        ShowTab(EquipmentSlot.Weapon);
    }

    public void Refresh()
    {
        ShowTab(currentTab);
    }

    private void ShowTab(EquipmentSlot slot)
    {
        currentTab = slot;
        // Xóa items cũ
        foreach (var item in currentItems)
            if (item != null) Destroy(item.gameObject);
        currentItems.Clear();

        // Lọc inventory theo loại và slot
        var inventory = InventoryManager.Instance.inventory;
        var equips = inventory.slots
            .Where(s => s.item is EquipmentData equip && equip.slot == slot)
            .Select(s => s.item as EquipmentData)
            .ToList();

        foreach (var equip in equips)
        {
            GameObject go = Instantiate(itemPrefab, itemContainer);
            var dragItem = go.GetComponent<EquipmentDragItem>();
            if (dragItem == null) dragItem = go.AddComponent<EquipmentDragItem>();
            dragItem.Initialize(equip, parentPanel);
            currentItems.Add(dragItem);
        }
    }
}