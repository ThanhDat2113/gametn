using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static EquipmentDragItem CurrentDragging { get; private set; }

    public EquipmentData Equipment { get; private set; }

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    private CanvasGroup canvasGroup;
    private GameObject ghost;
    private EquipmentPanel panel;

    private bool _isDragging = false;
    private bool _isHovering = false;

    public void Initialize(EquipmentData equip, EquipmentPanel parentPanel)
    {
        Equipment = equip;
        panel = parentPanel;
        SetupCommon();

        if (equip == null) { SetEmptyVisual(); return; }

        if (icon != null)
        {
            icon.sprite = equip.icon;
            icon.enabled = true;
            icon.color = Color.white;
        }
        if (nameText != null)
            nameText.text = equip.itemName;
    }

    public void InitializeDummy(EquipmentPanel parentPanel)
    {
        Equipment = null;
        panel = parentPanel;
        SetupCommon();
        SetEmptyVisual();
    }

    private void SetupCommon()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (icon == null)
            icon = GetComponentInChildren<Image>();
    }

    private void SetEmptyVisual()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = true;
            icon.color = new Color(1, 1, 1, 0f);
        }
        if (nameText != null)
            nameText.text = "Empty";
    }

    public void DestroyGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
    }

    // ── Drag ──

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Equipment == null) return;

        _isDragging = true;
        CurrentDragging = this;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Bắt đầu drag preview - giữ preview của item này
        panel?.StartDragPreview(Equipment);

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        if (rootCanvas == null)
        {
            Debug.LogWarning("[EquipmentDragItem] Không tìm thấy rootCanvas!");
            return;
        }

        ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        ghost.transform.SetParent(rootCanvas.transform, false);

        var ghostRect = ghost.GetComponent<RectTransform>();
        ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        ghostRect.position = eventData.position;

        var ghostImg = ghost.GetComponent<Image>();
        ghostImg.sprite = icon != null ? icon.sprite : null;
        ghostImg.color = new Color(1, 1, 1, 0.7f);
        ghostImg.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghost != null)
            ghost.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        CurrentDragging = null;
        DestroyGhost();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Kết thúc drag preview - clear preview (không giữ lại)
        panel?.EndDragPreview(false);
    }

    // ── Hover ──

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Equipment == null || panel == null) return;

        // Nếu panel đang trong trạng thái drag preview, không thay đổi preview
        if (panel.IsDraggingPreview) return;

        _isHovering = true;
        panel.ShowPreview(Equipment);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;

        // Nếu đang drag, không clear preview (vì preview đang được quản lý bởi drag)
        if (_isDragging) return;
        if (panel == null) return;

        panel.ClearPreview();
    }
}