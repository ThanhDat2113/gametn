using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Tabs (Buttons)")]
    public Button tabWeaponBtn;
    public Button tabHelmetBtn;
    public Button tabArmorBtn;
    public Button tabAccessoryBtn;
    public Button tabMaterialBtn;

    [Header("Panels (Container của từng tab)")]
    public GameObject weaponPanel;
    public GameObject helmetPanel;
    public GameObject armorPanel;
    public GameObject accessoryPanel;
    public GameObject materialPanel;

    [Header("Slot Containers (Grid Layout Group bên trong mỗi panel)")]
    public Transform weaponSlotContainer;
    public Transform helmetSlotContainer;
    public Transform armorSlotContainer;
    public Transform accessorySlotContainer;
    public Transform materialSlotContainer;

    [Header("Slot Settings")]
    public GameObject slotPrefab;
    public int totalSlots = 30;

    private List<InventorySlotUI> weaponSlotUIList = new List<InventorySlotUI>();
    private List<InventorySlotUI> helmetSlotUIList = new List<InventorySlotUI>();
    private List<InventorySlotUI> armorSlotUIList = new List<InventorySlotUI>();
    private List<InventorySlotUI> accessorySlotUIList = new List<InventorySlotUI>();
    private List<InventorySlotUI> materialSlotUIList = new List<InventorySlotUI>();

    void Start()
    {
        // Tạo các slot tĩnh cho từng panel
        CreateSlots(weaponSlotContainer, weaponSlotUIList);
        CreateSlots(helmetSlotContainer, helmetSlotUIList);
        CreateSlots(armorSlotContainer, armorSlotUIList);
        CreateSlots(accessorySlotContainer, accessorySlotUIList);
        CreateSlots(materialSlotContainer, materialSlotUIList);

        // Đăng ký sự kiện thay đổi inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.inventory.OnInventoryChanged += RefreshUI;

        RefreshUI();

        // Gán sự kiện cho các nút tab
        tabWeaponBtn.onClick.AddListener(() => ShowTab(EquipmentSlot.Weapon));
        tabHelmetBtn.onClick.AddListener(() => ShowTab(EquipmentSlot.Helmet));
        tabArmorBtn.onClick.AddListener(() => ShowTab(EquipmentSlot.Armor));
        tabAccessoryBtn.onClick.AddListener(() => ShowTab(EquipmentSlot.Accessory));
        tabMaterialBtn.onClick.AddListener(() => ShowTab(null)); // null = material

        // Mặc định hiện tab vũ khí
        ShowTab(EquipmentSlot.Weapon);
    }

    void CreateSlots(Transform container, List<InventorySlotUI> slotList)
    {
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject go = Instantiate(slotPrefab, container);
            var slotUI = go.GetComponent<InventorySlotUI>();
            if (slotUI == null) slotUI = go.AddComponent<InventorySlotUI>();
            slotUI.SetEmpty();
            slotList.Add(slotUI);
        }
    }

    void RefreshUI()
    {
        // Reset tất cả slot về trống
        ResetSlots(weaponSlotUIList);
        ResetSlots(helmetSlotUIList);
        ResetSlots(armorSlotUIList);
        ResetSlots(accessorySlotUIList);
        ResetSlots(materialSlotUIList);

        var inventory = InventoryManager.Instance.inventory;

        int weaponIdx = 0, helmetIdx = 0, armorIdx = 0, accessoryIdx = 0, materialIdx = 0;

        foreach (var invSlot in inventory.slots)
        {
            if (invSlot.item == null) continue;

            // Nếu là trang bị
            if (invSlot.item is EquipmentData equip)
            {
                switch (equip.slot)
                {
                    case EquipmentSlot.Weapon:
                        if (weaponIdx < weaponSlotUIList.Count)
                            weaponSlotUIList[weaponIdx++].Setup(equip, invSlot.amount);
                        break;
                    case EquipmentSlot.Helmet:
                        if (helmetIdx < helmetSlotUIList.Count)
                            helmetSlotUIList[helmetIdx++].Setup(equip, invSlot.amount);
                        break;
                    case EquipmentSlot.Armor:
                        if (armorIdx < armorSlotUIList.Count)
                            armorSlotUIList[armorIdx++].Setup(equip, invSlot.amount);
                        break;
                    case EquipmentSlot.Accessory:
                        if (accessoryIdx < accessorySlotUIList.Count)
                            accessorySlotUIList[accessoryIdx++].Setup(equip, invSlot.amount);
                        break;
                }
            }
            // Nếu là nguyên liệu
            else if (invSlot.item.itemType == ItemType.Material)
            {
                if (materialIdx < materialSlotUIList.Count)
                    materialSlotUIList[materialIdx++].Setup(invSlot.item, invSlot.amount);
            }
        }
    }

    void ResetSlots(List<InventorySlotUI> slotList)
    {
        foreach (var slot in slotList) slot.SetEmpty();
    }

    void ShowTab(EquipmentSlot? slot)
    {
        // Ẩn tất cả panel
        weaponPanel.SetActive(false);
        helmetPanel.SetActive(false);
        armorPanel.SetActive(false);
        accessoryPanel.SetActive(false);
        materialPanel.SetActive(false);

        if (slot == null)
            materialPanel.SetActive(true);
        else
        {
            switch (slot.Value)
            {
                case EquipmentSlot.Weapon: weaponPanel.SetActive(true); break;
                case EquipmentSlot.Helmet: helmetPanel.SetActive(true); break;
                case EquipmentSlot.Armor: armorPanel.SetActive(true); break;
                case EquipmentSlot.Accessory: accessoryPanel.SetActive(true); break;
            }
        }
    }
}