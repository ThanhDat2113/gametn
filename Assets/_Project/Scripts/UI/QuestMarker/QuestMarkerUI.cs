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

    [Header("Rotation")]
    [Tooltip("Bật để marker xoay chỉ hướng về NPC khi NPC ở ngoài khung hình (kiểu mũi tên edge). " +
             "Khi NPC visible trên màn hình, marker không xoay (đứng thẳng).")]
    [SerializeField] private bool enableRotation = true;

    [Tooltip("Offset góc để căn sprite gốc về đúng hướng.\n" +
             "Sprite chỉ RIGHT(→): 0°  |  UP(↑): -90°  |  LEFT(←): 180°  |  DOWN(↓): 90°")]
    [SerializeField] private float spriteAngleOffset = 0f;

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

        Vector2 screenPos;
        if (inView)
        {
            // NPC visible → đặt marker đúng vị trí NPC trên màn hình, không xoay.
            Vector3 sp = _mainCamera.WorldToScreenPoint(targetPos);
            screenPos = new Vector2(sp.x, sp.y);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);

            _rectTransform.anchoredPosition = canvasPos;
            if (enableRotation) _rectTransform.localRotation = Quaternion.identity;
        }
        else
        {
            // NPC ngoài khung hình → clamp về mép, xoay chỉ hướng về NPC.
            screenPos = ScreenEdgeMarkerCalculator.CalculateEdgeScreenPos(targetPos, _mainCamera, edgePadding);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);

            _rectTransform.anchoredPosition = canvasPos;

            if (enableRotation)
            {
                float angle = ScreenEdgeMarkerCalculator.CalculateArrowRotation(targetPos, _mainCamera);
                // localRotation (không phải rotation) để xoay đúng trong không gian local của
                // canvas, không bị ảnh hưởng bởi rotation của parent (vd minimap container xoay).
                _rectTransform.localRotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
            }
        }

        _canvasGroup.alpha = 1f;
    }

    public void SetActive(bool active) => gameObject.SetActive(active);
}
