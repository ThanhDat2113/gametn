using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CombatPlanningUI : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas planningCanvas;

    [Header("Round Display")]
    public TextMeshProUGUI roundText;

    [Header("Skill Wheel")]
    public GameObject skillButtonPrefab;
    public GameObject skillButtonUltiPrefab;
    public RectTransform leftSkillContainer;
    public RectTransform rightSkillContainer;
    public float skillColumnOffset = 160f;
    public float skillRowSpacing = 70f;

    [Header("Skill Wheel Visual")]
    public Color skillNormalColor = new Color(0.1f, 0.1f, 0.15f, 0.92f);
    public Color skillHoverColor = new Color(0.85f, 0.2f, 0.1f, 1f);
    public Color skillCooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    public Color skillSelectedColor = new Color(1f, 0.7f, 0f, 1f);

    [Header("Action Bar")]
    public RectTransform actionBarPanel;
    public GameObject actionSlotPrefab;
    public RectTransform actionSlotContainer;
    public Button confirmButton;
    public TextMeshProUGUI instructionText;

    [Header("Action Bar Layout")]
    public float slotWidth = 100f;
    public float slotSpacing = 20f;

    [Header("Target Highlight")]
    public GameObject targetHighlightPrefab;

    private CombatManager combat;
    private Camera mainCam;
    private CanvasGroup planningCanvasGroup;
    private List<CombatUnit> planningUnits = new();
    private CombatUnit activeUnit;
    private CombatUnit pendingUnit;
    private SkillData pendingSkill;
    private bool isChoosingTarget;
    private Dictionary<CombatUnit, (SkillData skill, List<CombatUnit> targets)> choices = new();
    private List<CombatUnit> actionOrder = new();
    private List<GameObject> activeSkillButtons = new();
    private List<GameObject> targetHighlights = new();
    private List<ActionSlotUI> actionSlots = new();
    private int dragFromIndex = -1;
    private bool isDragging;
    private int hoveredSlotIndex = -1;

    private void Start()
    {
        mainCam = Camera.main;
        combat = CombatManager.Instance;
        if (combat == null)
        {
            Debug.LogError("[PlanUI] Không tìm thấy CombatManager!");
            return;
        }

        combat.OnPlayerPlanStarted += OnPlanStarted;
        combat.OnExecuteStarted += OnExecuteStarted;
        combat.OnVictory += HideUI;
        combat.OnDefeat += HideUI;
        combat.OnCombatStarted += OnCombatStarted;
        combat.OnPlanChanged += RebuildActionBar;

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
        else
            Debug.LogError("[PlanUI] confirmButton chưa gán!");

        if (planningCanvas != null) 
        {
            planningCanvas.gameObject.SetActive(true);
            planningCanvasGroup = planningCanvas.GetComponent<CanvasGroup>();
            if (planningCanvasGroup == null)
            {
                planningCanvasGroup = planningCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }
        if (actionBarPanel != null) actionBarPanel.gameObject.SetActive(true);
        if (leftSkillContainer != null) leftSkillContainer.gameObject.SetActive(false);
        if (rightSkillContainer != null) rightSkillContainer.gameObject.SetActive(false);

        SetInstruction("");

        StartCoroutine(EnsureActionBarInit());
    }

    private IEnumerator EnsureActionBarInit()
    {
        yield return new WaitForSeconds(0.2f);
        if (actionSlots.Count == 0 && combat.PlayerUnits != null && combat.PlayerUnits.Count > 0)
        {
            var alive = combat.PlayerUnits.Where(u => u.IsAlive).ToList();
            if (alive.Count > 0)
            {
                planningUnits = alive;
                actionOrder = new List<CombatUnit>(alive);
                RebuildActionBar();
                SetInstruction("Đang chờ lượt...");
                if (roundText != null) roundText.text = $"Round {combat.CurrentRound}";
            }
        }
    }

    private void OnDestroy()
    {
        if (combat == null) return;
        combat.OnPlayerPlanStarted -= OnPlanStarted;
        combat.OnExecuteStarted -= OnExecuteStarted;
        combat.OnVictory -= HideUI;
        combat.OnDefeat -= HideUI;
        combat.OnCombatStarted -= OnCombatStarted;
        combat.OnPlanChanged -= RebuildActionBar;
    }

    private void Update()
    {
        if (planningCanvas == null || !planningCanvas.gameObject.activeSelf) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            HandleWorldClick(Input.mousePosition);
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelCurrentAction();
    }

    private void OnCombatStarted()
    {
        Debug.Log("[PlanUI] OnCombatStarted called");
        var playerUnits = combat.PlayerUnits.Where(u => u.IsAlive).ToList();
        if (playerUnits.Count > 0 && actionSlots.Count == 0)
        {
            planningUnits = playerUnits;
            actionOrder = new List<CombatUnit>(playerUnits);
            RebuildActionBar();
            SetInstruction("Đang chờ lượt của bạn...");
            if (roundText != null) roundText.text = $"Round {combat.CurrentRound}";
        }
    }

    private void OnPlanStarted(List<CombatUnit> units)
    {
        Debug.Log($"[PlanUI] OnPlanStarted: {units.Count} units");
        if (planningCanvas != null) planningCanvas.gameObject.SetActive(true);

        if (roundText != null)
            roundText.text = $"Round {combat.CurrentRound}";

        planningUnits = units.Where(u => u.IsAlive).ToList();
        choices.Clear();
        actionOrder = new List<CombatUnit>(planningUnits);
        activeUnit = null;
        pendingUnit = null;
        pendingSkill = null;
        isChoosingTarget = false;

        foreach (var slot in actionSlots)
            if (slot != null) Destroy(slot.gameObject);
        actionSlots.Clear();

        RebuildActionBar();
        if (confirmButton != null) confirmButton.interactable = true;
        SetInstruction("Nhấn vào nhân vật để chọn skill hoặc nhấn Confirm để tự động.");
    }

    private void OnExecuteStarted()
    {
        if (planningCanvas != null) planningCanvas.gameObject.SetActive(false);
        if (confirmButton != null) confirmButton.interactable = false;
    }

    private void HideUI()
    {
        if (planningCanvas != null) planningCanvas.gameObject.SetActive(false);
        if (confirmButton != null) confirmButton.interactable = false;
    }

    private void HandleWorldClick(Vector3 mousePos)
    {
        Ray ray = mainCam.ScreenPointToRay(mousePos);
        var hits = Physics2D.GetRayIntersectionAll(ray);
        UnitView clickedView = null;
        foreach (var hit in hits)
        {
            var view = hit.collider?.GetComponent<UnitView>();
            if (view != null && view.LinkedUnit.IsAlive)
            {
                clickedView = view;
                break;
            }
        }
        if (isChoosingTarget)
            HandleTargetClick(clickedView);
        else
            HandleUnitClick(clickedView);
    }

    private void HandleUnitClick(UnitView view)
    {
        if (view == null)
        {
            CloseSkillWheel();
            return;
        }
        var unit = view.LinkedUnit;
        if (!unit.IsPlayer || !unit.IsAlive) return;

        if (activeUnit != null && activeUnit != unit)
            CloseSkillWheel();

        OpenSkillWheel(unit, view);
    }

    private void OpenSkillWheel(CombatUnit unit, UnitView view)
    {
        CloseSkillWheel();
        activeUnit = unit;

        var skills = unit.Data.skills;
        Vector2 screenPos = mainCam.WorldToScreenPoint(view.transform.position);
        RectTransform canvasRect = planningCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, planningCanvas.worldCamera, out Vector2 canvasPos);

        if (leftSkillContainer != null)
        {
            leftSkillContainer.gameObject.SetActive(true);
            leftSkillContainer.anchoredPosition = new Vector2(canvasPos.x - skillColumnOffset, canvasPos.y);
        }
        if (rightSkillContainer != null)
        {
            rightSkillContainer.gameObject.SetActive(true);
            rightSkillContainer.anchoredPosition = new Vector2(canvasPos.x + skillColumnOffset, canvasPos.y);
        }

        for (int i = 0; i < skills.Length; i++)
        {
            var skill = skills[i];
            bool isLeft = i < 3;
            var container = isLeft ? leftSkillContainer : rightSkillContainer;
            if (container == null) continue;
            int localIdx = isLeft ? i : (i - 3);
            bool isUlti = (i >= 5);
            var prefab = (isUlti && skillButtonUltiPrefab != null) ? skillButtonUltiPrefab : skillButtonPrefab;
            if (prefab == null) continue;

            var go = Instantiate(prefab, container);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -localIdx * skillRowSpacing);

            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                bool cd = !unit.IsSkillReady(i);
                label.text = cd ? $"{skill.skillName}\\n<size=70%>CD: {unit.SkillCooldowns[i]}</size>" : $"{skill.skillName}\\n<size=70%>{skill.basePoint}pt</size>";
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                bool onCD = !unit.IsSkillReady(i);
                bool isChosen = choices.TryGetValue(unit, out var ch) && ch.skill == skill;
                img.color = onCD ? skillCooldownColor : (isChosen ? skillSelectedColor : skillNormalColor);
            }

            int capturedIdx = i;
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = unit.IsSkillReady(i);
                btn.onClick.AddListener(() => OnSkillButtonClicked(unit, skill, capturedIdx));
            }
            AddHoverEffect(go, unit, i);
            activeSkillButtons.Add(go);
        }
        SetInstruction($"{unit.UnitName} — chọn skill");
    }

    private void CloseSkillWheel()
    {
        foreach (var go in activeSkillButtons) if (go != null) Destroy(go);
        activeSkillButtons.Clear();
        if (leftSkillContainer != null) leftSkillContainer.gameObject.SetActive(false);
        if (rightSkillContainer != null) rightSkillContainer.gameObject.SetActive(false);
        activeUnit = null;
        if (!isChoosingTarget)
            SetInstruction("Nhấn vào nhân vật để chọn skill");
    }

    private void OnSkillButtonClicked(CombatUnit unit, SkillData skill, int skillIndex)
    {
        if (choices.TryGetValue(unit, out var existing) && existing.skill == skill)
        {
            combat.ClearPlayerSkillSelection(unit);
            choices.Remove(unit);
            RebuildActionBar();
            SetInstruction($"{unit.UnitName} đã hủy {skill.skillName}.");
            CloseSkillWheel();
            Debug.Log($"[PlanUI] {unit.UnitName} hủy {skill.skillName}");
            return;
        }

        if (skill.targetType == TargetType.AllEnemies)
        {
            var targets = combat.EnemyUnits.Where(e => e.IsAlive).ToList();
            ConfirmSkillChoice(unit, skill, targets);
            CloseSkillWheel();
            return;
        }
        if (skill.targetType == TargetType.AllAllies)
        {
            var targets = combat.PlayerUnits.Where(p => p.IsAlive).ToList();
            ConfirmSkillChoice(unit, skill, targets);
            CloseSkillWheel();
            return;
        }

        pendingUnit = unit;
        pendingSkill = skill;
        isChoosingTarget = true;
        HighlightValidTargets(skill);
        SetInstruction($"Chọn mục tiêu cho [{skill.skillName}] (Chuột phải hủy)");
        CloseSkillWheel();
    }

    private void HandleTargetClick(UnitView view)
    {
        if (!isChoosingTarget || view == null) return;
        var target = view.LinkedUnit;
        if (!target.IsAlive) return;
        bool wantEnemy = pendingSkill.targetType == TargetType.SingleEnemy;
        bool wantAlly = pendingSkill.targetType == TargetType.SingleAlly;
        if ((wantEnemy && !target.IsPlayer) || (wantAlly && target.IsPlayer))
            FinishTargetSelection(new List<CombatUnit> { target });
    }

    private void FinishTargetSelection(List<CombatUnit> targets)
    {
        isChoosingTarget = false;
        ClearTargetHighlights();
        if (pendingUnit != null && pendingSkill != null)
            ConfirmSkillChoice(pendingUnit, pendingSkill, targets);
        pendingUnit = null;
        pendingSkill = null;
        SetInstruction("Nhấn vào nhân vật để chọn skill");
    }

    private void CancelCurrentAction()
    {
        if (isChoosingTarget)
        {
            isChoosingTarget = false;
            pendingUnit = null;
            pendingSkill = null;
            ClearTargetHighlights();
            SetInstruction("Nhấn vào nhân vật để chọn skill");
        }
        else
        {
            CloseSkillWheel();
        }
    }

    private void ConfirmSkillChoice(CombatUnit unit, SkillData skill, List<CombatUnit> targets)
    {
        if (unit == null) return;
        Debug.Log($"[DebugUI] Calling SetPlayerSkillSelection for {unit.UnitName}");
        combat.SetPlayerSkillSelection(unit, skill, targets);

        choices[unit] = (skill, targets);
        if (!actionOrder.Contains(unit)) actionOrder.Add(unit);
        RebuildActionBar();
        Debug.Log($"[PlanUI] {unit.UnitName} → {skill.skillName} → {string.Join(",", targets.Select(t => t.UnitName))}");
    }

    private void HighlightValidTargets(SkillData skill)
    {
        ClearTargetHighlights();
        if (targetHighlightPrefab == null) return;
        var pool = skill.targetType == TargetType.SingleEnemy ? combat.EnemyUnits.Where(e => e.IsAlive) : combat.PlayerUnits.Where(p => p.IsAlive);
        foreach (var unit in pool)
        {
            var view = combat.GetUnitView(unit);
            if (view == null) continue;
            var go = Instantiate(targetHighlightPrefab, view.transform.position, Quaternion.identity);
            targetHighlights.Add(go);
        }
    }

    private void ClearTargetHighlights()
    {
        foreach (var go in targetHighlights) if (go != null) Destroy(go);
        targetHighlights.Clear();
    }

    private void RebuildActionBar()
    {
        var aliveUnits = combat.ActionOrder.Where(u => u.IsAlive).ToList();
        Debug.Log($"[PlanUI] RebuildActionBar: {aliveUnits.Count} slots from CombatManager's ActionOrder");

        foreach (var slot in actionSlots)
            if (slot != null) Destroy(slot.gameObject);
        actionSlots.Clear();

        for (int i = 0; i < aliveUnits.Count; i++)
        {
            var unit = aliveUnits[i];
            SkillData skill = null;
            List<CombatUnit> targets = null;
            if (choices.TryGetValue(unit, out var ch))
            {
                skill = ch.skill;
                targets = ch.targets;
            }
            var go = Instantiate(actionSlotPrefab, actionSlotContainer);
            var slot = go.GetComponent<ActionSlotUI>();
            if (slot == null) slot = go.AddComponent<ActionSlotUI>();
            slot.Setup(unit, skill, targets, i, this);
            actionSlots.Add(slot);
        }
        AlignActionSlots();
    }

    private void AlignActionSlots()
    {
        if (actionSlotContainer == null) return;
        int count = actionSlots.Count;
        if (count == 0) return;

        float totalWidth = count * slotWidth + (count - 1) * slotSpacing;
        float startX = -totalWidth / 2f;
        for (int i = 0; i < count; i++)
        {
            float x = startX + i * (slotWidth + slotSpacing) + slotWidth / 2f;
            var rect = actionSlots[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, 0);
        }
    }

    private void UpdateConfirmButton()
    {
        bool allChosen = planningUnits.All(u => !u.IsAlive || choices.ContainsKey(u));
        confirmButton.interactable = allChosen;
        var txt = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
            txt.text = allChosen ? "✓ CONFIRM" : $"Còn {planningUnits.Count(u => u.IsAlive && !choices.ContainsKey(u))} chưa chọn";
    }

    public void OnSlotDragStart(int index)
    {
        dragFromIndex = index;
        isDragging = true;
        hoveredSlotIndex = -1;
    }

    public void OnSlotDragging(Vector2 screenPos)
    {
        if (!isDragging) return;
        int nearest = FindNearestSlot(screenPos);
        if (nearest != hoveredSlotIndex)
        {
            foreach (var s in actionSlots) s.HideIndicator();
            if (nearest >= 0 && nearest != dragFromIndex) actionSlots[nearest].ShowIndicator();
            hoveredSlotIndex = nearest;
        }
    }

    public void OnSlotHovered(int index)
    {
        if (!isDragging) return;
        hoveredSlotIndex = index;
    }

    public void OnSlotDragEnd()
    {
        if (!isDragging) return;
        isDragging = false;
        foreach (var s in actionSlots) s.HideIndicator();
        int swapWith = hoveredSlotIndex;
        hoveredSlotIndex = -1;
        if (swapWith < 0 || swapWith == dragFromIndex) { dragFromIndex = -1; return; }
        
        var alive = combat.ActionOrder.Where(u => u.IsAlive).ToList();
        if (dragFromIndex >= alive.Count || swapWith >= alive.Count) { dragFromIndex = -1; return; }
        
        var newOrder = new List<CombatUnit>(alive);
        var tmp = newOrder[dragFromIndex];
        newOrder[dragFromIndex] = newOrder[swapWith];
        newOrder[swapWith] = tmp;

        combat.UpdateActionOrder(newOrder);
        
        dragFromIndex = -1;
        Debug.Log("[PlanUI] Sent new action order to CombatManager.");
    }

    private int FindNearestSlot(Vector2 screenPos)
    {
        if (actionSlots.Count == 0) return -1;
        float minDist = float.MaxValue;
        int nearest = -1;
        for (int i = 0; i < actionSlots.Count; i++)
        {
            Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(null, actionSlots[i].GetComponent<RectTransform>().position);
            float dist = Mathf.Abs(screenPos.x - slotScreen.x);
            if (dist < minDist) { minDist = dist; nearest = i; }
        }
        return nearest;
    }

    private void OnConfirm()
    {
        combat.AutoCompleteAndConfirm();
    }

    private void SetInstruction(string text)
    {
        if (instructionText != null) instructionText.text = text;
    }

    private void AddHoverEffect(GameObject go, CombatUnit unit, int skillIdx)
    {
        var trigger = go.AddComponent<EventTrigger>();
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ =>
        {
            var img = go.GetComponent<Image>();
            if (img != null && unit.IsSkillReady(skillIdx)) img.color = skillHoverColor;
        });
        trigger.triggers.Add(enterEntry);
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ =>
        {
            var img = go.GetComponent<Image>();
            if (img != null && unit.IsSkillReady(skillIdx))
            {
                bool isChosen = choices.TryGetValue(unit, out var ch) && ch.skill == unit.Data.skills[skillIdx];
                img.color = isChosen ? skillSelectedColor : skillNormalColor;
            }
        });
        trigger.triggers.Add(exitEntry);
    }
}