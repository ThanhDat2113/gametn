using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class QuestMarkerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image arrowIcon;

    [Header("Settings")]
    [Tooltip("Padding tính từ mép màn hình (pixel).")]
    [SerializeField] private float edgePadding = 60f;

    [Header("UI Avoidance")]
    [Tooltip("Tự động tránh các UI panel đang active trên màn hình (minimap, HUD, quest text...).")]
    [SerializeField] private bool avoidUIElements = true;

    [Tooltip("Các tag của Canvas/RectTransform cần BỎ QUA khi scan (vd: canvas chứa marker chính nó).")]
    [SerializeField] private string[] ignoreCanvasTags = { "UICanvas", "MarkerCanvas" };

    [Tooltip("Số lần thử đẩy marker ra khỏi vùng bị chặn trước khi bỏ qua.")]
    [SerializeField] private int maxAvoidIterations = 5;

    [Tooltip("Padding thêm xung quanh mỗi UI element khi tránh (pixel màn hình).")]
    [SerializeField] private float uiAvoidPadding = 10f;

    [Tooltip("Số frame giữa mỗi lần re-scan UI elements (0 = scan mỗi frame, cao hơn = ít tốn hơn).")]
    [SerializeField] private int scanInterval = 10;

    [Header("Rotation")]
    [SerializeField] private bool enableRotation = true;
    [Tooltip("Sprite chỉ RIGHT(→): 0°  |  UP(↑): -90°  |  LEFT(←): 180°  |  DOWN(↓): 90°")]
    [SerializeField] private float spriteAngleOffset = 0f;

    private RectTransform _rectTransform;
    private CanvasGroup   _canvasGroup;

    private QuestMarkerBridge _targetBridge;
    private Camera            _mainCamera;
    private RectTransform     _canvasRect;
    private Camera            _uiCamera;

    // Cache danh sách rect UI cần tránh (screen space pixel)
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

        Canvas canvas = canvasParent.GetComponentInParent<Canvas>();
        _uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;
    }

    private void Update()
    {
        if (_mainCamera == null || !_mainCamera.gameObject.activeInHierarchy)
            _mainCamera = Camera.main;

        if (_targetBridge == null || _mainCamera == null || _canvasRect == null) return;

        // Ẩn khi dialogue đang chạy
        bool dialogueActive = DialogueBubbleUI.Instance != null && DialogueBubbleUI.Instance.IsShowing;
        _canvasGroup.alpha = dialogueActive ? 0f : 1f;
        if (dialogueActive) return;

        // Re-scan UI elements theo interval để tránh scan mỗi frame
        _frameCounter++;
        if (avoidUIElements && _frameCounter >= scanInterval)
        {
            _frameCounter = 0;
            RefreshAvoidRects();
        }

        Vector3 targetPos = _targetBridge.MarkerPosition;
        bool inView = ScreenEdgeMarkerCalculator.IsInViewFrustum(targetPos, _mainCamera);

        if (inView)
        {
            Vector3 sp = _mainCamera.WorldToScreenPoint(targetPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, new Vector2(sp.x, sp.y), _uiCamera, out Vector2 canvasPos);

            _rectTransform.anchoredPosition = canvasPos;
            if (enableRotation) _rectTransform.localRotation = Quaternion.identity;
        }
        else
        {
            float dynamicPadding = edgePadding + GetHalfIconSize();
            Vector2 screenPos = ScreenEdgeMarkerCalculator.CalculateEdgeScreenPos(
                targetPos, _mainCamera, dynamicPadding);

            // Tránh các UI đang active
            if (avoidUIElements && _avoidRects.Count > 0)
                screenPos = PushOutOfUIRects(screenPos, dynamicPadding);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _uiCamera, out Vector2 canvasPos);

            _rectTransform.anchoredPosition = canvasPos;

            if (enableRotation)
            {
                float angle = ScreenEdgeMarkerCalculator.CalculateArrowRotation(targetPos, _mainCamera);
                _rectTransform.localRotation = Quaternion.AngleAxis(angle + spriteAngleOffset, Vector3.forward);
            }
        }
    }

    // ── UI Avoidance ──────────────────────────────────────────────────────────

    /// <summary>
    /// Scan tất cả Graphic active trên Canvas, convert sang screen-space rect,
    /// lưu vào _avoidRects để dùng trong PushOutOfUIRects().
    /// Bỏ qua canvas chứa marker chính nó (tránh self-avoidance).
    /// </summary>
    private void RefreshAvoidRects()
    {
        _avoidRects.Clear();

        // Canvas gốc chứa marker — bỏ qua toàn bộ children của canvas này
        Canvas selfCanvas = _canvasRect != null ? _canvasRect.GetComponentInParent<Canvas>() : null;

        var allGraphics = FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        foreach (var g in allGraphics)
        {
            if (!g.gameObject.activeInHierarchy) continue;
            if (g.canvasRenderer.cull) continue; // bị cull (ngoài viewport)

            // Bỏ qua nếu thuộc canvas của marker
            Canvas parentCanvas = g.canvas;
            if (parentCanvas != null && parentCanvas == selfCanvas) continue;

            // Bỏ qua nếu canvas có tag trong ignoreCanvasTags
            if (parentCanvas != null && IsIgnoredCanvas(parentCanvas)) continue;

            // Bỏ qua các element quá nhỏ (dưới 20x20 pixel) — text ký tự đơn, v.v.
            Rect screenRect = GetScreenRect(g.rectTransform);
            if (screenRect.width < 20f || screenRect.height < 20f) continue;

            // Expand thêm uiAvoidPadding
            screenRect.x      -= uiAvoidPadding;
            screenRect.y      -= uiAvoidPadding;
            screenRect.width  += uiAvoidPadding * 2f;
            screenRect.height += uiAvoidPadding * 2f;

            _avoidRects.Add(screenRect);
        }
    }

    /// <summary>
    /// Đẩy screenPos ra khỏi tất cả avoidRects bằng cách lặp lại tối đa
    /// maxAvoidIterations lần. Mỗi lần tìm rect đang chứa điểm và đẩy ra
    /// theo cạnh gần nhất dọc theo viền màn hình.
    /// </summary>
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

                // Tính khoảng cách đến từng cạnh của rect
                float dLeft   = screenPos.x - rect.xMin;
                float dRight  = rect.xMax   - screenPos.x;
                float dBottom = screenPos.y - rect.yMin;
                float dTop    = rect.yMax   - screenPos.y;
                float minDist = Mathf.Min(dLeft, dRight, dBottom, dTop);

                // Đẩy ra theo cạnh gần nhất, nhưng clamp về viền màn hình
                if (minDist == dBottom)
                    screenPos.y = Mathf.Max(padding, rect.yMin - padding);
                else if (minDist == dTop)
                    screenPos.y = Mathf.Min(sh - padding, rect.yMax + padding);
                else if (minDist == dLeft)
                    screenPos.x = Mathf.Max(padding, rect.xMin - padding);
                else
                    screenPos.x = Mathf.Min(sw - padding, rect.xMax + padding);

                moved = true;
                break; // re-check từ đầu sau mỗi lần đẩy
            }
            if (!moved) break; // không còn overlap → xong
        }

        return screenPos;
    }

    private bool IsIgnoredCanvas(Canvas canvas)
    {
        if (ignoreCanvasTags == null) return false;
        foreach (var tag in ignoreCanvasTags)
        {
            if (!string.IsNullOrEmpty(tag) && canvas.CompareTag(tag)) return true;
        }
        return false;
    }

    /// <summary>Convert RectTransform sang Rect trong screen space (pixel).</summary>
    private static Rect GetScreenRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // GetWorldCorners trả về world position — với Screen Space Overlay canvas
        // thì world position = screen position trực tiếp.
        // Với Camera canvas cần project qua camera, nhưng UI element thường là SS Overlay.
        float xMin = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float xMax = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float yMin = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float yMax = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private float GetHalfIconSize()
    {
        if (_rectTransform == null) return 0f;
        Canvas canvas = _canvasRect != null ? _canvasRect.GetComponentInParent<Canvas>() : null;
        float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
        return Mathf.Max(_rectTransform.sizeDelta.x, _rectTransform.sizeDelta.y) * 0.5f * canvasScale;
    }

    public void SetActive(bool active) => gameObject.SetActive(active);
}