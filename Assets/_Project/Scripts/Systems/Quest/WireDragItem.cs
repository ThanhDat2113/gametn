using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Component gắn trên các đầu dây bên TRÁI — cho phép kéo thả.
/// Auto-assign bởi FlowPuzzle khi khởi tạo.
/// </summary>
public class WireDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public FlowPuzzle flowPuzzleRef;
    [HideInInspector] public int colorIndex;
    [HideInInspector] public bool isConnected = false;

    public Image image { get; private set; }
    public CanvasGroup canvasGroup { get; private set; }
    private RectTransform _rectTransform;

    void Awake()
    {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Lấy vị trí tâm của item này (screen space).
    /// </summary>
    public Vector2 GetScreenCenter()
    {
        Vector3[] corners = new Vector3[4];
        _rectTransform.GetWorldCorners(corners);
        // corners = bottom-left, top-left, top-right, bottom-right
        return (corners[0] + corners[2]) / 2f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isConnected || flowPuzzleRef == null) return;
        flowPuzzleRef.OnBeginDragWire(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isConnected || flowPuzzleRef == null) return;
        flowPuzzleRef.OnDragWire(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isConnected || flowPuzzleRef == null) return;
        flowPuzzleRef.OnEndDragWire(this, eventData);
    }
}
