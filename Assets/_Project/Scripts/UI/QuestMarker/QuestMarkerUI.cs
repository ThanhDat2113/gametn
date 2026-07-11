using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Marker quest — hybrid giữa 2 hành vi:
///   • NPC ĐANG trong tầm nhìn camera → marker bám thẳng lên đầu NPC (giống bản gốc).
///   • NPC NGOÀI tầm nhìn → marker rơi về vòng tròn "la bàn" quanh chân player,
///     thay cho việc bám mép màn hình của bản cũ.
///
/// Đọc mode hiện tại từ PlayerMarkerRing.Instance mỗi frame để biết dùng vòng tròn
/// world-space hay screen-overlay khi NPC ngoài tầm nhìn:
///   • ScreenOverlayRing — UI marker con của markerContainer, vòng tròn UI 2D quanh
///     vị trí player-trên-màn-hình.
///   • WorldSpaceRing — marker tự thêm 1 Canvas (World Space), đặt transform thật
///     trong world trên vòng tròn quanh chân player, luôn billboard về phía camera.
///
/// Tắt `showAboveTargetWhenInView` nếu muốn marker LUÔN bám vòng tròn, kể cả khi
/// NPC đang hiện rõ trên màn hình.
///
/// Hướng đặt marker trên vòng tròn luôn tính theo CAMERA-RELATIVE
/// (ScreenEdgeMarkerCalculator.GetScreenDirection/GetWorldFlatDirection).
///
/// LƯU Ý: cần có đúng 1 PlayerMarkerRing trong scene, nếu không marker sẽ
/// không tự cập nhật vị trí (Update() return sớm).
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class QuestMarkerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image arrowIcon;

    [Header("UI Avoidance (chỉ áp dụng khi ở ScreenOverlayRing)")]
    [Tooltip("Tự động tránh các UI panel đang active trên màn hình (minimap, HUD, quest text...).")]
    [SerializeField] private bool avoidUIElements = true;
    [Tooltip("Các tag của Canvas cần BỎ QUA khi scan (vd: canvas chứa marker chính nó).")]
    [SerializeField] private string[] ignoreCanvasTags = { "UICanvas", "MarkerCanvas" };
    [Tooltip("Số lần thử đẩy marker ra khỏi vùng bị chặn trước khi bỏ qua.")]
    [SerializeField] private int maxAvoidIterations = 5;
    [Tooltip("Padding thêm xung quanh mỗi UI element khi tránh (pixel màn hình).")]
    [SerializeField] private float uiAvoidPadding = 10f;
    [Tooltip("Số frame giữa mỗi lần re-scan UI elements (0 = scan mỗi frame).")]
    [SerializeField] private int scanInterval = 10;

    [Header("Rotation")]
    [SerializeField] private bool enableRotation = true;
    [Tooltip("Sprite chỉ RIGHT(→): 0°  |  UP(↑): -90°  |  LEFT(←): 180°  |  DOWN(↓): 90°")]
    [SerializeField] private float spriteAngleOffset = 0f;

    [Header("Above-Target (khi NPC đang trong tầm nhìn camera)")]
    [Tooltip("Bật: khi NPC nằm trong view frustum của camera, marker hiện THẲNG PHÍA TRÊN ĐẦU NPC " +
             "(giống bản gốc), thay vì bám vòng tròn quanh player. Tắt: marker luôn bám vòng tròn, " +
             "kể cả khi NPC đang hiện rõ trên màn hình.")]
    [SerializeField] private bool showAboveTargetWhenInView = true;
    [Tooltip("Độ cao cộng thêm phía trên MarkerPosition của bridge (world units) khi hiện trên đầu NPC.")]
    [SerializeField] private float aboveTargetHeightOffset = 0.3f;

    [Header("World Space Ring")]
    [Tooltip("Scale RectTransform khi Canvas tự tạo ở mode WorldSpaceRing " +
             "(kích thước UI px → world units). Chỉnh cho icon không quá to/nhỏ ngoài world.")]
    [SerializeField] private float worldCanvasScale = 0.01f;
    [Tooltip("Sorting order cho Canvas world-space tự tạo, đảm bảo marker vẽ đè lên terrain/props.")]
    [SerializeField] private int worldCanvasSortingOrder = 10;

    private RectTransform _rectTransform;
    private CanvasGroup   _canvasGroup;
    private RectTransform _canvasRect;   // dùng ở ScreenOverlayRing
    private Camera        _mainCamera;
    private Camera        _uiCamera;
    private Canvas        _worldCanvas;  // tự thêm khi ở WorldSpaceRing

    private QuestMarkerBridge _targetBridge;
    private readonly List<Rect> _avoidRects = new List<Rect>();
    private int _frameCounter = 0;

    public QuestMarkerBridge TargetBridge => _targetBridge;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        if (arrowIcon == null) arrowIcon = GetComponent<Image>();
    }

    public void InitializeFromBridge(QuestMarkerBridge bridge, RectTransform canvasParent)
    {
        if (bridge == null) { Debug.LogError("[QuestMarkerUI] bridge is null"); return; }
        _targetBridge = bridge;
        _canvasRect   = canvasParent;

        if (IsWorldSpaceRingMode())
        {
            SetupWorldCanvas();
        }
        else if (canvasParent != null)
        {
            Canvas canvas = canvasParent.GetComponentInParent<Canvas>();
            _uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;
        }
    }

    private bool IsWorldSpaceRingMode() =>
        PlayerMarkerRing.Instance != null &&
        PlayerMarkerRing.Instance.Mode == PlayerMarkerRing.RingMode.WorldSpaceRing;

    /// <summary>Tự thêm Canvas (World Space) cho chính marker này khi ở mode world-space ring,
    /// để nó có thể render như 1 object độc lập trong world thay vì phải là con của UI canvas.</summary>
    private void SetupWorldCanvas()
    {
        _worldCanvas = GetComponent<Canvas>();
        if (_worldCanvas == null) _worldCanvas = gameObject.AddComponent<Canvas>();

        _worldCanvas.renderMode      = RenderMode.WorldSpace;
        _worldCanvas.overrideSorting = true;
        _worldCanvas.sortingOrder    = worldCanvasSortingOrder;

        _rectTransform.localScale = Vector3.one * worldCanvasScale;
    }

    private void Update()
    {
        if (_mainCamera == null || !_mainCamera.gameObject.activeInHierarchy)
            _mainCamera = Camera.main;

        if (_targetBridge == null || _mainCamera == null) return;

        var ring = PlayerMarkerRing.Instance;
        if (ring == null) return; // cần PlayerMarkerRing trong scene để hệ thống hoạt động

        if (_worldCanvas != null) _worldCanvas.worldCamera = _mainCamera;

        // Ẩn khi dialogue đang chạy
        bool dialogueActive = DialogueBubbleUI.Instance != null && DialogueBubbleUI.Instance.IsShowing;
        _canvasGroup.alpha = dialogueActive ? 0f : 1f;
        if (dialogueActive) return;

        Vector3 targetPos = _targetBridge.MarkerPosition;

        if (ring.Mode == PlayerMarkerRing.RingMode.WorldSpaceRing)
            UpdateWorldSpacePosition(ring, targetPos);
        else
            UpdateScreenOverlayPosition(ring, targetPos);
    }

    // ── WorldSpaceRing ────────────────────────────────────────────────────────

    private void UpdateWorldSpacePosition(PlayerMarkerRing ring, Vector3 targetPos)
    {
        Vector3 headWorldPos = targetPos + Vector3.up * aboveTargetHeightOffset;

        bool tryInView = showAboveTargetWhenInView &&
                          ScreenEdgeMarkerCalculator.IsInViewFrustum(headWorldPos, _mainCamera);

        bool placedInView = false;
        if (tryInView)
        {
            if (IsValid(headWorldPos))
            {
                transform.position = headWorldPos;
                // Billboard đúng cho camera isometric: copy thẳng rotation của camera
                // (không dùng LookRotation vì bị gimbal lock khi camera pitch ~45-60°)
                transform.rotation = _mainCamera.transform.rotation;
                placedInView = true;
            }
        }

        if (!placedInView)
        {
            // NPC ngoài tầm nhìn (hoặc in-view bị NaN) → rơi về vòng tròn quanh chân player.
            Vector3 ringPos = ring.GetWorldRingPosition(targetPos, _mainCamera);
            if (!IsValid(ringPos)) return;

            transform.position = ringPos;

            // Billboard: copy camera rotation làm base (tránh gimbal lock isometric),
            // sau đó roll thêm để mũi tên chỉ đúng hướng NPC.
            transform.rotation = _mainCamera.transform.rotation;

            if (enableRotation)
            {
                float angle = ScreenEdgeMarkerCalculator.CalculateArrowRotation(targetPos, _mainCamera);
                // spriteAngleOffset bù theo hướng vẽ gốc của sprite (RIGHT=0, UP=-90, v.v.)
                transform.Rotate(Vector3.forward, angle + spriteAngleOffset, Space.Self);
            }
        }
    }

    // ── ScreenOverlayRing ─────────────────────────────────────────────────────

    private void UpdateScreenOverlayPosition(PlayerMarkerRing ring, Vector3 targetPos)
    {
        if (_canvasRect == null) return;

        bool inView = showAboveTargetWhenInView &&
                      ScreenEdgeMarkerCalculator.IsInViewFrustum(targetPos, _mainCamera);

        if (inView)
        {
            // NPC đang hiện trên màn hình → marker bám thẳng lên đầu NPC, không dùng vòng tròn.
            Vector3 headWorldPos = targetPos + Vector3.up * aboveTargetHeightOffset;
            Vector3 sp = _mainCamera.WorldToScreenPoint(headWorldPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, new Vector2(sp.x, sp.y), _uiCamera, out Vector2 headCanvasPos);
            _rectTransform.anchoredPosition = headCanvasPos;

            if (enableRotation) _rectTransform.localRotation = Quaternion.identity;
            return;
        }

        // NPC ngoài tầm nhìn → rơi về vòng tròn quanh player-trên-màn-hình.
        _frameCounter++;
        if (avoidUIElements && _frameCounter >= scanInterval)
        {
            _frameCounter = 0;
            RefreshAvoidRects();
        }

        Vector2 screenPos = ring.GetScreenRingPosition(targetPos, _mainCamera);

        if (avoidUIElements && _avoidRects.Count > 0)
            screenPos = PushOutOfUIRects(screenPos, uiAvoidPadding);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);
        _rectTransform.anchoredPosition = canvasPos;

        if (enableRotation)
        {
            float angle = ScreenEdgeMarkerCalculator.CalculateArrowRotation(targetPos, _mainCamera);
            _rectTransform.localRotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
        }
    }

    // ── UI Avoidance (giữ nguyên từ bản cũ, chỉ áp dụng ở ScreenOverlayRing) ────

    private void RefreshAvoidRects()
    {
        _avoidRects.Clear();
        Canvas selfCanvas = _canvasRect != null ? _canvasRect.GetComponentInParent<Canvas>() : null;

        var allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        foreach (var g in allGraphics)
        {
            if (!g.gameObject.activeInHierarchy) continue;
            if (g.canvasRenderer.cull) continue;

            Canvas parentCanvas = g.canvas;
            if (parentCanvas != null && parentCanvas == selfCanvas) continue;
            if (parentCanvas != null && IsIgnoredCanvas(parentCanvas)) continue;

            Rect screenRect = GetScreenRect(g.rectTransform);
            if (screenRect.width < 20f || screenRect.height < 20f) continue;

            screenRect.x      -= uiAvoidPadding;
            screenRect.y      -= uiAvoidPadding;
            screenRect.width  += uiAvoidPadding * 2f;
            screenRect.height += uiAvoidPadding * 2f;

            _avoidRects.Add(screenRect);
        }
    }

    private Vector2 PushOutOfUIRects(Vector2 screenPos, float padding)
    {
        float sw = _mainCamera.pixelWidth;
        float sh = _mainCamera.pixelHeight;

        for (int iter = 0; iter < maxAvoidIterations; iter++)
        {
            bool moved = false;
            foreach (var rect in _avoidRects)
            {
                if (!rect.Contains(screenPos)) continue;

                float dLeft   = screenPos.x - rect.xMin;
                float dRight  = rect.xMax   - screenPos.x;
                float dBottom = screenPos.y - rect.yMin;
                float dTop    = rect.yMax   - screenPos.y;
                float minDist = Mathf.Min(dLeft, dRight, dBottom, dTop);

                if (minDist == dBottom)      screenPos.y = Mathf.Max(padding, rect.yMin - padding);
                else if (minDist == dTop)    screenPos.y = Mathf.Min(sh - padding, rect.yMax + padding);
                else if (minDist == dLeft)   screenPos.x = Mathf.Max(padding, rect.xMin - padding);
                else                         screenPos.x = Mathf.Min(sw - padding, rect.xMax + padding);

                moved = true;
                break;
            }
            if (!moved) break;
        }
        return screenPos;
    }

    private bool IsIgnoredCanvas(Canvas canvas)
    {
        if (ignoreCanvasTags == null) return false;
        foreach (var tag in ignoreCanvasTags)
            if (!string.IsNullOrEmpty(tag) && canvas.CompareTag(tag)) return true;
        return false;
    }

    private static Rect GetScreenRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float xMin = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float xMax = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float yMin = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float yMax = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    public void SetActive(bool active) => gameObject.SetActive(active);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsValid(Vector3 v) =>
        !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)
        && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
}