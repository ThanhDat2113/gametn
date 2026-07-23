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
    public Button tabMaterialBtn; // giữ lại nhưng sẽ ẩn

    [Header("Panels (Container của từng tab)")]
    public GameObject weaponPanel;
    public GameObject helmetPanel;
    public GameObject armorPanel;
    public GameObject accessoryPanel;
    public GameObject materialPanel; // giữ lại nhưng sẽ ẩn

    [Header("Slot Containers (Grid Layout Group bên trong mỗi panel)")]
    public Transform weaponSlotContainer;
    public Transform helmetSlotContainer;
    public Transform armorSlotContainer;
    public Transform accessorySlotContainer;
    public Transform materialSlotContainer; // giữ lại nhưng không dùng

    [Header("Slot Settings")]
    public GameObject slotPrefab;
    public int totalSlots = 30;

    private List<InventorySlotUI> weaponSlotUIList = new List<InventorySlotUI>();
    private List<InventorySlotUI> helmetSlotUIList = new List<InventorySlotUI>();
    private List<InventorySlotUI> armorSlotUIList = new List<InventorySlotUI>();
    private List<InventorySlotUI> accessorySlotUIList = new List<InventorySlotUI>();
    // Không cần list cho material

    void Awake()
    {
        // Ẩn tab Material và panel Material
        if (tabMaterialBtn != null)
            tabMaterialBtn.gameObject.SetActive(false);
        if (materialPanel != null)
            materialPanel.SetActive(false);
    }

    void Start()
    {
        // Tạo slot cho các panel còn lại (bỏ material)
        CreateSlots(weaponSlotContainer, weaponSlotUIList);
        CreateSlots(helmetSlotContainer, helmetSlotUIList);
        CreateSlots(armorSlotContainer, armorSlotUIList);
        CreateSlots(accessorySlotContainer, accessorySlotUIList);
        // KHÔNG tạo slot cho material

        // Đăng ký sự kiện thay đổi inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.inventory.OnInventoryChanged += RefreshUI;

        RefreshUI();

        // Gán sự kiện cho các nút tab (bỏ material)
        tabWeaponBtn.onClick.AddListener(() => ShowTab(EquipmentSlot.Weapon));
        tabHelmetBtn.onClick.AddListener(() => ShowTab(EquipmentSlot.Helmet));
        tabArmorBtn.onClick.AddListener(() => ShowTab(EquipmentSlot.Armor));
        tabAccessoryBtn.onClick.AddListener(() => ShowTab(EquipmentSlot.Accessory));
        // Không gán cho tabMaterialBtn

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
        // Không reset material

        var inventory = InventoryManager.Instance.inventory;

        int weaponIdx = 0, helmetIdx = 0, armorIdx = 0, accessoryIdx = 0;

        foreach (var invSlot in inventory.slots)
        {
            if (invSlot.item == null) continue;

            // Chỉ xử lý trang bị (bỏ qua Material)
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
            // Bỏ qua itemType == Material
        }
    }

    void ResetSlots(List<InventorySlotUI> slotList)
    {
        foreach (var slot in slotList) slot.SetEmpty();
    }

    void ShowTab(EquipmentSlot slot)
    {
        // Ẩn tất cả panel
        weaponPanel.SetActive(false);
        helmetPanel.SetActive(false);
        armorPanel.SetActive(false);
        accessoryPanel.SetActive(false);
        // materialPanel đã ẩn từ đầu, không cần set

        // Hiện panel tương ứng
        switch (slot)
        {
            case EquipmentSlot.Weapon: weaponPanel.SetActive(true); break;
            case EquipmentSlot.Helmet: helmetPanel.SetActive(true); break;
            case EquipmentSlot.Armor: armorPanel.SetActive(true); break;
            case EquipmentSlot.Accessory: accessoryPanel.SetActive(true); break;
        }
    }
}