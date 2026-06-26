using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class QuestMarkerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image arrowIcon;

    [Header("Settings")]
    [SerializeField] private float edgePadding = 20f;

    private RectTransform _rectTransform;
    private CanvasGroup   _canvasGroup;

    private QuestMarkerBridge _targetBridge;
    private Camera            _mainCamera;
    private RectTransform     _canvasRect;
    private Camera            _uiCamera;

    public QuestMarkerBridge TargetBridge => _targetBridge;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        if (arrowIcon == null) arrowIcon = GetComponent<Image>();
        _mainCamera = Camera.main;
    }

    public void InitializeFromBridge(QuestMarkerBridge bridge, RectTransform canvasParent)
    {
        if (bridge == null) { Debug.LogError("[QuestMarkerUI] bridge is null"); return; }
        _targetBridge = bridge;
        _canvasRect   = canvasParent;

        Canvas canvas = canvasParent.GetComponentInParent<Canvas>();
        _uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;
    }

    private void Update()
    {
        if (_targetBridge == null || _mainCamera == null || _canvasRect == null) return;

        Vector3 targetPos = _targetBridge.MarkerPosition;
        bool inView = ScreenEdgeMarkerCalculator.IsInViewFrustum(targetPos, _mainCamera);

        // NPC visible → đặt marker đúng vị trí NPC trên màn hình.
        // NPC ngoài khung hình → clamp về mép gần nhất theo hướng NPC.
        Vector2 screenPos = inView
            ? (Vector2)_mainCamera.WorldToScreenPoint(targetPos)
            : ScreenEdgeMarkerCalculator.CalculateEdgeScreenPos(targetPos, _mainCamera, edgePadding);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);

        _rectTransform.anchoredPosition = canvasPos;
        _canvasGroup.alpha = 1f;
    }

    public void SetActive(bool active) => gameObject.SetActive(active);
}