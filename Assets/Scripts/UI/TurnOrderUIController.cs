using System.Collections.Generic;
using UnityEngine;

public class TurnOrderUIController : MonoBehaviour
{
    [Header("UI References")]
    public ActionSlotUI iconPrefab;
    public RectTransform iconContainer;

    [Header("Colors")]
    public Color playerColor = new Color(0.2f, 0.3f, 0.8f, 1f);
    public Color enemyColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    public Color currentTurnColor = Color.yellow;

    private List<ActionSlotUI> allIcons = new List<ActionSlotUI>();
    private CombatManager combatManager;

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
            combatManager.OnRoundSetup += RebuildTurnOrderUI;
            combatManager.OnUnitTurnStart += OnUnitTurnStart;
            combatManager.OnActionResolved += OnActionResolved; // Xóa icon khi hành động xong
        }
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnRoundSetup -= RebuildTurnOrderUI;
            combatManager.OnUnitTurnStart -= OnUnitTurnStart;
            combatManager.OnActionResolved -= OnActionResolved;
        }
    }

    public void RebuildTurnOrderUI(List<CombatUnit> turnOrder)
    {
        if (turnOrder == null) return;

        // Xóa icon cũ
        foreach (var icon in allIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        allIcons.Clear();

        // Tạo icon cho tất cả unit trong turn hiện tại
        foreach (var unit in turnOrder)
        {
            if (unit == null) continue;
            ActionSlotUI newIcon = Instantiate(iconPrefab, iconContainer);
            newIcon.SetupForTurnOrder(unit, unit.IsPlayer ? playerColor : enemyColor);
            allIcons.Add(newIcon);
        }
    }

    private void OnUnitTurnStart(CombatUnit currentUnit)
    {
        // Highlight icon của lượt hiện tại (không xóa)
        int currentIndex = allIcons.FindIndex(icon => icon.LinkedUnit == currentUnit);
        if (currentIndex == -1) return;

        for (int i = 0; i < allIcons.Count; i++)
        {
            var icon = allIcons[i];
            if (i == currentIndex)
                icon.SetBorderColor(currentTurnColor);
            else
                icon.SetBorderColor(icon.LinkedUnit.IsPlayer ? playerColor : enemyColor);
        }
    }

    private void OnActionResolved(ActionResult result)
    {
        // Xóa icon của unit vừa hành động khỏi danh sách (chờ 0.2s để animation kịp)
        var actor = result.Actor;
        var iconToRemove = allIcons.Find(icon => icon.LinkedUnit == actor);
        if (iconToRemove != null)
        {
            allIcons.Remove(iconToRemove);
            Destroy(iconToRemove.gameObject);
        }
        // Sau khi xóa, dồn các icon còn lại (nếu cần layout lại, tự động)
    }
}