using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Icon marker duy nhất trên Canvas — kiểu hiển thị giống Honkai: Star Rail / Genshin:
///
///   TRONG TẦM NHÌN CAMERA:
///     Icon "lơ lửng" world-space tại vị trí target (+ chiều cao nổi lên),
///     không xoay, không clamp — bám đúng theo world position qua WorldToScreenPoint.
///
///   NGOÀI TẦM NHÌN CAMERA:
///     Icon clamp về mép màn hình theo hướng target — KHÔNG xoay (giữ góc identity).
///
/// Hoạt động cho cả 2 chế độ:
///   - NPC mode      (SetNPCMode)       → target = vị trí NPC (MarkerPosition)
///   - Waypoint mode (SetWaypointTarget) → target = vị trí waypoint hiện tại
///
/// QuestMarkerManager gọi InitializeFromBridge() như cũ, không cần sửa.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class QuestMarkerUI : MonoBehaviour
{
    // ── Serialized ────────────────────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("Icon chính dùng khi marker lơ lửng trong tầm nhìn (không xoay)")]
    [SerializeField] private Image floatingIcon;

    [Tooltip("Icon mũi tên dùng khi target ngoài màn hình (xoay theo hướng, fallback kiểu minimap)")]
    [SerializeField] private Image edgeArrowIcon;

    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Floating Marker (in view)")]
    [Tooltip("Độ cao marker lơ lửng phía trên target trong world units")]
    [SerializeField] private float floatHeight = 2.2f;

    [Header("Edge Arrow (out of view)")]
    [SerializeField] private float edgePadding = 20f;

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 6f;

    // ── Private ───────────────────────────────────────────────────────────────

    private RectTransform _rectTransform;
    private CanvasGroup   _canvasGroup;
    private Canvas        _parentCanvas;
    private RectTransform _canvasRect;
    private Camera        _mainCamera;
    private Camera        _uiCamera;

    private QuestMarkerBridge _targetBridge;

    // Waypoint mode override
    private bool    _waypointMode;
    private Vector3 _waypointWorldPos;

    private float _targetAlpha = 0f;
    private bool  _wasInView;

    // ── Properties ────────────────────────────────────────────────────────────

    public QuestMarkerBridge TargetBridge => _targetBridge;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        _mainCamera    = Camera.main;

        // Fallback nếu quên gán: dùng Image gốc trên object làm floatingIcon
        if (floatingIcon == null) floatingIcon = GetComponent<Image>();

        _canvasGroup.alpha = 0f;
    }

    /// <summary>Gọi bởi QuestMarkerManager khi spawn marker.</summary>
    public void InitializeFromBridge(QuestMarkerBridge bridge, RectTransform canvasParent)
    {
        if (bridge == null) { Debug.LogError("[QuestMarkerUI] bridge is null"); return; }

        _targetBridge = bridge;
        _canvasRect   = canvasParent;
        _parentCanvas = canvasParent.GetComponentInParent<Canvas>();
        _uiCamera     = (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _parentCanvas.worldCamera : null;

        _waypointMode = false;
        _targetAlpha  = 1f;
    }

    // ── Public API (gọi bởi WaypointNavigator) ───────────────────────────────

    public void SetWaypointTarget(Vector3 worldPos)
    {
        _waypointMode     = true;
        _waypointWorldPos = worldPos;
        _targetAlpha      = 1f;
    }

    public void SetNPCMode()
    {
        _waypointMode = false;
        _targetAlpha  = 1f;
    }

    public void SetActive(bool active)
    {
        _targetAlpha = active ? 1f : 0f;
        gameObject.SetActive(active);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        _canvasGroup.alpha = Mathf.MoveTowards(
            _canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);

        if (_targetBridge == null || _mainCamera == null || _canvasRect == null) return;

        Vector3 targetPos = _waypointMode ? _waypointWorldPos : _targetBridge.MarkerPosition;
        UpdateMarkerToTarget(targetPos);
        UpdateDistanceText(targetPos);
    }

    // ── Core: Floating (in-view) vs Edge Arrow (out-of-view) ─────────────────

    private void UpdateMarkerToTarget(Vector3 targetPos)
    {
        bool inView = ScreenEdgeMarkerCalculator.IsInViewFrustum(targetPos, _mainCamera);

        if (inView)
            ShowFloatingMarker(targetPos);
        else
            ShowEdgeArrow(targetPos);

        _wasInView = inView;
    }

    /// <summary>Marker lơ lửng world-space, không xoay, không clamp.</summary>
    private void ShowFloatingMarker(Vector3 targetPos)
    {
        SetIconMode(showFloating: true);

        Vector3 floatPos = targetPos + Vector3.up * floatHeight;
        Vector3 sp = _mainCamera.WorldToScreenPoint(floatPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, new Vector2(sp.x, sp.y), _uiCamera, out Vector2 canvasPos);

        _rectTransform.anchoredPosition = canvasPos;
        _rectTransform.localRotation    = Quaternion.identity;
    }

    /// <summary>Icon clamp mép màn hình, KHÔNG xoay — chỉ đổi vị trí (fallback).</summary>
    private void ShowEdgeArrow(Vector3 targetPos)
    {
        SetIconMode(showFloating: false);

        Vector3 playerPos = _targetBridge.PlayerTransform != null
            ? _targetBridge.PlayerTransform.position
            : _mainCamera.transform.position;

        Vector2 screenPos = ScreenEdgeMarkerCalculator.CalculateEdgeScreenPos(
            playerPos, targetPos, _mainCamera, edgePadding);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);

        _rectTransform.anchoredPosition = canvasPos;
        _rectTransform.localRotation    = Quaternion.identity;
    }

    /// <summary>Bật/tắt đúng icon tương ứng (nếu có 2 icon riêng biệt).</summary>
    private void SetIconMode(bool showFloating)
    {
        // Nếu chỉ dùng 1 icon chung (edgeArrowIcon == null), không cần đổi gì
        if (edgeArrowIcon == null) return;

        if (floatingIcon  != null) floatingIcon.enabled  = showFloating;
        edgeArrowIcon.enabled = !showFloating;
    }

    // ── Distance Text ─────────────────────────────────────────────────────────

    private void UpdateDistanceText(Vector3 targetPos)
    {
        if (distanceText == null) return;
        if (_targetBridge?.PlayerTransform == null) { distanceText.text = ""; return; }

        Vector3 delta = targetPos - _targetBridge.PlayerTransform.position;
        delta.y = 0f;
        float dist = delta.magnitude;

        distanceText.text = dist >= 10f ? $"{Mathf.RoundToInt(dist)}m" : $"{dist:F1}m";
    }
}