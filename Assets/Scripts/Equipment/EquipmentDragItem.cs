using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public EquipmentData Equipment { get; private set; }

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    private CanvasGroup canvasGroup;
    private GameObject ghost;
    private Canvas rootCanvas;
    private EquipmentPanel panel;

    public void Initialize(EquipmentData equip, EquipmentPanel parentPanel)
    {
        Equipment = equip;
        panel = parentPanel;
        if (icon != null)
        {
            icon.sprite = equip.icon;
            icon.enabled = true;
        }
        if (nameText != null) nameText.text = equip.itemName;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void DestroyGhost()
    {
        if (ghost != null) Destroy(ghost);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        ghost.transform.SetParent(rootCanvas.transform, false);
        var ghostRect = ghost.GetComponent<RectTransform>();
        ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        ghostRect.position = eventData.position;

        var ghostImg = ghost.GetComponent<Image>();
        ghostImg.sprite = icon.sprite;
        ghostImg.color = new Color(1, 1, 1, 0.7f);
        ghostImg.raycastTarget = false;

        var ghostGroup = ghost.GetComponent<CanvasGroup>();
        ghostGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghost != null)
            ghost.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyGhost();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}