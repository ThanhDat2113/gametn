using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Combat Planning UI — thay thế CombatTestUI cho phần chọn skill.
///
/// TÍNH NĂNG:
///   • Click nhân vật trên sân → Skill Wheel mở ra 2 bên
///   • Bên trái: Skill 1, 2, 3  |  Bên phải: Skill 4, 5, Ultimate
///   • Sau khi chọn skill → click enemy/ally trên sân để chọn target
///   • Hàng Action Bar bên dưới: icon nhân vật đã chọn skill
///   • Kéo thả trong Action Bar để đổi thứ tự hành động (PlanningOrder)
///   • CONFIRM → SubmitAllPlayerChoices
///
/// SETUP:
///   1. Gán script này vào GameObject "CombatPlanningUI"
///   2. Kéo Canvas vào planningCanvas
///   3. Tạo Prefab cho skillButtonPrefab, actionSlotPrefab
///   4. Kéo các Transform anchor vào đúng slots
/// </summary>
public class CombatPlanningUI : MonoBehaviour
{
    // ── Canvas References ─────────────────────────────────────
    [Header("Canvas")]
    public Canvas planningCanvas;

    // ── Skill Wheel ───────────────────────────────────────────
    [Header("Skill Wheel")]
    [Tooltip("Prefab button cho skill thường (Skill 1-5)")]
    public GameObject skillButtonPrefab;

    [Tooltip("Prefab button cho ULTI (index 5+) — cùng kích thước nhưng style khác. Để trống thì dùng chung skillButtonPrefab.")]
    public GameObject skillButtonUltiPrefab;

    [Tooltip("Container chứa skill bên trái (Skill 1,2,3) — pivot: center-right")]
    public RectTransform leftSkillContainer;

    [Tooltip("Container chứa skill bên phải (Skill 4,5,Ulti) — pivot: center-left")]
    public RectTransform rightSkillContainer;

    [Tooltip("Khoảng cách ngang từ tâm nhân vật đến cạnh trong của cột skill")]
    public float skillColumnOffset = 160f;

    [Tooltip("Khoảng cách dọc giữa các skill button")]
    public float skillRowSpacing = 70f;

    // ── Skill Wheel Visual ────────────────────────────────────
    [Header("Skill Wheel Visual")]
    public Color skillNormalColor = new Color(0.1f, 0.1f, 0.15f, 0.92f);
    public Color skillHoverColor = new Color(0.85f, 0.2f, 0.1f, 1f);
    public Color skillCooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    public Color skillSelectedColor = new Color(1f, 0.7f, 0f, 1f);

    // ── Action Bar ────────────────────────────────────────────
    [Header("Action Bar")]
    [Tooltip("Panel chứa toàn bộ Action Bar")]
    public RectTransform actionBarPanel;

    [Tooltip("Prefab 1 slot trong Action Bar — cần Image (portrait) + TextMeshProUGUI (tên skill)")]
    public GameObject actionSlotPrefab;

    [Tooltip("Container layout ngang cho các slot")]
    public RectTransform actionSlotContainer;

    [Tooltip("Button CONFIRM")]
    public Button confirmButton;

    [Tooltip("Text hiển thị hướng dẫn hiện tại")]
    public TextMeshProUGUI instructionText;

    // ── Target Highlight ──────────────────────────────────────
    [Header("Target Highlight")]
    [Tooltip("Prefab vòng highlight khi đang chọn target — SpriteRenderer hoặc UI Image")]
    public GameObject targetHighlightPrefab;

    // ── Internal State ────────────────────────────────────────
    private CombatManager combat;
    private Camera mainCam;

    // Planning state
    private List<CombatUnit> planningUnits = new();   // tất cả player alive
    private CombatUnit activeUnit;              // unit đang mở skill wheel
    private CombatUnit pendingUnit;             // unit đang chờ chọn target
    private SkillData pendingSkill;            // skill vừa chọn, chờ target
    private bool isChoosingTarget;

    // Choices: unit → (skill, targets)  — map để dễ cập nhật
    private Dictionary<CombatUnit, (SkillData skill, List<CombatUnit> targets)> choices = new();

    // Action Bar order (thứ tự kéo thả)
    private List<CombatUnit> actionOrder = new();

    // Skill wheel buttons đang hiển thị
    private List<GameObject> activeSkillButtons = new();

    // Target highlight instances
    private List<GameObject> targetHighlights = new();

    // Action slot UI objects (đồng bộ với actionOrder)
    private List<ActionSlotUI> actionSlots = new();

    // Drag state
    private int dragFromIndex = -1;
    private bool isDragging;

    // ─────────────────────────────────────────────────────────
    private void Start()
    {
        mainCam = Camera.main;
        combat = CombatManager.Instance;

        if (combat == null)
        {
            Debug.LogError("[PlanUI] Không tìm thấy CombatManager.Instance!");
            return;
        }

        combat.OnPlayerPlanStarted += OnPlanStarted;
        combat.OnExecuteStarted += OnExecuteStarted;
        combat.OnVictory += HideAll;
        combat.OnDefeat += HideAll;

        Debug.Log("[PlanUI] Subscribed to CombatManager events OK");

        confirmButton.onClick.AddListener(OnConfirm);
        confirmButton.interactable = false;

        HideAll();
    }

    private void OnDestroy()
    {
        if (combat == null) return;
        combat.OnPlayerPlanStarted -= OnPlanStarted;
        combat.OnExecuteStarted -= OnExecuteStarted;
        combat.OnVictory -= HideAll;
        combat.OnDefeat -= HideAll;
    }

    // ─────────────────────────────────────────────────────────
    private void Update()
    {
        if (!planningCanvas.gameObject.activeSelf) return;

        // Click chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick(Input.mousePosition);
        }

        // ESC / chuột phải hủy
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelCurrentAction();
        }
    }

    // ── Plan Started ──────────────────────────────────────────
    private void OnPlanStarted(List<CombatUnit> units)
    {
        Debug.Log($"[PlanUI] OnPlanStarted fired — {units.Count} units");
        foreach (var u in units)
            Debug.Log($"  unit: {u.UnitName}  portrait={(u.Data.portrait != null ? "OK" : "NULL")}");

        planningUnits = units;
        choices.Clear();
        actionOrder = new List<CombatUnit>(units);
        activeUnit = null;
        pendingUnit = null;
        pendingSkill = null;
        isChoosingTarget = false;

        if (planningCanvas == null)
        { Debug.LogError("[PlanUI] planningCanvas chưa được gán!"); return; }
        if (actionBarPanel == null)
        { Debug.LogError("[PlanUI] actionBarPanel chưa được gán!"); return; }
        if (actionSlotPrefab == null)
        { Debug.LogError("[PlanUI] actionSlotPrefab chưa được gán!"); return; }
        if (actionSlotContainer == null)
        { Debug.LogError("[PlanUI] actionSlotContainer chưa được gán!"); return; }

        planningCanvas.gameObject.SetActive(true);
        actionBarPanel.gameObject.SetActive(true);

        RebuildActionBar();
        UpdateConfirmButton();
        SetInstruction("Nhấn vào nhân vật để chọn skill");

        Debug.Log($"[PlanUI] RebuildActionBar done — {actionSlots.Count} slots created");
    }

    private void OnExecuteStarted() => HideAll();
    private void HideAll()
    {
        CloseSkillWheel();
        ClearTargetHighlights();

        // Xóa slot thật sự khi kết thúc planning (không phải trong round)
        foreach (var slot in actionSlots)
            if (slot != null) Destroy(slot.gameObject);
        actionSlots.Clear();

        planningCanvas.gameObject.SetActive(false);
    }

    // ── Mouse Click Handler ───────────────────────────────────
    private void HandleMouseClick(Vector3 mousePos)
    {
        // 1. Nếu click vào UI element → không xử lý raycast world
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 2. Raycast vào world — chỉ lấy hit đầu tiên có UnitView
        Ray ray = mainCam.ScreenPointToRay(mousePos);
        var hits = Physics2D.GetRayIntersectionAll(ray);

        UnitView clickedView = null;
        foreach (var hit in hits)
        {
            var view = hit.collider?.GetComponent<UnitView>();
            if (view != null && view.LinkedUnit.IsAlive)
            {
                clickedView = view;
                break; // chỉ xử lý 1 unit dù collider chồng nhau
            }
        }

        // 3. Xử lý đúng 1 lần — guard bằng isChoosingTarget
        if (isChoosingTarget)
            HandleTargetClick(clickedView);
        else
            HandleUnitClick(clickedView);
    }

    // ── Click nhân vật để mở skill wheel ─────────────────────
    private void HandleUnitClick(UnitView view)
    {
        if (view == null)
        {
            CloseSkillWheel();
            return;
        }

        var unit = view.LinkedUnit;

        // Chỉ cho phép click player alive
        if (!unit.IsPlayer || !unit.IsAlive) return;

        // Toggle: click lại unit đang mở → đóng
        if (activeUnit == unit)
        {
            CloseSkillWheel();
            return;
        }

        OpenSkillWheel(unit, view);
    }

    // ── Mở Skill Wheel ────────────────────────────────────────
    // Layout theo sketch:
    //
    //   [Skill1]  |     | [Skill4]
    //   [Skill2]  | NV  | [Skill5]
    //   [Skill3]  |     | [ULTI ]
    //
    // Trái: skill index 0,1,2  — xếp dọc, anchor phải
    // Phải: skill index 3,4    — xếp dọc trên
    //        skill index 5+    — ULTI, cùng kích thước, tag "ulti" để style riêng
    //
    // Tất cả button CÙNG kích thước — bạn tự style ULTI qua skillButtonUltiPrefab
    // hoặc dựa vào tag name "ulti" trong prefab child.
    private void OpenSkillWheel(CombatUnit unit, UnitView view)
    {
        CloseSkillWheel();
        activeUnit = unit;

        var skills = unit.Data.skills;

        // World → screen → canvas local
        Vector2 screenPos = mainCam.WorldToScreenPoint(view.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            planningCanvas.GetComponent<RectTransform>(),
            screenPos, planningCanvas.worldCamera,
            out Vector2 canvasPos);

        // Căn container: trái sát nhân vật, phải sát nhân vật
        // leftSkillContainer  anchor: right edge chạm nhân vật
        // rightSkillContainer anchor: left edge chạm nhân vật
        leftSkillContainer.anchoredPosition = new Vector2(
            canvasPos.x - skillColumnOffset, canvasPos.y);
        rightSkillContainer.anchoredPosition = new Vector2(
            canvasPos.x + skillColumnOffset, canvasPos.y);

        for (int i = 0; i < skills.Length; i++)
        {
            var skill = skills[i];
            bool isLeft = i < 3;                          // 0,1,2 → trái
            var container = isLeft ? leftSkillContainer : rightSkillContainer;
            int localIdx = isLeft ? i : (i - 3);          // vị trí trong cột

            // Chọn prefab: ULTI (index 5) dùng prefab riêng nếu có
            bool isUlti = (i >= 5);
            var prefab = (isUlti && skillButtonUltiPrefab != null)
                           ? skillButtonUltiPrefab
                           : skillButtonPrefab;

            var go = Instantiate(prefab, container);
            var rect = go.GetComponent<RectTransform>();

            // Xếp dọc từ trên xuống, căn theo pivot của container
            // Trái: xếp từ phải sang (pivot phải), phải: từ trái sang (pivot trái)
            rect.anchoredPosition = new Vector2(0f, -localIdx * skillRowSpacing);

            // Gán text
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                bool cd = !unit.IsSkillReady(i);
                label.text = cd
                    ? $"{skill.skillName}\n<size=70%>CD: {unit.SkillCooldowns[i]}</size>"
                    : $"{skill.skillName}\n<size=70%>{skill.basePoint}pt</size>";
            }

            // Màu nền
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                bool onCD = !unit.IsSkillReady(i);
                bool isChosen = choices.TryGetValue(unit, out var ch) && ch.skill == skill;
                img.color = onCD ? skillCooldownColor :
                            isChosen ? skillSelectedColor :
                                        skillNormalColor;
            }

            // Button
            int capturedIdx = i;
            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = unit.IsSkillReady(i);
                btn.onClick.AddListener(() => OnSkillButtonClicked(capturedIdx));
            }

            AddHoverEffect(go, unit, i);
            activeSkillButtons.Add(go);
        }

        SetInstruction($"{unit.UnitName} — chọn skill");
    }

    private void CloseSkillWheel()
    {
        foreach (var go in activeSkillButtons)
            if (go != null) Destroy(go);
        activeSkillButtons.Clear();
        activeUnit = null;

        if (!isChoosingTarget)
            SetInstruction("Nhấn vào nhân vật để chọn skill");
    }

    // ── Skill Button Clicked ──────────────────────────────────
    private void OnSkillButtonClicked(int skillIndex)
    {
        if (activeUnit == null) return;

        // Lưu unit và skill trước khi CloseSkillWheel() xóa activeUnit
        pendingUnit = activeUnit;
        var skill = activeUnit.Data.skills[skillIndex];
        pendingSkill = skill;

        CloseSkillWheel(); // activeUnit = null sau đây

        // Auto-target
        if (skill.targetType == TargetType.AllEnemies)
        {
            var targets = combat.EnemyUnits.Where(e => e.IsAlive).ToList();
            ConfirmSkillChoice(pendingUnit, skill, targets);
            pendingUnit = null; pendingSkill = null;
            return;
        }
        if (skill.targetType == TargetType.AllAllies)
        {
            var targets = combat.PlayerUnits.Where(p => p.IsAlive).ToList();
            ConfirmSkillChoice(pendingUnit, skill, targets);
            pendingUnit = null; pendingSkill = null;
            return;
        }

        // SingleTarget → chờ player click target
        isChoosingTarget = true;
        HighlightValidTargets(skill);
        SetInstruction($"Chọn mục tiêu cho [{skill.skillName}]  (Chuột phải để hủy)");
    }

    // ── Target Click ──────────────────────────────────────────
    private void HandleTargetClick(UnitView view)
    {
        // Guard: isChoosingTarget có thể bị set false bởi FinishTargetSelection
        // trong cùng frame nếu collider chồng nhau → chặn gọi lần 2
        if (!isChoosingTarget) return;
        if (view == null) return;

        var target = view.LinkedUnit;
        if (!target.IsAlive) return;

        bool wantEnemy = pendingSkill.targetType == TargetType.SingleEnemy;
        bool wantAlly = pendingSkill.targetType == TargetType.SingleAlly;

        if (wantEnemy && !target.IsPlayer)
            FinishTargetSelection(new List<CombatUnit> { target });
        else if (wantAlly && target.IsPlayer)
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

    // ── Confirm Skill Choice ──────────────────────────────────
    private void ConfirmSkillChoice(CombatUnit unit, SkillData skill,
                                    List<CombatUnit> targets)
    {
        if (unit == null) return;

        choices[unit] = (skill, targets);

        // Đảm bảo unit có trong actionOrder
        if (!actionOrder.Contains(unit))
            actionOrder.Add(unit);

        RebuildActionBar();
        UpdateConfirmButton();

        string targetNames = string.Join(", ", targets.Select(t => t.UnitName));
        Debug.Log($"[PlanUI] {unit.UnitName} → [{skill.skillName}] → [{targetNames}]");
    }

    // ── Target Highlights ─────────────────────────────────────
    private void HighlightValidTargets(SkillData skill)
    {
        ClearTargetHighlights();
        if (targetHighlightPrefab == null) return;

        var pool = skill.targetType == TargetType.SingleEnemy
            ? combat.EnemyUnits.Where(e => e.IsAlive)
            : combat.PlayerUnits.Where(p => p.IsAlive);

        foreach (var unit in pool)
        {
            var view = combat.GetUnitView(unit);
            if (view == null) continue;

            var go = Instantiate(targetHighlightPrefab,
                view.transform.position, Quaternion.identity);
            targetHighlights.Add(go);
        }
    }

    private void ClearTargetHighlights()
    {
        foreach (var go in targetHighlights)
            if (go != null) Destroy(go);
        targetHighlights.Clear();
    }

    // ── Action Bar ────────────────────────────────────────────
    // REUSE slot thay vì Destroy/Instantiate — tránh trigger event trên slot đang destroy
    private void RebuildActionBar()
    {
        var aliveUnits = actionOrder.Where(u => u.IsAlive).ToList();

        // Tạo thêm slot nếu thiếu
        while (actionSlots.Count < aliveUnits.Count)
        {
            var go = Instantiate(actionSlotPrefab, actionSlotContainer);
            var slot = go.GetComponent<ActionSlotUI>();
            if (slot == null) slot = go.AddComponent<ActionSlotUI>();
            actionSlots.Add(slot);
        }

        // Ẩn slot thừa
        for (int i = aliveUnits.Count; i < actionSlots.Count; i++)
            actionSlots[i].gameObject.SetActive(false);

        // Update từng slot tại chỗ
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

            actionSlots[i].gameObject.SetActive(true);
            actionSlots[i].Setup(unit, skill, targets, i, this);
        }
    }

    private void UpdateConfirmButton()
    {
        // Có thể CONFIRM khi tất cả unit alive đã chọn skill
        bool allChosen = planningUnits.All(u => !u.IsAlive || choices.ContainsKey(u));
        confirmButton.interactable = allChosen;

        var txt = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
            txt.text = allChosen ? "✓  CONFIRM" :
                $"Còn {planningUnits.Count(u => u.IsAlive && !choices.ContainsKey(u))} nhân vật chưa chọn";
    }

    // ── Drag & Drop reorder (INSERT style) ───────────────────
    // Kéo slot A thả vào vị trí trước slot B
    // → A chèn vào trước B, các slot giữa dịch chuyển

    private int hoveredSlotIndex = -1; // slot đang được hover khi drag

    public void OnSlotDragStart(int index)
    {
        dragFromIndex = index;
        isDragging = true;
        hoveredSlotIndex = -1;
    }

    public void OnSlotDragging(Vector2 screenPos)
    {
        if (!isDragging) return;

        // Tìm slot gần nhất với vị trí chuột để hiện drop indicator
        int nearest = FindNearestSlot(screenPos);
        if (nearest != hoveredSlotIndex)
        {
            // Ẩn indicator cũ
            foreach (var s in actionSlots) s.HideIndicator();

            // Hiện indicator ở slot mới (trừ chính slot đang drag)
            if (nearest >= 0 && nearest != dragFromIndex)
                actionSlots[nearest].ShowIndicator();

            hoveredSlotIndex = nearest;
        }
    }

    public void OnSlotHovered(int index)
    {
        // Gọi từ OnPointerEnter của slot — cập nhật hoveredSlotIndex
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

        // Không hợp lệ hoặc thả vào chính nó → không đổi
        if (swapWith < 0 || swapWith == dragFromIndex)
        {
            dragFromIndex = -1;
            return;
        }

        var alive = actionOrder.Where(u => u.IsAlive).ToList();

        if (dragFromIndex >= alive.Count || swapWith >= alive.Count)
        {
            dragFromIndex = -1;
            return;
        }

        // SWAP: đổi chỗ 2 nhân vật
        var tmp = alive[dragFromIndex];
        alive[dragFromIndex] = alive[swapWith];
        alive[swapWith] = tmp;

        actionOrder = alive;
        dragFromIndex = -1;

        RebuildActionBar();
    }

    // Tìm slot gần chuột nhất (theo X) trong actionSlots
    private int FindNearestSlot(Vector2 screenPos)
    {
        if (actionSlots.Count == 0) return -1;

        float minDist = float.MaxValue;
        int nearest = -1;

        for (int i = 0; i < actionSlots.Count; i++)
        {
            var slotRect = actionSlots[i].GetComponent<RectTransform>();
            Vector2 slotScreen = RectTransformUtility.WorldToScreenPoint(
                null, slotRect.position);

            float dist = Mathf.Abs(screenPos.x - slotScreen.x);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }
        return nearest;
    }

    // ── CONFIRM ───────────────────────────────────────────────
    private void OnConfirm()
    {
        if (!planningUnits.All(u => !u.IsAlive || choices.ContainsKey(u))) return;

        // Build list theo thứ tự actionOrder
        var submitList = actionOrder
            .Where(u => u.IsAlive && choices.ContainsKey(u))
            .Select(u =>
            {
                var (skill, targets) = choices[u];
                return (u, skill, targets);
            })
            .ToList();

        combat.SubmitAllPlayerChoices(submitList);
        HideAll();
    }

    // ── Helpers ───────────────────────────────────────────────
    private void SetInstruction(string text)
    {
        if (instructionText != null)
            instructionText.text = text;
    }

    private void AddHoverEffect(GameObject go, CombatUnit unit, int skillIdx)
    {
        var trigger = go.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry
        { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ =>
        {
            var img = go.GetComponent<Image>();
            if (img != null && unit.IsSkillReady(skillIdx))
                img.color = skillHoverColor;
        });
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry
        { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ =>
        {
            var img = go.GetComponent<Image>();
            if (img == null) return;
            bool isChosen = choices.TryGetValue(unit, out var ch) &&
                            ch.skill == unit.Data.skills[skillIdx];
            img.color = isChosen ? skillSelectedColor : skillNormalColor;
        });
        trigger.triggers.Add(exitEntry);
    }
}