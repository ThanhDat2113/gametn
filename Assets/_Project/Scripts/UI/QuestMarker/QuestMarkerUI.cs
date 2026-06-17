using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Icon mũi tên duy nhất trên Canvas — hoạt động ở 2 chế độ:
///
///   CHẾ ĐỘ WAYPOINT (có WaypointPath):
///     Icon xoay chỉ về waypoint hiện tại → khi qua hết waypoint → chỉ thẳng NPC
///
///   CHẾ ĐỘ NPC TRỰC TIẾP (không có WaypointPath):
///     NPC trong màn hình  → icon đặt đúng vị trí trên đầu NPC, không xoay
///     NPC ngoài màn hình  → icon clamp về mép màn hình, xoay theo hướng NPC
///
/// WaypointNavigator sẽ gọi SetWaypointTarget() hoặc SetNPCTarget() để chuyển chế độ.
/// QuestMarkerManager không cần sửa — vẫn gọi InitializeFromBridge() như cũ.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class QuestMarkerUI : MonoBehaviour
{
    // ── Serialized ────────────────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private Image arrowIcon;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Arrow Settings")]
    [SerializeField] private float edgePadding = 20f;

    [Tooltip("Offset góc sprite khi angle = 0° (target ở phải player).\n" +
             "Sprite → RIGHT: 0° | LEFT: 180° | UP: -90° | DOWN: 90°")]
    [SerializeField] private float spriteAngleOffset = 180f;



    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 6f;

    // ── Private ───────────────────────────────────────────────────────────────

    private RectTransform _rectTransform;
    private CanvasGroup   _canvasGroup;
    private Canvas        _parentCanvas;
    private RectTransform _canvasRect;
    private Camera        _mainCamera;
    private Camera        _uiCamera;

    // Chế độ NPC trực tiếp (cũ)
    private QuestMarkerBridge _targetBridge;

    // Chế độ Waypoint (mới)
    private bool    _waypointMode;
    private Vector3 _waypointWorldPos;

    private float _targetAlpha = 0f;

    // ── Properties ────────────────────────────────────────────────────────────

    public QuestMarkerBridge TargetBridge => _targetBridge;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        _mainCamera    = Camera.main;

        if (arrowIcon == null) arrowIcon = GetComponent<Image>();

        _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Gọi bởi QuestMarkerManager khi spawn (chế độ NPC trực tiếp).
    /// </summary>
    public void InitializeFromBridge(QuestMarkerBridge bridge, RectTransform canvasParent)
    {
        if (bridge == null) { Debug.LogError("[QuestMarkerUI] bridge is null"); return; }

        _targetBridge = bridge;
        _canvasRect   = canvasParent;
        _parentCanvas = canvasParent.GetComponentInParent<Canvas>();
        _uiCamera     = (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _parentCanvas.worldCamera : null;

        // Mặc định chế độ NPC, WaypointNavigator sẽ override nếu có path
        _waypointMode = false;
        _targetAlpha  = 1f;
    }

    // ── Public API (gọi bởi WaypointNavigator) ───────────────────────────────

    /// <summary>
    /// Chuyển sang chế độ waypoint: icon xoay quanh tâm màn hình chỉ về worldPos.
    /// </summary>
    public void SetWaypointTarget(Vector3 worldPos)
    {
        _waypointMode    = true;
        _waypointWorldPos = worldPos;
        _targetAlpha     = 1f;
    }

    /// <summary>
    /// Trở về chế độ NPC trực tiếp (khi đã qua hết waypoint hoặc không có path).
    /// </summary>
    public void SetNPCMode()
    {
        _waypointMode = false;
        _targetAlpha  = 1f;
    }

    public void SetActive(bool active)
    {
        _targetAlpha = active ? 1f : 0f;
        if (!active) gameObject.SetActive(false);
        else         gameObject.SetActive(true);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Fade
        _canvasGroup.alpha = Mathf.MoveTowards(
            _canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);

        if (_mainCamera == null || _canvasRect == null) return;

        if (_waypointMode)
            UpdateWaypointMode();
        else
            UpdateNPCMode();

        UpdateDistanceText();
    }

    // ── Waypoint Mode ─────────────────────────────────────────────────────────

    private void UpdateWaypointMode()
    {
        if (_targetBridge == null) return;

        Vector3 playerPos = _targetBridge.PlayerTransform != null
            ? _targetBridge.PlayerTransform.position
            : _mainCamera.transform.position;

        // Clamp về mép màn hình (giống NPC ngoài màn hình) nhưng target là waypoint
        Vector2 screenPos = ScreenEdgeMarkerCalculator.CalculateEdgeScreenPos(
            playerPos, _waypointWorldPos, _mainCamera, edgePadding);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);

        float angle = ScreenEdgeMarkerCalculator.CalculateArrowRotation(
            playerPos, _waypointWorldPos, _mainCamera);

        _rectTransform.anchoredPosition = canvasPos;
        _rectTransform.localRotation =
            Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
    }

    // ── NPC Direct Mode (logic cũ) ────────────────────────────────────────────

    private void UpdateNPCMode()
    {
        if (_targetBridge == null) return;

        Vector3 targetPos = _targetBridge.MarkerPosition;
        Vector3 playerPos = _targetBridge.PlayerTransform != null
            ? _targetBridge.PlayerTransform.position
            : _mainCamera.transform.position;

        bool inView = ScreenEdgeMarkerCalculator.IsInViewFrustum(targetPos, _mainCamera);

        if (inView)
        {
            // NPC trong màn hình → đặt đúng vị trí trên đầu NPC
            Vector3 sp = _mainCamera.WorldToScreenPoint(targetPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, new Vector2(sp.x, sp.y), _uiCamera, out Vector2 canvasPos);

            _rectTransform.anchoredPosition = canvasPos;
            _rectTransform.localRotation    = Quaternion.identity;
        }
        else
        {
            // NPC ngoài màn hình → clamp về mép + xoay
            Vector2 screenPos = ScreenEdgeMarkerCalculator.CalculateEdgeScreenPos(
                playerPos, targetPos, _mainCamera, edgePadding);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);

            float angle = ScreenEdgeMarkerCalculator.CalculateArrowRotation(
                playerPos, targetPos, _mainCamera);

            _rectTransform.anchoredPosition = canvasPos;
            _rectTransform.localRotation    =
                Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
        }
    }

    // ── Distance Text ─────────────────────────────────────────────────────────

    private void UpdateDistanceText()
    {
        if (distanceText == null) return;
        if (_targetBridge?.PlayerTransform == null) { distanceText.text = ""; return; }

        Vector3 target = _waypointMode ? _waypointWorldPos : _targetBridge.MarkerPosition;
        Vector3 delta  = target - _targetBridge.PlayerTransform.position;
        delta.y = 0f;
        float dist = delta.magnitude;

        distanceText.text = dist >= 10f ? $"{Mathf.RoundToInt(dist)}m" : $"{dist:F1}m";
    }
}