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

    [Header("Target Highlight")]
    public GameObject targetHighlightPrefab;
    
    [Header("Instruction Text")]
    public TextMeshProUGUI instructionText;

    private CombatManager combat;
    private Camera mainCam;
    private CanvasGroup planningCanvasGroup;

    private CombatUnit currentUnit;
    private SkillData selectedSkill;
    private bool isChoosingTarget;

    private List<GameObject> activeSkillButtons = new();
    private List<GameObject> targetHighlights = new();

    private void Start()
    {
        mainCam = Camera.main;
        combat = CombatManager.Instance;
        if (combat == null)
        {
            Debug.LogError("[PlanUI] Không tìm thấy CombatManager!");
            gameObject.SetActive(false);
            return;
        }

        // Subscribe to new turn-based events
        combat.OnPlayerTurnStart += OnPlayerTurn;
        combat.OnActionResolved += OnActionResolved;
        combat.OnVictory += HideUI;
        combat.OnDefeat += HideUI;
        combat.OnCombatStarted += OnCombatStarted;

        if (planningCanvas != null)
        {
            planningCanvas.gameObject.SetActive(true);
            planningCanvasGroup = planningCanvas.GetComponent<CanvasGroup>();
            if (planningCanvasGroup == null)
            {
                planningCanvasGroup = planningCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            // Start hidden
            planningCanvasGroup.alpha = 0;
            planningCanvasGroup.interactable = false;
        }

        if (leftSkillContainer != null) leftSkillContainer.gameObject.SetActive(false);
        if (rightSkillContainer != null) rightSkillContainer.gameObject.SetActive(false);

        SetInstruction("");
    }

    private void OnDestroy()
    {
        if (combat == null) return;
        combat.OnPlayerTurnStart -= OnPlayerTurn;
        combat.OnActionResolved -= OnActionResolved;
        combat.OnVictory -= HideUI;
        combat.OnDefeat -= HideUI;
        combat.OnCombatStarted -= OnCombatStarted;
    }

    private void Update()
    {
        if (planningCanvas == null || !planningCanvas.gameObject.activeSelf || !isChoosingTarget) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            HandleWorldClick(Input.mousePosition);
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelCurrentAction();
    }
    
    private void OnCombatStarted()
    {
        // Đã xóa hiển thị round text theo yêu cầu
        // if (roundText != null) roundText.text = $"Round {combat.CurrentRound}";
    }

    private void OnPlayerTurn(CombatUnit unit)
    {
        Debug.Log($"[PlanUI] OnPlayerTurn event received for {unit.UnitName}.");
        currentUnit = unit;
        isChoosingTarget = false;
        selectedSkill = null;

        var view = combat.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == unit);
        if (view == null)
        {
            Debug.LogError($"[PlanUI] Could not find UnitView for {unit.UnitName}. UI will not be shown.");
            return;
        }
        
        Debug.Log($"[PlanUI] Found UnitView for {unit.UnitName}. Opening skill wheel.");
        // Đã xóa hiển thị round text theo yêu cầu
        // if (roundText != null) roundText.text = $"Round {combat.CurrentRound}";

        ShowUI();
        OpenSkillWheel(unit, view);
    }
    
    private void OnActionResolved(ActionResult result)
    {
        if (currentUnit != null && result.Actor == currentUnit)
        {
            HideUI();
        }
    }

    private void ShowUI()
    {
        if (planningCanvasGroup != null)
        {
            planningCanvasGroup.alpha = 1;
            planningCanvasGroup.interactable = true;
        }
    }

    private void HideUI(ActionResult _ = null)
    {
        CloseSkillWheel();
        ClearTargetHighlights();
        if (planningCanvasGroup != null)
        {
            planningCanvasGroup.alpha = 0;
            planningCanvasGroup.interactable = false;
        }
        currentUnit = null;
        selectedSkill = null;
        isChoosingTarget = false;
        SetInstruction("");
    }
    
    private void HideUI()
    {
        HideUI(null);
    }

    private void HandleWorldClick(Vector3 mousePos)
    {
        Ray ray = mainCam.ScreenPointToRay(mousePos);
        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray);
        UnitView clickedView = null;

        // Thử 2D physics trước
        foreach (var hit in hits2D)
        {
            var view = hit.collider?.GetComponent<UnitView>();
            if (view != null && view.LinkedUnit.IsAlive)
            {
                clickedView = view;
                break;
            }
        }

        // Nếu không tìm thấy với 2D, thử 3D physics
        if (clickedView == null)
        {
            if (Physics.Raycast(ray, out RaycastHit hit3D, 100f))
            {
                clickedView = hit3D.collider?.GetComponent<UnitView>();
                if (clickedView != null && !clickedView.LinkedUnit.IsAlive)
                    clickedView = null;
            }
        }

        if (isChoosingTarget)
        {
            OnTargetSelected(clickedView);
        }
    }

    private void OpenSkillWheel(CombatUnit unit, UnitView view)
    {
        CloseSkillWheel();

        // Dùng AvailableSkills từ unit instance (đã được instantiate riêng) thay vì Data.skills gốc
        var skills = unit.AvailableSkills.Count > 0 ? unit.AvailableSkills.ToArray() : unit.Data.skills;
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

            StartCoroutine(AnimateSkillButton(rect, localIdx * 0.05f));

            bool canAfford = skill.apCost <= combat.CurrentPlayerAP;

            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"{skill.skillName}\n<size=70%>AP: {skill.apCost}</size>";
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                // Nếu không đủ AP, nút sẽ có màu khác
                img.color = canAfford ? skillNormalColor : skillCooldownColor;
            }

            int capturedIdx = i;
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                // Chỉ có thể nhấn nút nếu đủ AP
                btn.interactable = canAfford;
                btn.onClick.AddListener(() => OnSkillSelected(skill));
            }
            AddHoverEffect(go, unit, capturedIdx, skill);
            activeSkillButtons.Add(go);
        }
        SetInstruction($"{unit.UnitName} — Choose a skill");
    }

    private void CloseSkillWheel()
    {
        foreach (var go in activeSkillButtons) if (go != null) Destroy(go);
        activeSkillButtons.Clear();
        if (leftSkillContainer != null) leftSkillContainer.gameObject.SetActive(false);
        if (rightSkillContainer != null) rightSkillContainer.gameObject.SetActive(false);
    }

    private IEnumerator AnimateSkillButton(RectTransform buttonRect, float delay)
    {
        yield return new WaitForSeconds(delay);

        buttonRect.localScale = Vector3.zero;
        float duration = 0.25f;
        float elapsed = 0f;
        float overshoot = 1.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentScale = Mathf.Lerp(0, overshoot, t);
            buttonRect.localScale = new Vector3(currentScale, currentScale, 1f);
            yield return null;
        }

        duration = 0.15f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentScale = Mathf.Lerp(overshoot, 1f, t);
            buttonRect.localScale = new Vector3(currentScale, currentScale, 1f);
            yield return null;
        }

        buttonRect.localScale = Vector3.one;
    }

    private void OnSkillSelected(SkillData skill)
    {
        if (skill.targetType == TargetType.AllEnemies)
        {
            var targets = combat.EnemyUnits.Where(e => e.IsAlive).ToList();
            combat.SubmitPlayerTurnAction(skill, targets);
            return;
        }
        if (skill.targetType == TargetType.AllAllies)
        {
            var targets = combat.PlayerUnits.Where(p => p.IsAlive).ToList();
            combat.SubmitPlayerTurnAction(skill, targets);
            return;
        }

        selectedSkill = skill;
        isChoosingTarget = true;
        HighlightValidTargets(skill);
        SetInstruction($"Choose a target for [{skill.skillName}] (Right-click to cancel)");
        CloseSkillWheel();
    }

    private void OnTargetSelected(UnitView view)
    {
        if (!isChoosingTarget || view == null || selectedSkill == null) return;

        var targetUnit = view.LinkedUnit;
        if (!targetUnit.IsAlive) return;

        bool isEnemyTarget = selectedSkill.targetType == TargetType.SingleEnemy;
        bool isAllyTarget = selectedSkill.targetType == TargetType.SingleAlly;

        if ((isEnemyTarget && !targetUnit.IsPlayer) || (isAllyTarget && targetUnit.IsPlayer))
        {
            combat.SubmitPlayerTurnAction(selectedSkill, new List<CombatUnit> { targetUnit });
        }
    }

    private void CancelCurrentAction()
    {
        if (isChoosingTarget)
        {
            isChoosingTarget = false;
            selectedSkill = null;
            ClearTargetHighlights();
            var view = combat.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == currentUnit);
            if(view != null) OpenSkillWheel(currentUnit, view);
            else SetInstruction("Action canceled. Select a unit.");
        }
    }

    private void HighlightValidTargets(SkillData skill)
    {
        ClearTargetHighlights();
        if (targetHighlightPrefab == null) return;

        IEnumerable<CombatUnit> pool;
        switch (skill.targetType)
        {
            case TargetType.SingleEnemy:
                pool = combat.EnemyUnits.Where(e => e.IsAlive);
                break;
            case TargetType.SingleAlly:
                pool = combat.PlayerUnits.Where(p => p.IsAlive);
                break;
            default:
                return;
        }

        foreach (var unit in pool)
        {
            var view = combat.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == unit);
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

    private void SetInstruction(string text)
    {
        if (instructionText != null) instructionText.text = text;
    }

    private void AddHoverEffect(GameObject go, CombatUnit unit, int skillIdx, SkillData skill)
    {
        var trigger = go.AddComponent<EventTrigger>();
        bool canAfford = skill.apCost <= combat.CurrentPlayerAP;

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ =>
        {
            var img = go.GetComponent<Image>();
            if (img != null && canAfford) img.color = skillHoverColor;
        });
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ =>
        {
            var img = go.GetComponent<Image>();
            if (img != null && canAfford)
            {
                img.color = (selectedSkill == skill) ? skillSelectedColor : skillNormalColor;
            }
        });
        trigger.triggers.Add(exitEntry);
    }
}