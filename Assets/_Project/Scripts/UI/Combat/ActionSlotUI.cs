using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class ActionSlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler
{
    // ── Visual references ─────────────────────────────────────
    private Image portrait;
    private TextMeshProUGUI unitNameText;
    private TextMeshProUGUI skillNameText;
    private TextMeshProUGUI orderText;
    private Image borderImage;
    private Image dropIndicator;

    // ── Data ──────────────────────────────────────────────────
    public CombatUnit LinkedUnit { get; private set; }
    public SkillData LinkedSkill { get; private set; }
    public List<CombatUnit> LinkedTargets { get; private set; }
    public int SlotIndex { get; private set; }

    private CombatPlanningUI parentUI;

    // ── Drag ──────────────────────────────────────────────────
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    // Ghost: Image đơn giản, KHÔNG clone gameObject (tránh trigger script)
    private GameObject ghostObj;

    // ── Colors ────────────────────────────────────────────────
    private static readonly Color BorderNormal = new Color(0.6f, 0.5f, 0.2f, 1f);
    private static readonly Color BorderDragging = new Color(1f, 0.8f, 0.2f, 1f);
    private static readonly Color BorderEmpty = new Color(0.3f, 0.3f, 0.35f, 1f);
    private static readonly Color IndicatorOn = new Color(1f, 0.75f, 0.1f, 1f);
    private static readonly Color GhostColor = new Color(1f, 1f, 1f, 0.5f);

    // ─────────────────────────────────────────────────────────
    // Gán thủ công trong Inspector — ưu tiên hơn tìm theo tên
    [Header("References (kéo tay vào hoặc để trống — tự tìm)")]
    public Image portraitRef;
    public Image borderRef;
    public Image dropIndicatorRef;
    public TextMeshProUGUI unitNameRef;
    public TextMeshProUGUI skillNameRef;
    public TextMeshProUGUI orderRef;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rootCanvas = GetComponentInParent<Canvas>();

        // --- LOGIC MỚI: TÌM KIẾM COMPONENT MẠNH MẼ HƠN ---

        // Ưu tiên reference gán tay
        portrait = portraitRef;
        borderImage = borderRef;
        dropIndicator = dropIndicatorRef;
        unitNameText = unitNameRef;
        skillNameText = skillNameRef;
        orderText = orderRef;

        // Nếu không gán tay, tìm trong tất cả các con
        if (portrait == null) portrait = FindComponentInChildren<Image>("Portrait");
        if (borderImage == null) borderImage = FindComponentInChildren<Image>("Border");
        if (dropIndicator == null) dropIndicator = FindComponentInChildren<Image>("DropIndicator");
        if (unitNameText == null) unitNameText = FindComponentInChildren<TextMeshProUGUI>("UnitName");
        if (skillNameText == null) skillNameText = FindComponentInChildren<TextMeshProUGUI>("SkillName");
        if (orderText == null) orderText = FindComponentInChildren<TextMeshProUGUI>("OrderText");
        
        // --- KIỂM TRA LỖI ---
        if (portrait == null) Debug.LogError($"[ActionSlotUI] trên '{gameObject.name}' không tìm thấy Image con tên là 'Portrait'!");
        if (borderImage == null) Debug.LogError($"[ActionSlotUI] trên '{gameObject.name}' không tìm thấy Image con tên là 'Border'!");
        if (unitNameText == null) Debug.LogWarning($"[ActionSlotUI] trên '{gameObject.name}' không tìm thấy TextMeshPro con tên là 'UnitName'.");


        HideIndicator();
    }

    // Hàm helper mới để tìm component trong children bằng tên, bất kể cấp độ
    private T FindComponentInChildren<T>(string name) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true); // true để tìm cả trong các object bị deactive
        foreach (T component in components)
        {
            if (component.gameObject.name == name)
            {
                return component;
            }
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────
    public void Setup(CombatUnit unit, SkillData skill,
                      List<CombatUnit> targets, int index,
                      CombatPlanningUI parent)
    {
        LinkedUnit = unit;
        LinkedSkill = skill;
        LinkedTargets = targets;
        SlotIndex = index;
        parentUI = parent;

        Debug.Log($"[ActionSlotUI] Setup: unit={unit.UnitName} " +
                  $"portrait={(portrait != null ? "found" : "NULL")} " +
                  $"portraitRef={(portraitRef != null ? "found" : "NULL")} " +
                  $"data.portrait={(unit.Data.portrait != null ? "has sprite" : "NULL")}");

        // Dùng portrait nếu có, fallback sang battleSprite
        var portraitSprite = unit.Data.portrait != null
                             ? unit.Data.portrait
                             : unit.Data.battleSprite;

        if (portrait != null && portraitSprite != null)
        {
            portrait.sprite = portraitSprite;
            portrait.enabled = true;
        }

        if (unitNameText != null) unitNameText.text = unit.UnitName;
        if (orderText != null) orderText.text = (index + 1).ToString();
        if (skillNameText != null)
            skillNameText.text = skill != null ? skill.skillName : "—";

        if (borderImage != null)
            borderImage.color = skill != null ? BorderNormal : BorderEmpty;

        HideIndicator();
    }

    /// <summary>
    /// Hàm mới: Setup icon cho thanh Turn Order
    /// </summary>
    public void SetupForTurnOrder(CombatUnit unit, Color borderColor)
    {
        // QUAN TRỌNG: Kích hoạt GameObject TRƯỚC TIÊN để đảm bảo Awake() đã chạy
        // và các tham chiếu (portrait, unitNameText,...) không bị null.
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        LinkedUnit = unit;
        LinkedSkill = null; // Không có skill trong context này
        LinkedTargets = null;

        // CẬP NHẬT: Ưu tiên dùng ảnh chân dung (portrait), nếu không có thì dùng ảnh chiến đấu (battleSprite)
        if (portrait != null)
        {
            var spriteToShow = unit.Data.portrait ?? unit.Data.battleSprite;

            if (spriteToShow != null)
            {
                portrait.sprite = spriteToShow;
                portrait.enabled = true;
            }
            else
            {
                // Nếu không có ảnh nào cả, hãy tắt Image đi
                portrait.enabled = false;
            }
        }

        // Đặt màu nền/viền
        SetBorderColor(borderColor);

        // Ẩn các text không cần thiết
        if (unitNameText != null) unitNameText.gameObject.SetActive(false);
        if (skillNameText != null) skillNameText.gameObject.SetActive(false);
        if (orderText != null) orderText.gameObject.SetActive(false);
        
        HideIndicator();
    }

    /// <summary>
    /// Hàm mới: Thay đổi màu của viền
    /// </summary>
    public void SetBorderColor(Color color)
    {
        if (borderImage != null)
        {
            borderImage.color = color;
        }
    }

    // ── Drop indicator ────────────────────────────────────────
    public void ShowIndicator()
    {
        if (dropIndicator == null) return;
        dropIndicator.gameObject.SetActive(true);
        dropIndicator.color = IndicatorOn;
    }

    public void HideIndicator()
    {
        if (dropIndicator != null)
            dropIndicator.gameObject.SetActive(false);
    }

    // ── IBeginDragHandler ─────────────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        // parentUI.OnSlotDragStart(SlotIndex);

        // // Tạo ghost đơn giản: chỉ Image + portrait, KHÔNG clone toàn bộ gameObject
        // // để tránh script bị trigger lại
        // ghostObj = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
        // ghostObj.transform.SetParent(rootCanvas.transform, false);

        // var ghostRect = ghostObj.GetComponent<RectTransform>();
        // ghostRect.sizeDelta = rectTransform.sizeDelta;

        // var ghostGroup = ghostObj.GetComponent<CanvasGroup>();
        // ghostGroup.alpha = 0.55f;
        // ghostGroup.blocksRaycasts = false;

        // // Copy background color
        // var ghostImg = ghostObj.AddComponent<Image>();
        // ghostImg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        // // Copy portrait nếu có
        // if (portrait != null && portrait.sprite != null)
        // {
        //     var portraitGO = new GameObject("Portrait", typeof(RectTransform));
        //     portraitGO.transform.SetParent(ghostObj.transform, false);
        //     var pImg = portraitGO.AddComponent<Image>();
        //     pImg.sprite = portrait.sprite;
        //     pImg.color = GhostColor;
        //     var pRect = portraitGO.GetComponent<RectTransform>();
        //     var srcRect = portrait.rectTransform;
        //     pRect.anchorMin = srcRect.anchorMin;
        //     pRect.anchorMax = srcRect.anchorMax;
        //     pRect.sizeDelta = srcRect.sizeDelta;
        //     pRect.anchoredPosition = srcRect.anchoredPosition;
        // }

        // MoveGhost(eventData.position);

        // canvasGroup.alpha = 0.3f;
        // if (borderImage != null) borderImage.color = BorderDragging;
    }

    // ── IDragHandler ──────────────────────────────────────────
    public void OnDrag(PointerEventData eventData)
    {
        // MoveGhost(eventData.position);
        // parentUI.OnSlotDragging(eventData.position);
    }

    // ── IEndDragHandler ───────────────────────────────────────
    public void OnEndDrag(PointerEventData eventData)
    {
        // if (ghostObj != null)
        // {
        //     Destroy(ghostObj);
        //     ghostObj = null;
        // }

        // canvasGroup.alpha = 1f;
        // if (borderImage != null)
        //     borderImage.color = LinkedSkill != null ? BorderNormal : BorderEmpty;

        // parentUI.OnSlotDragEnd();
    }

    // ── IPointerEnterHandler ──────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        // if (eventData.pointerDrag != null)
        //     parentUI.OnSlotHovered(SlotIndex);
    }

    // ─────────────────────────────────────────────────────────
    private void MoveGhost(Vector2 screenPos)
    {
        if (ghostObj == null || rootCanvas == null) return;
        var ghostRect = ghostObj.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            screenPos, rootCanvas.worldCamera,
            out Vector3 worldPos);
        ghostRect.position = worldPos;
    }
}