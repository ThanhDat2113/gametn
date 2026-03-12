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
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rootCanvas = GetComponentInParent<Canvas>();

        portrait = transform.Find("Portrait")?.GetComponent<Image>();
        borderImage = transform.Find("Border")?.GetComponent<Image>();
        unitNameText = transform.Find("UnitName")?.GetComponent<TextMeshProUGUI>();
        skillNameText = transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
        orderText = transform.Find("OrderText")?.GetComponent<TextMeshProUGUI>();
        dropIndicator = transform.Find("DropIndicator")?.GetComponent<Image>();

        HideIndicator();
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

        if (portrait != null && unit.Data.portrait != null)
            portrait.sprite = unit.Data.portrait;

        if (unitNameText != null) unitNameText.text = unit.UnitName;
        if (orderText != null) orderText.text = (index + 1).ToString();
        if (skillNameText != null)
            skillNameText.text = skill != null ? skill.skillName : "—";

        if (borderImage != null)
            borderImage.color = skill != null ? BorderNormal : BorderEmpty;

        HideIndicator();
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
        parentUI.OnSlotDragStart(SlotIndex);

        // Tạo ghost đơn giản: chỉ Image + portrait, KHÔNG clone toàn bộ gameObject
        // để tránh script bị trigger lại
        ghostObj = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup));
        ghostObj.transform.SetParent(rootCanvas.transform, false);

        var ghostRect = ghostObj.GetComponent<RectTransform>();
        ghostRect.sizeDelta = rectTransform.sizeDelta;

        var ghostGroup = ghostObj.GetComponent<CanvasGroup>();
        ghostGroup.alpha = 0.55f;
        ghostGroup.blocksRaycasts = false;

        // Copy background color
        var ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        // Copy portrait nếu có
        if (portrait != null && portrait.sprite != null)
        {
            var portraitGO = new GameObject("Portrait", typeof(RectTransform));
            portraitGO.transform.SetParent(ghostObj.transform, false);
            var pImg = portraitGO.AddComponent<Image>();
            pImg.sprite = portrait.sprite;
            pImg.color = GhostColor;
            var pRect = portraitGO.GetComponent<RectTransform>();
            var srcRect = portrait.rectTransform;
            pRect.anchorMin = srcRect.anchorMin;
            pRect.anchorMax = srcRect.anchorMax;
            pRect.sizeDelta = srcRect.sizeDelta;
            pRect.anchoredPosition = srcRect.anchoredPosition;
        }

        MoveGhost(eventData.position);

        canvasGroup.alpha = 0.3f;
        if (borderImage != null) borderImage.color = BorderDragging;
    }

    // ── IDragHandler ──────────────────────────────────────────
    public void OnDrag(PointerEventData eventData)
    {
        MoveGhost(eventData.position);
        parentUI.OnSlotDragging(eventData.position);
    }

    // ── IEndDragHandler ───────────────────────────────────────
    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostObj != null)
        {
            Destroy(ghostObj);
            ghostObj = null;
        }

        canvasGroup.alpha = 1f;
        if (borderImage != null)
            borderImage.color = LinkedSkill != null ? BorderNormal : BorderEmpty;

        parentUI.OnSlotDragEnd();
    }

    // ── IPointerEnterHandler ──────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
            parentUI.OnSlotHovered(SlotIndex);
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