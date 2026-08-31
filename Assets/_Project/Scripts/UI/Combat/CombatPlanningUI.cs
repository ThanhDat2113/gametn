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
    public float skillWheelOffsetY = 0f;

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

    [Header("End Turn Button")]
    public Button endTurnButton;

    private CombatManager combat;
    private CanvasGroup planningCanvasGroup;
    private Camera MainCam 
    { 
        get 
        {
            if (CombatManager.Instance?.cameraManager != null)
            {
                var cm = CombatManager.Instance.cameraManager;
                return cm.GetComponent<Camera>();
            }
            return Camera.main;
        }
    }

    private CombatUnit currentUnit;
    private List<Coroutine> skillButtonAnimations = new List<Coroutine>();
    private SkillData selectedSkill;
    private bool isChoosingTarget;
    private bool isSelectingUnit; // true = đang chọn unit, false = đang chọn skill/target

    private List<GameObject> activeSkillButtons = new();
    private List<GameObject> targetHighlights = new();
    private UnitView _hoveredView; // unit đang được rê chuột (hover viền + hiện nội tại)

    private void Start()
    {
        combat = CombatManager.Instance;
        if (combat == null)
        {
            Debug.LogError("[PlanUI] Không tìm thấy CombatManager!");
            gameObject.SetActive(false);
            return;
        }

        combat.OnPlayerTurnStart += OnPlayerTurn;
        combat.OnActionResolved += OnActionResolved;
        combat.OnPlayerTurnEnd += HideUI;
        combat.OnVictory += (_) => HideUI();
        combat.OnDefeat += HideUI;
        combat.OnCombatStarted += OnCombatStarted;

        if (planningCanvas != null)
        {
            planningCanvas.gameObject.SetActive(true);
            planningCanvasGroup = planningCanvas.GetComponent<CanvasGroup>();
            if (planningCanvasGroup == null)
                planningCanvasGroup = planningCanvas.gameObject.AddComponent<CanvasGroup>();
            planningCanvasGroup.alpha = 0;
            planningCanvasGroup.interactable = false;
        }

        if (leftSkillContainer != null) leftSkillContainer.gameObject.SetActive(false);
        if (rightSkillContainer != null) rightSkillContainer.gameObject.SetActive(false);
        SetInstruction("");

        // End Turn button
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
            endTurnButton.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (combat == null) return;
        combat.OnPlayerTurnStart -= OnPlayerTurn;
        combat.OnActionResolved -= OnActionResolved;
        combat.OnPlayerTurnEnd -= HideUI;
        combat.OnVictory -= (_) => HideUI();
        combat.OnDefeat -= HideUI;
        combat.OnCombatStarted -= OnCombatStarted;
    }

    private void Update()
    {
        if (planningCanvas == null || !planningCanvas.gameObject.activeSelf) return;
        if (combat.CurrentPhase != CombatPhase.PlayerTurn || !combat.IsWaitingForPlayerSelection)
        {
            // Ngoài lượt chọn (action đang chạy / lượt địch) → tắt hover, không inspect
            ClearHoverHighlight();
            return;
        }

        // ── Hover: viền sáng + hiển thị nội tại của unit đang rê chuột ──
        UpdateHoverHighlight();

        if (isSelectingUnit && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            HandleUnitSelectionClick(Input.mousePosition);

        if (isChoosingTarget && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            HandleWorldClick(Input.mousePosition);

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelCurrentAction();
    }
    
    private void OnCombatStarted() { }

    // ── NHẬN DANH SÁCH UNIT CÓ THỂ ACT ──────────────────────
    private void OnPlayerTurn(List<CombatUnit> unitsCanAct)
    {
        Debug.Log($"[PlanUI] Player turn bắt đầu. {unitsCanAct.Count} unit có thể hành động.");

        // Reset trạng thái
        currentUnit = null;
        selectedSkill = null;
        isChoosingTarget = false;
        isSelectingUnit = true; // Bắt đầu chế độ chọn unit

        ShowUI();
        SetInstruction("Chọn 1 nhân vật sáng để ra lệnh hành động");
        UpdateAPDisplay();
        UpdateUnitEmphasis(unitsCanAct);
    }

    // ── XỬ LÝ CLICK CHỌN UNIT TRÊN WORLD ────────────────────
    private void HandleUnitSelectionClick(Vector3 mousePos)
    {
        Ray ray = MainCam.ScreenPointToRay(mousePos);
        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray);
        UnitView clickedView = null;
        foreach (var hit in hits2D)
        {
            var view = hit.collider?.GetComponent<UnitView>();
            if (view != null && view.LinkedUnit.IsPlayer && view.LinkedUnit.CanActThisTurn)
            { clickedView = view; break; }
        }
        if (clickedView == null)
        {
            if (Physics.Raycast(ray, out RaycastHit hit3D, 100f))
            {
                clickedView = hit3D.collider?.GetComponent<UnitView>();
                if (clickedView != null && (!clickedView.LinkedUnit.IsPlayer || !clickedView.LinkedUnit.CanActThisTurn))
                    clickedView = null;
            }
        }

        if (clickedView != null)
        {
            AudioManager.Instance?.PlayUISelect();
            SelectUnit(clickedView.LinkedUnit);
        }
    }

    private void SelectUnit(CombatUnit unit)
    {
        currentUnit = unit;
        isSelectingUnit = false;
        isChoosingTarget = false;
        var view = combat.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == unit);
        if (view == null)
        {
            Debug.LogError($"[PlanUI] Could not find UnitView for {unit.UnitName}.");
            return;
        }
        OpenSkillWheel(unit, view);
        UpdateAPDisplay();
    }

    private void OnEndTurnClicked()
    {
        Debug.Log("[PlanUI] Player clicked End Turn.");
        HideUI();
        combat.EndPlayerTurn();
    }
    
    private void OnActionResolved(ActionResult result)
    {
        ClearHoverHighlight();

        // ── Thông tin kỹ năng KẺ ĐỊCH đang sử dụng ──
        // Chỉ hiển thị khi ngoài lượt player (enemy turn / Madara opening blitz)
        // để không đè lên text đang hướng dẫn người chơi giữa chừng.
        if (result != null && result.Actor != null && result.Skill != null
            && !result.Actor.IsPlayer && combat.CurrentPhase != CombatPhase.PlayerTurn)
        {
            ShowEnemySkillInfo(result);
        }

        // KHÔNG reset UI ở đây — OnActionResolved chạy TRƯỚC khi CombatManager cấp
        // extra action (ExtraTurnEffect deferred grant nằm sau `yield return ResolveAction`).
        // Nếu reset/update emphasis tại đây sẽ dùng trạng thái cũ (target chưa được cấp lượt)
        // → target bị mờ và không click được. Thay vào đó, vòng lặp DoPlayerTurn sẽ gọi
        // OnPlayerTurnStart(unitsCanAct) NGAY SAU khi grant xong → UI refresh đúng trạng thái mới.
    }

    /// <summary>
    /// Hiển thị thông tin kỹ năng mà kẻ địch đang dùng, tái sử dụng đúng định dạng
    /// như bảng mô tả khi hover nút kỹ năng của player (tên + mục tiêu + mô tả).
    /// Canvas chuyển sang chế độ xem-only: alpha = 1 nhưng interactable = false,
    /// nút End Turn giữ nguyên trạng thái ẩn (đang ngoài lượt player).
    /// Text tự được thay thế khi lượt player bắt đầu (OnPlayerTurn → SetInstruction)
    /// hoặc bị ẩn khi trận đấu kết thúc (HideUI do OnVictory/OnDefeat gọi).
    /// </summary>
    private void ShowEnemySkillInfo(ActionResult result)
    {
        if (planningCanvasGroup != null)
        {
            planningCanvasGroup.alpha = 1;
            planningCanvasGroup.interactable = false;
        }

        string targetHint = GetTargetTypeHint(result.Skill.targetType);
        string desc = string.IsNullOrEmpty(result.Skill.description) ? "Chưa có mô tả." : result.Skill.description;

        string targetsLine = "";
        if (result.InitialTargets != null && result.InitialTargets.Count > 0)
        {
            string names = string.Join(", ", result.InitialTargets
                .Where(t => t != null)
                .Select(t => t.UnitName));
            if (!string.IsNullOrEmpty(names))
                targetsLine = $"\n<size=60%>Vào: {names}</size>";
        }

        SetInstruction($"<color=#FF6B6B>{result.Actor.UnitName}</color> dùng [{result.Skill.skillName}]\n{targetHint}\n{desc}{targetsLine}");
    }

    private void ShowUI()
    {
        if (planningCanvasGroup != null)
        {
            planningCanvasGroup.alpha = 1;
            planningCanvasGroup.interactable = true;
        }
        if (endTurnButton != null) endTurnButton.gameObject.SetActive(true);
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
        if (endTurnButton != null) endTurnButton.gameObject.SetActive(false);
        currentUnit = null;
        selectedSkill = null;
        isChoosingTarget = false;
        isSelectingUnit = false;
        ClearHoverHighlight();
        SetInstruction("");
        UpdateUnitEmphasis(null);
    }
    
    private void HideUI() => HideUI(null);

    private void HandleWorldClick(Vector3 mousePos)
    {
        Ray ray = MainCam.ScreenPointToRay(mousePos);
        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray);
        UnitView clickedView = null;
        foreach (var hit in hits2D)
        {
            var view = hit.collider?.GetComponent<UnitView>();
            if (view != null && view.LinkedUnit.IsAlive) { clickedView = view; break; }
        }
        if (clickedView == null)
        {
            if (Physics.Raycast(ray, out RaycastHit hit3D, 100f))
            {
                clickedView = hit3D.collider?.GetComponent<UnitView>();
                if (clickedView != null && !clickedView.LinkedUnit.IsAlive) clickedView = null;
            }
        }
        if (isChoosingTarget) OnTargetSelected(clickedView);
    }

    private void OpenSkillWheel(CombatUnit unit, UnitView view)
    {
        CloseSkillWheel();
        var skills = unit.AvailableSkills.Count > 0 ? unit.AvailableSkills.ToArray() : unit.Data.skills;
        Vector2 screenPos = MainCam.WorldToScreenPoint(view.transform.position);
        RectTransform canvasRect = planningCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, planningCanvas.worldCamera, out Vector2 canvasPos);

        if (leftSkillContainer != null)
        {
            leftSkillContainer.gameObject.SetActive(true);
            leftSkillContainer.anchoredPosition = new Vector2(canvasPos.x - skillColumnOffset, canvasPos.y + skillWheelOffsetY);
        }
        if (rightSkillContainer != null)
        {
            rightSkillContainer.gameObject.SetActive(true);
            rightSkillContainer.anchoredPosition = new Vector2(canvasPos.x + skillColumnOffset, canvasPos.y + skillWheelOffsetY);
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
            go.name = $"SkillButton_{skill.skillName}";
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -localIdx * skillRowSpacing);

            Coroutine anim = StartCoroutine(AnimateSkillButton(rect, localIdx * 0.05f));
            skillButtonAnimations.Add(anim);

            bool canAfford = skill.apCost <= combat.CurrentPlayerAP;
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = $"{skill.skillName}\n<size=70%>AP: {skill.apCost}</size>";
            var img = go.GetComponent<Image>();
            if (img != null) img.color = canAfford ? skillNormalColor : skillCooldownColor;

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = canAfford;
                btn.onClick.AddListener(() => OnSkillSelected(skill));
            }
            AddHoverEffect(go, unit, i, skill);
            activeSkillButtons.Add(go);
        }
        SetInstruction($"{unit.UnitName} — Chọn kỹ năng (di chuột vào để xem mô tả)");
    }

    private void CloseSkillWheel()
    {
        foreach (var anim in skillButtonAnimations) if (anim != null) StopCoroutine(anim);
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
        float duration = 0.25f, elapsed = 0f, overshoot = 1.25f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            buttonRect.localScale = new Vector3(Mathf.Lerp(0, overshoot, t), Mathf.Lerp(0, overshoot, t), 1f);
            yield return null;
        }
        duration = 0.15f; elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(overshoot, 1f, t);
            buttonRect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        buttonRect.localScale = Vector3.one;
    }

    private void OnSkillSelected(SkillData skill)
    {
        if (combat.CurrentPlayerAP < skill.apCost)
        {
            Debug.LogWarning($"[AP] Không đủ AP để dùng {skill.skillName}.");
            return;
        }
        AudioManager.Instance?.PlayUISelect();
        if (skill.autoConfirmOnSelect)
        {
            combat.SubmitPlayerAction(currentUnit, skill, new List<CombatUnit> { currentUnit });
            UpdateAPDisplay();
        }
        else
        {
            selectedSkill = skill;
            isChoosingTarget = true;
            HighlightValidTargets(skill);
            SetInstruction($"Chọn mục tiêu cho [{skill.skillName}] (Chuột phải để hủy)");
            CloseSkillWheel();
        }
    }

    private void OnTargetSelected(UnitView view)
    {
        if (!isChoosingTarget || view == null || selectedSkill == null) return;
        var clickedUnit = view.LinkedUnit;
        if (!clickedUnit.IsAlive || !clickedUnit.IsTargetable) return;
        bool isValid = false;
        switch (selectedSkill.targetType)
        {
            case TargetType.SingleEnemy: case TargetType.AllEnemies: isValid = !clickedUnit.IsPlayer; break;
            case TargetType.SingleAlly: case TargetType.AllAllies: isValid = clickedUnit.IsPlayer; break;
            case TargetType.Self: isValid = (clickedUnit == currentUnit); break;
        }
        if (isValid)
        {
            List<CombatUnit> finalTargets = new List<CombatUnit>();
            switch (selectedSkill.targetType)
            {
                case TargetType.SingleEnemy: case TargetType.SingleAlly: case TargetType.Self:
                    finalTargets.Add(clickedUnit); break;
                case TargetType.AllEnemies: finalTargets = combat.EnemyUnits.Where(e => e.IsAlive && e.IsTargetable).ToList(); break;
                case TargetType.AllAllies: finalTargets = combat.PlayerUnits.Where(p => p.IsAlive).ToList(); break;
            }
            if (finalTargets.Count > 0)
            {
                AudioManager.Instance?.PlayUITargetConfirm();
                combat.SubmitPlayerAction(currentUnit, selectedSkill, finalTargets);
                UpdateAPDisplay();
            }
        }
    }

    private void CancelCurrentAction()
    {
        if (isChoosingTarget)
        {
            // Đang chọn target → quay lại skill wheel
            AudioManager.Instance?.PlayUICancel();
            isChoosingTarget = false;
            selectedSkill = null;
            ClearTargetHighlights();
            var view = combat.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == currentUnit);
            if (view != null) OpenSkillWheel(currentUnit, view);
            else SetInstruction("Đã hủy. Chọn 1 nhân vật sáng để ra lệnh hành động.");
            return;
        }

        if (!isSelectingUnit && currentUnit != null)
        {
            // Đang ở skill wheel → quay lại chọn unit
            AudioManager.Instance?.PlayUICancel();
            CloseSkillWheel();
            ClearTargetHighlights();
            currentUnit = null;
            selectedSkill = null;
            isSelectingUnit = true;
            var remaining = combat.PlayerUnits.Where(u => u.CanActThisTurn).ToList();
            SetInstruction("Chọn 1 nhân vật sáng để ra lệnh hành động");
            UpdateUnitEmphasis(remaining);
            return;
        }

        // Đang chọn unit → không làm gì (hoặc có thể thêm sau)
    }

    private void HighlightValidTargets(SkillData skill)
    {
        ClearTargetHighlights();
        UpdateUnitEmphasis(null);
        if (targetHighlightPrefab == null) return;
        IEnumerable<CombatUnit> pool;
        switch (skill.targetType)
        {
            case TargetType.SingleEnemy: case TargetType.AllEnemies: pool = combat.EnemyUnits.Where(e => e.IsAlive && e.IsTargetable); break;
            case TargetType.SingleAlly: case TargetType.AllAllies: pool = combat.PlayerUnits.Where(p => p.IsAlive); break;
            case TargetType.Self: pool = new List<CombatUnit> { currentUnit }; break;
            default: pool = Enumerable.Empty<CombatUnit>(); break;
        }
        foreach (var unit in pool)
        {
            var view = combat.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == unit);
            if (view == null) continue;
            var go = Instantiate(targetHighlightPrefab, view.transform);
            targetHighlights.Add(go);
        }
    }

    private void ClearTargetHighlights()
    {
        foreach (var go in targetHighlights) if (go != null) Destroy(go);
        targetHighlights.Clear();
    }

    private void SetInstruction(string text) { if (instructionText != null) instructionText.text = text; }

    private void AddHoverEffect(GameObject go, CombatUnit unit, int skillIdx, SkillData skill)
    {
        var trigger = go.AddComponent<EventTrigger>();
        bool canAfford = skill.apCost <= combat.CurrentPlayerAP;

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ =>
        {
            if (!canAfford) return;
            var rect = go.GetComponent<RectTransform>();
            if (rect != null && gameObject.activeInHierarchy)
                StartCoroutine(HoverScaleCoroutine(rect, 1f, 1.15f));
            var img = go.GetComponent<Image>();
            if (img != null) img.color = skillHoverColor;
            AudioManager.Instance?.PlayUIHover();

            // Hiển thị mô tả kỹ năng khi hover vào nút kỹ năng
            string targetHint = GetTargetTypeHint(skill.targetType);
            string desc = string.IsNullOrEmpty(skill.description) ? "Chưa có mô tả." : skill.description;
            SetInstruction($"{skill.skillName} (AP: {skill.apCost})\n{targetHint}\n{desc}");
        });
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ =>
        {
            if (!canAfford) return;
            var rect = go.GetComponent<RectTransform>();
            if (rect != null && gameObject.activeInHierarchy)
                StartCoroutine(HoverScaleCoroutine(rect, rect.localScale.x, 1f));
            var img = go.GetComponent<Image>();
            if (img != null) img.color = (selectedSkill == skill) ? skillSelectedColor : skillNormalColor;

            // Khôi phục text hướng dẫn khi rời khỏi nút kỹ năng
            if (currentUnit != null && !isChoosingTarget)
                SetInstruction($"{currentUnit.UnitName} — Chọn kỹ năng (di chuột vào để xem mô tả)");
        });
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// Chuyển TargetType thành text hướng dẫn ngắn gọn.
    /// </summary>
    private string GetTargetTypeHint(TargetType type)
    {
        switch (type)
        {
            case TargetType.SingleEnemy: return "Mục tiêu: 1 kẻ địch";
            case TargetType.AllEnemies: return "Mục tiêu: Tất cả kẻ địch";
            case TargetType.SingleAlly: return "Mục tiêu: 1 đồng minh";
            case TargetType.AllAllies: return "Mục tiêu: Tất cả đồng minh";
            case TargetType.Self: return "Mục tiêu: Bản thân";
            default: return "";
        }
    }

    private IEnumerator HoverScaleCoroutine(RectTransform rect, float from, float to)
    {
        float duration = 0.12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(from, to, t);
            rect.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        rect.localScale = new Vector3(to, to, to);
    }

    private void UpdateUnitEmphasis(List<CombatUnit> selectableUnits = null)
    {
        var allViews = combat.GetAllUnitViews();
        
        if (isSelectingUnit && selectableUnits != null)
        {
            // Chế độ chọn unit: highlight đúng unit trong danh sách selectable (dùng Contains,
            // thay vì suy từ HasActedThisTurn) — đảm bảo unit được ExtraTurnEffect cấp thêm
            // lượt (ActionsRemainingThisTurn > 0) luôn sáng để player chọn.
            foreach (var view in allViews)
            {
                bool selectable = view.LinkedUnit.IsPlayer && view.LinkedUnit.IsAlive
                                  && (selectableUnits.Contains(view.LinkedUnit) || view.LinkedUnit.CanActThisTurn);
                view.SetAlpha(selectable ? 1f : 0.4f);
            }
            return;
        }

        if (!isChoosingTarget || selectedSkill == null)
        {
            foreach (var view in allViews) view.SetAlpha(1f);
            return;
        }

        // Chế độ chọn target
        bool isTargetingEnemies = (selectedSkill.targetType == TargetType.SingleEnemy || selectedSkill.targetType == TargetType.AllEnemies);
        foreach (var view in allViews)
        {
            bool isTargetGroup = (isTargetingEnemies && !view.LinkedUnit.IsPlayer) || (!isTargetingEnemies && view.LinkedUnit.IsPlayer);
            bool isCaster = (view.LinkedUnit == currentUnit);
            view.SetAlpha((isTargetGroup || isCaster) ? 1f : 0.4f);
        }
    }

    private void UpdateAPDisplay() { if (apDisplay != null) apDisplay.text = $"AP: {combat.CurrentPlayerAP}"; }

    // ── Hover Inspect (viền sáng + xem nội tại — chỉ trong lượt player) ──
    /// <summary>
    /// Tìm UnitView dưới con trỏ chuột (raycast 2D rồi 3D fallback). Chỉ nhận unit còn sống.
    /// </summary>
    private UnitView GetUnitUnderCursor()
    {
        Ray ray = MainCam.ScreenPointToRay(Input.mousePosition);

        foreach (var hit in Physics2D.GetRayIntersectionAll(ray))
        {
            var view = hit.collider?.GetComponent<UnitView>();
            if (view != null && view.LinkedUnit != null && view.LinkedUnit.IsAlive) return view;
        }

        if (Physics.Raycast(ray, out RaycastHit hit3D, 100f))
        {
            var view = hit3D.collider?.GetComponent<UnitView>();
            if (view != null && view.LinkedUnit != null && view.LinkedUnit.IsAlive) return view;
        }
        return null;
    }

    /// <summary>
    /// Theo dõi unit đang hover: bật viền sáng + hiện mô tả nội tại lên bảng instruction.
    /// Rời chuột → tắt viền + trả lại text hướng dẫn theo ngữ cảnh hiện tại.
    /// </summary>
    private void UpdateHoverHighlight()
    {
        UnitView hovered = null;
        if (!isChoosingTarget && !EventSystem.current.IsPointerOverGameObject())
            hovered = GetUnitUnderCursor();

        if (hovered == _hoveredView) return;

        if (_hoveredView != null) _hoveredView.SetHoverHighlight(false);
        _hoveredView = hovered;

        if (_hoveredView != null)
        {
            _hoveredView.SetHoverHighlight(true);
            ShowUnitPassiveInfo(_hoveredView.LinkedUnit);
        }
        else
        {
            RestoreDefaultInstruction();
        }
    }

    private void ClearHoverHighlight()
    {
        if (_hoveredView != null)
        {
            if (_hoveredView != null && _hoveredView.gameObject != null)
                _hoveredView.SetHoverHighlight(false);
            _hoveredView = null;
        }
    }

    /// <summary>
    /// Hiển thị mô tả nội tại của unit lên bảng instruction (màu xanh = ta, đỏ = địch).
    /// Không đụng CanvasGroup — trong lượt player canvas đã tương tác bình thường.
    /// </summary>
    private void ShowUnitPassiveInfo(CombatUnit unit)
    {
        string color = unit.IsPlayer ? "#7CFC00" : "#FF6B6B";
        string desc = PassiveDescriptions.Get(unit);
        if (string.IsNullOrEmpty(desc)) desc = "Không có nội tại đặc biệt.";
        SetInstruction($"<color={color}>{unit.UnitName}</color> — Nội tại\n{desc}");
    }

    /// <summary>
    /// Trả lại text hướng dẫn phù hợp với trạng thái UI hiện tại (dùng khi hover rời khỏi unit).
    /// </summary>
    private void RestoreDefaultInstruction()
    {
        if (isChoosingTarget && selectedSkill != null)
            SetInstruction($"Chọn mục tiêu cho [{selectedSkill.skillName}] (Chuột phải để hủy)");
        else if (currentUnit != null && !isSelectingUnit)
            SetInstruction($"{currentUnit.UnitName} — Chọn kỹ năng (di chuột vào để xem mô tả)");
        else
            SetInstruction("Chọn 1 nhân vật sáng để ra lệnh hành động");
    }
}