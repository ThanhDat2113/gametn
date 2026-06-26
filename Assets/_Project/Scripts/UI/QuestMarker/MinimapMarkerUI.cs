using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Marker NPC hiển thị trên Minimap. Tái sử dụng QuestMarkerBridge (cùng nguồn
/// dữ liệu với marker chỉ đường trên màn hình chính) để không cần hệ thống quest riêng.
///
/// OUT-OF-RANGE BEHAVIOR (chọn qua enum trong Inspector):
///   • Clamp — kẹp marker vào mép minimap (giống edge marker màn hình chính)
///   • Hide  — ẩn marker hoàn toàn khi NPC ngoài phạm vi minimap
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class MinimapMarkerUI : MonoBehaviour
{
    public enum OutOfRangeBehavior { Clamp, Hide }

    [Header("Out-of-range Behavior")]
    [Tooltip("Clamp: kẹp marker vào mép minimap khi NPC ngoài phạm vi.\n" +
             "Hide: ẩn marker hoàn toàn khi NPC ngoài phạm vi.")]
    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.Clamp;

    [Header("UI References")]
    [SerializeField] private Image markerIcon;

    [Tooltip("Padding tính từ mép minimap khi clamp (UI units).")]
    [SerializeField] private float edgePadding = 8f;

    private RectTransform _rectTransform;
    private CanvasGroup   _canvasGroup;
    private QuestMarkerBridge _targetBridge;

    public QuestMarkerBridge TargetBridge => _targetBridge;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        if (markerIcon == null) markerIcon = GetComponent<Image>();
    }

    public void InitializeFromBridge(QuestMarkerBridge bridge)
    {
        if (bridge == null) { Debug.LogError("[MinimapMarkerUI] bridge is null"); return; }
        _targetBridge = bridge;
    }

    private void Update()
    {
        if (_targetBridge == null || MinimapController.Instance == null) return;

        Vector2 mapPos = MinimapController.Instance.WorldToMinimapPosition(
            _targetBridge.MarkerPosition, out bool isWithinRange);

        if (isWithinRange)
        {
            _rectTransform.anchoredPosition = mapPos;
            _canvasGroup.alpha = 1f;
        }
        else
        {
            switch (outOfRangeBehavior)
            {
                case OutOfRangeBehavior.Clamp:
                    _rectTransform.anchoredPosition = MinimapController.Instance.ClampToMinimapEdge(mapPos, edgePadding);
                    _canvasGroup.alpha = 1f;
                    break;

                case OutOfRangeBehavior.Hide:
                    _canvasGroup.alpha = 0f;
                    break;
            }
        }
    }

    public void SetActive(bool active) => gameObject.SetActive(active);
}
