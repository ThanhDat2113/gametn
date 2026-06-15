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

    [Tooltip("Offset góc để căn sprite về đúng hướng khi angle = 0° (target ở phải player).\n" +
             "Sprite chỉ RIGHT(→) = 0° | LEFT(←) = 180° | UP(↑) = -90° | DOWN(↓) = 90°")]
    [SerializeField] private float spriteAngleOffset = 180f; // Sprite gốc chỉ trái

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
            ? canvas.worldCamera : null;
    }

    private void Update()
    {
        if (_targetBridge == null || _mainCamera == null || _canvasRect == null) return;

        Vector3 targetPos = _targetBridge.MarkerPosition;
        Vector3 playerPos = _targetBridge.PlayerTransform != null
            ? _targetBridge.PlayerTransform.position
            : _mainCamera.transform.position;

        bool inView = ScreenEdgeMarkerCalculator.IsInViewFrustum(targetPos, _mainCamera);

        if (inView)
        {
            // NPC trong màn hình: đặt marker đúng vị trí, ẩn rotation
            Vector3 sp = _mainCamera.WorldToScreenPoint(targetPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, new Vector2(sp.x, sp.y), _uiCamera, out Vector2 canvasPos);

            _rectTransform.anchoredPosition = canvasPos;
            _rectTransform.localRotation = Quaternion.identity;
        }
        else
        {
            // NPC ngoài màn hình: clamp về mép + xoay mũi tên
            Vector2 screenPos = ScreenEdgeMarkerCalculator.CalculateEdgeScreenPos(
                playerPos, targetPos, _mainCamera, edgePadding);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);

            float angle = ScreenEdgeMarkerCalculator.CalculateArrowRotation(
                playerPos, targetPos, _mainCamera);

            _rectTransform.anchoredPosition = canvasPos;
            _rectTransform.localRotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
        }

        _canvasGroup.alpha = 1f;
    }

    public void SetActive(bool active) => gameObject.SetActive(active);
}
