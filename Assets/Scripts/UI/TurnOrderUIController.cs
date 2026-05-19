using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrderUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo Prefab/GameObject mẫu của icon vào đây.")]
    public ActionSlotUI iconPrefab;
    [Tooltip("Kéo container chứa các icon (có Horizontal Layout Group) vào đây.")]
    public RectTransform iconContainer;

    [Header("Colors")]
    public Color playerColor = new Color(0.2f, 0.3f, 0.8f, 1f); // Alpha = 1
    public Color enemyColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Alpha = 1
    public Color currentTurnColor = Color.yellow;

    [Header("Display Settings")]
    public int displayCount = 5;

    private List<ActionSlotUI> activeIcons = new List<ActionSlotUI>();
    private List<CombatUnit> fullTurnOrder = new List<CombatUnit>();
    private CombatManager combatManager;

    void Start()
    {
        if (iconPrefab == null || iconContainer == null)
        {
            Debug.LogError("[TurnOrderUI] Vui lòng gán Icon Prefab và Container trong Inspector!");
            gameObject.SetActive(false);
            return;
        }
        iconPrefab.gameObject.SetActive(false); // Ẩn prefab mẫu

        combatManager = CombatManager.Instance;
        if (combatManager != null)
        {
            combatManager.OnRoundSetup += SetupInitialTurnOrder;
            combatManager.OnUnitTurnStart += UpdateVisibleIcons;
        }
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnRoundSetup -= SetupInitialTurnOrder;
            combatManager.OnUnitTurnStart -= UpdateVisibleIcons;
        }
    }

    private void SetupInitialTurnOrder(List<CombatUnit> turnOrder)
    {
        // Xóa các icon cũ
        foreach (var icon in activeIcons)
        {
            Destroy(icon.gameObject);
        }
        activeIcons.Clear();

        // Tạo icon mới cho mỗi unit trong turn order, theo đúng thứ tự
        foreach (var unit in turnOrder)
        {
            ActionSlotUI newIcon = Instantiate(iconPrefab, iconContainer);
            newIcon.SetupForTurnOrder(unit, unit.IsPlayer ? playerColor : enemyColor);
            activeIcons.Add(newIcon);
        }

        // Cập nhật hiển thị lần đầu, nhưng không làm gì cả ở đây.
        // Sự kiện OnUnitTurnStart đầu tiên sẽ tự động xử lý việc highlight icon đầu tiên.
    }

    private void UpdateVisibleIcons(CombatUnit currentUnit)
    {
        // Tìm vị trí của unit hiện tại trong danh sách icon đang hoạt động
        int currentIndex = activeIcons.FindIndex(icon => icon.LinkedUnit == currentUnit);

        if (currentIndex == -1)
        {
            // Có thể unit đã chết và bị loại bỏ, hoặc một lỗi khác.
            // Để an toàn, ta không làm gì cả.
            Debug.LogWarning($"[TurnOrderUI] Không tìm thấy icon cho unit: {currentUnit.UnitName}");
            return;
        }

        // Cập nhật trạng thái của tất cả các icon dựa trên vị trí của unit hiện tại
        for (int i = 0; i < activeIcons.Count; i++)
        {
            var icon = activeIcons[i];

            // Chỉ hiển thị các icon trong "cửa sổ" hiển thị (từ lượt hiện tại trở đi)
            if (i >= currentIndex && i < currentIndex + displayCount)
            {
                icon.gameObject.SetActive(true);

                // Highlight icon của lượt hiện tại
                if (i == currentIndex)
                {
                    icon.SetBorderColor(currentTurnColor);
                }
                else // Reset màu cho các icon khác
                {
                    icon.SetBorderColor(icon.LinkedUnit.IsPlayer ? playerColor : enemyColor);
                }
            }
            else
            {
                // Ẩn các icon nằm ngoài cửa sổ hiển thị
                icon.gameObject.SetActive(false);
            }
        }
    }
}