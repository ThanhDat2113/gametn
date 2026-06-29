using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Component gắn trên các ổ cắm bên PHẢI — nhận thả dây.
/// Auto-assign bởi FlowPuzzle khi khởi tạo.
/// </summary>
public class WireDropTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
    /// Lấy vị trí tâm (screen space).
    /// </summary>
    public Vector2 GetScreenCenter()
    {
        Vector3[] corners = new Vector3[4];
        _rectTransform.GetWorldCorners(corners);
        return (corners[0] + corners[2]) / 2f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isConnected) return;
        if (flowPuzzleRef != null && flowPuzzleRef.IsDragging)
        {
            // Highlight khi kéo đến gần
            image.color = Color.Lerp(image.color, Color.white, 0.5f);
            image.transform.localScale = Vector3.one * 1.15f;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isConnected) return;
        image.transform.localScale = Vector3.one;
        if (flowPuzzleRef != null)
            image.color = flowPuzzleRef.GetWireColor(colorIndex);
    }
}
