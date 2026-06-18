// EquipmentListUI.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentListUI : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button weaponTab;
    public Button helmetTab;
    public Button armorTab;
    public Button accessoryTab;

    [Header("Container")]
    public Transform itemContainer;
    public GameObject itemPrefab;

    [Header("Settings")]
    public int fixedSlotCount = 20;

    private EquipmentSlot currentTab = EquipmentSlot.Weapon;
    private List<EquipmentDragItem> slotItems = new List<EquipmentDragItem>();
    private EquipmentPanel parentPanel;
    private bool isInitialized = false;

    private void Start()
    {
        // Auto-initialize nếu chưa được gọi từ EquipmentPanel
        if (!isInitialized)
        {
            parentPanel = GetComponentInParent<EquipmentPanel>();
            Initialize(parentPanel);
        }
    }

    public void Initialize(EquipmentPanel panel)
    {
        parentPanel = panel;
        isInitialized = true;

        weaponTab.onClick.RemoveAllListeners();
        helmetTab.onClick.RemoveAllListeners();
        armorTab.onClick.RemoveAllListeners();
        accessoryTab.onClick.RemoveAllListeners();

        weaponTab.onClick.AddListener(() => ShowTab(EquipmentSlot.Weapon));
        helmetTab.onClick.AddListener(() => ShowTab(EquipmentSlot.Helmet));
        armorTab.onClick.AddListener(() => ShowTab(EquipmentSlot.Armor));
        accessoryTab.onClick.AddListener(() => ShowTab(EquipmentSlot.Accessory));

        CreateFixedSlots();
        ShowTab(EquipmentSlot.Weapon);
    }

    private void CreateFixedSlots()
    {
        foreach (var slot in slotItems)
            if (slot != null) Destroy(slot.gameObject);
        slotItems.Clear();

        if (itemPrefab == null)
        {
            Debug.LogError("[EquipmentListUI] itemPrefab chưa được gán!");
            return;
        }

        if (itemContainer == null)
        {
            Debug.LogError("[EquipmentListUI] itemContainer chưa được gán!");
            return;
        }

        for (int i = 0; i < fixedSlotCount; i++)
        {
            GameObject go = Instantiate(itemPrefab, itemContainer);
            var dragItem = go.GetComponent<EquipmentDragItem>();
            if (dragItem == null)
                dragItem = go.AddComponent<EquipmentDragItem>();

            dragItem.InitializeDummy(parentPanel);
            go.SetActive(true);
            slotItems.Add(dragItem);
        }

        Debug.Log($"[EquipmentListUI] Created {slotItems.Count} slots.");
    }

    public void Refresh()
    {
        ShowTab(currentTab);
    }

    private void ShowTab(EquipmentSlot slot)
    {
        currentTab = slot;

        // Null guard
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[EquipmentListUI] InventoryManager.Instance is null, showing empty slots.");
            foreach (var item in slotItems)
                item.InitializeDummy(parentPanel);
            return;
        }

        var inventory = InventoryManager.Instance.inventory;

        var equips = inventory.slots
            .Where(s => s.item is EquipmentData equip && equip.slot == slot)
            .Select(s => s.item as EquipmentData)
            .ToList();

        for (int i = 0; i < slotItems.Count; i++)
        {
            if (i < equips.Count && equips[i] != null)
                slotItems[i].Initialize(equips[i], parentPanel);
            else
                slotItems[i].InitializeDummy(parentPanel);

            slotItems[i].gameObject.SetActive(true);
        }

        Debug.Log($"[EquipmentListUI] ShowTab {slot}, items: {equips.Count}");
    }
}