using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI hiển thị thứ tự turn cho side-based system.
/// Hiển thị danh sách unit còn có thể hành động trong lượt hiện tại.
/// </summary>
public class TurnOrderUIController : MonoBehaviour
{
    [Header("UI References")]
    public ActionSlotUI iconPrefab;
    public RectTransform iconContainer;

    [Header("Colors")]
    public Color playerColor = new Color(0.2f, 0.3f, 0.8f, 1f);
    public Color enemyColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    public Color actedColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

    private List<TurnOrderIcon> allIcons = new List<TurnOrderIcon>();
    private CombatManager combatManager;

    private class TurnOrderIcon
    {
        public ActionSlotUI Icon { get; set; }
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
            combatManager.OnPlayerTurnStart += OnPlayerTurnStart;
            combatManager.OnPlayerTurnEnd += OnPlayerTurnEnd;
            combatManager.OnEnemyTurnStart += OnEnemyTurnStart;
            combatManager.OnActionResolved += OnActionResolved;
        }
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnPlayerTurnStart -= OnPlayerTurnStart;
            combatManager.OnPlayerTurnEnd -= OnPlayerTurnEnd;
            combatManager.OnEnemyTurnStart -= OnEnemyTurnStart;
            combatManager.OnActionResolved -= OnActionResolved;
        }
    }

    private void RebuildUI()
    {
        // Xóa icon cũ
        foreach (var entry in allIcons)
        {
            if (entry.Icon != null) Destroy(entry.Icon.gameObject);
        }
        allIcons.Clear();

        if (combatManager == null) return;

        // Hiển thị player units còn có thể act
        bool isPlayerTurn = combatManager.CurrentPhase == CombatPhase.PlayerTurn;
        var unitsToShow = isPlayerTurn
            ? combatManager.PlayerUnits.Where(u => u.IsAlive).ToList()
            : combatManager.EnemyUnits.Where(u => u.IsAlive).ToList();

        foreach (var unit in unitsToShow)
        {
            if (unit == null) continue;

            ActionSlotUI newIcon = Instantiate(iconPrefab, iconContainer);
            bool hasActed = unit.HasActedThisTurn;
            Color iconColor = hasActed ? actedColor : (unit.IsPlayer ? playerColor : enemyColor);
            newIcon.SetupForTurnOrder(unit, iconColor);
            newIcon.gameObject.SetActive(true);

            allIcons.Add(new TurnOrderIcon
            {
                Icon = newIcon,
                LinkedUnit = unit
            });
        }
    }

    private void OnPlayerTurnStart(List<CombatUnit> units)
    {
        RebuildUI();
    }

    private void OnPlayerTurnEnd()
    {
        // Chuẩn bị cho enemy turn
    }

    private void OnEnemyTurnStart()
    {
        RebuildUI();
    }

    private void OnActionResolved(ActionResult result)
    {
        // Cập nhật icon: đánh dấu unit đã act
        if (result.Actor != null)
        {
            var entry = allIcons.Find(e => e.LinkedUnit == result.Actor);
            if (entry != null && entry.Icon != null)
            {
                entry.Icon.SetBorderColor(actedColor);
            }
        }
    }
}