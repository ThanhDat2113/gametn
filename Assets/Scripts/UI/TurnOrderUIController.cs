using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrderUIController : MonoBehaviour
{
    [Header("UI References")]
    public ActionSlotUI iconPrefab;
    public RectTransform iconContainer;

    [Header("Colors")]
    public Color playerColor = new Color(0.2f, 0.3f, 0.8f, 1f);
    public Color enemyColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    public Color currentTurnColor = Color.yellow;

    [Header("Action Value Bar")]
    public bool showActionValueBar = true;
    public Slider actionValueSliderPrefab;

    private List<TurnOrderIcon> allIcons = new List<TurnOrderIcon>();
    private CombatManager combatManager;

    // Lớp nội bộ để lưu icon + action value bar
    private class TurnOrderIcon
    {
        public ActionSlotUI Icon { get; set; }
        public Slider AvBar { get; set; }
        public CombatUnit LinkedUnit { get; set; }
    }

    void Start()
    {
        if (iconPrefab == null || iconContainer == null)
        {
            Debug.LogError("[TurnOrderUI] Vui lòng gán Icon Prefab và Container trong Inspector!");
            return;
        }
        iconPrefab.gameObject.SetActive(false);

        combatManager = CombatManager.Instance;
        if (combatManager != null)
        {
            combatManager.OnTurnOrderUpdated += RebuildTurnOrderUI;
            combatManager.OnUnitTurnStart += OnUnitTurnStart;
            combatManager.OnActionResolved += OnActionResolved;
        }
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnTurnOrderUpdated -= RebuildTurnOrderUI;
            combatManager.OnUnitTurnStart -= OnUnitTurnStart;
            combatManager.OnActionResolved -= OnActionResolved;
        }
    }

    void Update()
    {
        // Cập nhật action value bar mỗi frame
        if (showActionValueBar && allIcons.Count > 0)
        {
            foreach (var entry in allIcons)
            {
                if (entry.LinkedUnit != null && entry.AvBar != null)
                {
                    entry.AvBar.value = entry.LinkedUnit.CurrentActionValue / CombatUnit.ACTION_THRESHOLD;
                }
            }
        }
    }

    public void RebuildTurnOrderUI(List<CombatUnit> turnOrder)
    {
        if (turnOrder == null) return;

        // Xóa icon cũ
        foreach (var entry in allIcons)
        {
            if (entry.Icon != null) Destroy(entry.Icon.gameObject);
            if (entry.AvBar != null) Destroy(entry.AvBar.gameObject);
        }
        allIcons.Clear();

        // Tạo icon cho tất cả unit, sắp xếp theo action value giảm dần
        foreach (var unit in turnOrder)
        {
            if (unit == null) continue;

            // Tạo icon
            ActionSlotUI newIcon = Instantiate(iconPrefab, iconContainer);
            newIcon.SetupForTurnOrder(unit, unit.IsPlayer ? playerColor : enemyColor);
            newIcon.gameObject.SetActive(true);

            // Tạo action value bar nếu được bật
            Slider avBar = null;
            if (showActionValueBar && actionValueSliderPrefab != null)
            {
                avBar = Instantiate(actionValueSliderPrefab, iconContainer);
                avBar.gameObject.SetActive(true);
                avBar.minValue = 0f;
                avBar.maxValue = 1f;
                avBar.value = unit.CurrentActionValue / CombatUnit.ACTION_THRESHOLD;

                // Đặt màu cho bar
                var fillImage = avBar.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = unit.IsPlayer ? playerColor : enemyColor;
                }
            }

            allIcons.Add(new TurnOrderIcon
            {
                Icon = newIcon,
                AvBar = avBar,
                LinkedUnit = unit
            });
        }
    }

    private void OnUnitTurnStart(CombatUnit currentUnit)
    {
        // Highlight icon của lượt hiện tại
        int currentIndex = allIcons.FindIndex(entry => entry.LinkedUnit == currentUnit);
        if (currentIndex == -1) return;

        for (int i = 0; i < allIcons.Count; i++)
        {
            var entry = allIcons[i];
            if (i == currentIndex)
                entry.Icon.SetBorderColor(currentTurnColor);
            else
                entry.Icon.SetBorderColor(entry.LinkedUnit.IsPlayer ? playerColor : enemyColor);
        }
    }

    private void OnActionResolved(ActionResult result)
    {
        // Không xóa icon nữa - Turn Meter system tự cập nhật lại toàn bộ
        // Chỉ cập nhật highlight
        if (result.Actor != null)
        {
            OnUnitTurnStart(result.Actor);
        }
    }
}