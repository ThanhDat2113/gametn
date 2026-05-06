using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public Transform slotContainer;         // Grid Layout Group container
    public GameObject slotPrefab;           // Prefab slot (hình vuông, có Image và Text)
    public int totalSlots = 30;             // Số slot hiển thị cố định

    private List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();

    void Start()
    {
        // Tạo sẵn các slot trống
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject go = Instantiate(slotPrefab, slotContainer);
            var slotUI = go.GetComponent<InventorySlotUI>();
            if (slotUI == null) slotUI = go.AddComponent<InventorySlotUI>();
            slotUI.SetEmpty();
            slotUIList.Add(slotUI);
        }

        // Đăng ký sự kiện thay đổi inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.inventory.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }
    }

    void RefreshUI()
    {
        // Reset tất cả slot về trống
        foreach (var slotUI in slotUIList)
            slotUI.SetEmpty();

        // Đổ item vào các slot đầu tiên
        int index = 0;
        foreach (var invSlot in InventoryManager.Instance.inventory.slots)
        {
            if (invSlot.item == null) continue;
            if (index < slotUIList.Count)
            {
                slotUIList[index].Setup(invSlot.item, invSlot.amount);
                index++;
            }
        }
    }
}