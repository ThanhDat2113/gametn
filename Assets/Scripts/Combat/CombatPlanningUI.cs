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

    [Header("AP Display")]
    public TextMeshProUGUI apDisplay;

    private CombatManager combat;
    private Camera mainCam;
    private CanvasGroup planningCanvasGroup;

    private CombatUnit currentUnit;
    private List<Coroutine> skillButtonAnimations = new List<Coroutine>();
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
    UpdateAPDisplay();
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
        UpdateUnitEmphasis(); // Reset độ mờ khi UI ẩn
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
            go.name = $"SkillButton_{skill.skillName}"; // Đặt tên để dễ tìm
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -localIdx * skillRowSpacing);

            Coroutine anim = StartCoroutine(AnimateSkillButton(rect, localIdx * 0.05f));
            skillButtonAnimations.Add(anim);

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
                    btn.onClick.AddListener(() => {
                        AudioManager.Instance.PlayUIClick();
                        OnSkillSelected(skill);
                    });
                }
                AddHoverEffect(go, unit, capturedIdx, skill);
            activeSkillButtons.Add(go);
        }
        SetInstruction($"{unit.UnitName} — Choose a skill");
    }

    private void CloseSkillWheel()
    {
        // Dừng và xóa các animation cũ
        foreach (var anim in skillButtonAnimations)
        {
            if (anim != null) StopCoroutine(anim);
        }
        skillButtonAnimations.Clear();

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
        // Kiểm tra đủ AP trước tiên
        if (combat.CurrentPlayerAP < skill.apCost)
        {
            Debug.LogWarning($"[AP] Không đủ AP để dùng {skill.skillName}.");
            return;
        }

        // Xử lý các skill được đánh dấu là tự động xác nhận
        if (skill.autoConfirmOnSelect)
        {
            // Giả định mục tiêu là bản thân người dùng
            combat.SubmitPlayerTurnAction(skill, new List<CombatUnit> { currentUnit });
            UpdateAPDisplay();
        }
        else // Đối với các skill cần chọn mục tiêu
        {
            selectedSkill = skill;
            isChoosingTarget = true;
            HighlightValidTargets(skill);
            SetInstruction($"Choose a target for [{skill.skillName}] (Right-click to cancel)");
            CloseSkillWheel();
        }
    }

    private void OnTargetSelected(UnitView view)
    {
        if (!isChoosingTarget || view == null || selectedSkill == null) return;

        var clickedUnit = view.LinkedUnit;
        if (!clickedUnit.IsAlive) return;

        // Xác định xem mục tiêu được nhấp có hợp lệ để *xác nhận* hành động hay không
        bool isConfirmClickValid = false;
        switch (selectedSkill.targetType)
        {
            case TargetType.SingleEnemy:
            case TargetType.AllEnemies:
                isConfirmClickValid = !clickedUnit.IsPlayer;
                break;
            case TargetType.SingleAlly:
            case TargetType.AllAllies:
                isConfirmClickValid = clickedUnit.IsPlayer;
                break;
            case TargetType.Self:
                isConfirmClickValid = (clickedUnit == currentUnit);
                break;
        }

        if (isConfirmClickValid)
        {
            // Bây giờ, xác định danh sách mục tiêu *thực sự* cho kỹ năng
            List<CombatUnit> finalTargets = new List<CombatUnit>();
            switch (selectedSkill.targetType)
            {
                case TargetType.SingleEnemy:
                case TargetType.SingleAlly:
                case TargetType.Self:
                    finalTargets.Add(clickedUnit); // Chỉ mục tiêu đã nhấp
                    break;
                case TargetType.AllEnemies:
                    finalTargets = combat.EnemyUnits.Where(e => e.IsAlive).ToList(); // Tất cả kẻ địch
                    break;
                case TargetType.AllAllies:
                    finalTargets = combat.PlayerUnits.Where(p => p.IsAlive).ToList(); // Tất cả đồng minh
                    break;
            }
            
            if (finalTargets.Count > 0)
                {
                    combat.SubmitPlayerTurnAction(selectedSkill, finalTargets);
                    UpdateAPDisplay();
                    // Nếu skill không kết thúc lượt, UI sẽ được refresh bởi CombatManager,
                    // nên chúng ta không cần làm gì thêm ở đây.
                    // Nếu skill kết thúc lượt, CombatManager sẽ KHÔNG gửi lại OnPlayerTurnStart,
                    // và UI sẽ được ẩn đi bởi OnActionResolved.
                }
        }
    }

    private void CancelCurrentAction()
    {
        if (isChoosingTarget)
        {
            isChoosingTarget = false;
            selectedSkill = null;
            ClearTargetHighlights();
            UpdateUnitEmphasis(); // Reset độ mờ khi hủy
            var view = combat.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == currentUnit);
            if(view != null) OpenSkillWheel(currentUnit, view);
            else SetInstruction("Action canceled. Select a unit.");
        }
    }

    private void HighlightValidTargets(SkillData skill)
    {
        ClearTargetHighlights();

        // This call ensures that any previous emphasis is cleared before applying a new one.
        UpdateUnitEmphasis();

        if (targetHighlightPrefab == null) return;

        IEnumerable<CombatUnit> pool;
        switch (skill.targetType)
        {
            case TargetType.SingleEnemy:
            case TargetType.AllEnemies:
                pool = combat.EnemyUnits.Where(e => e.IsAlive);
                break;
            case TargetType.SingleAlly:
            case TargetType.AllAllies:
                pool = combat.PlayerUnits.Where(p => p.IsAlive);
                break;
            case TargetType.Self:
                pool = new List<CombatUnit> { currentUnit };
                break;
            default:
                pool = Enumerable.Empty<CombatUnit>();
                break;
        }

        foreach (var unit in pool)
        {
            var view = combat.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == unit);
            if (view == null) continue;
            // Parent the highlight to the view's transform so it moves with the unit.
            var go = Instantiate(targetHighlightPrefab, view.transform);
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

    private void UpdateUnitEmphasis()
    {
        var allViews = combat.GetAllUnitViews();
        float unfocusedAlpha = 0.5f;

        if (!isChoosingTarget || selectedSkill == null)
        {
            foreach (var view in allViews)
            {
                view.SetAlpha(1f);
            }
            return;
        }

        bool isTargetingEnemies = (selectedSkill.targetType == TargetType.SingleEnemy || selectedSkill.targetType == TargetType.AllEnemies);

        foreach (var view in allViews)
        {
            bool isTargetGroup = (isTargetingEnemies && !view.LinkedUnit.IsPlayer) || (!isTargetingEnemies && view.LinkedUnit.IsPlayer);
            bool isCaster = (view.LinkedUnit == currentUnit);

            if (isTargetGroup || isCaster)
            {
                view.SetAlpha(1f); // Rõ nét
            }
            else
            {
                view.SetAlpha(unfocusedAlpha); // Làm mờ
            }
        }
    }

    private void UpdateAPDisplay()
    {
        if (apDisplay != null)
        {
            apDisplay.text = $"AP: {combat.CurrentPlayerAP}";
        }
    }
}