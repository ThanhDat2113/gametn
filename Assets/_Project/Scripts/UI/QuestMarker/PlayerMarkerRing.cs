using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton quản lý "vòng tròn la bàn" quanh chân player — nơi các QuestMarkerUI
/// định vị xung quanh thay vì bám mép màn hình như hệ thống cũ.
///
/// 2 MODE (chọn qua Inspector, không cần sửa code — field `ringMode`):
///   • WorldSpaceRing    — vòng tròn vẽ thật trên mặt đất dưới chân player (3D,
///     đúng phối cảnh khi camera xoay). Marker cũng là object world-space, tự
///     billboard xoay mặt về camera.
///   • ScreenOverlayRing — vòng tròn giả lập bằng UI 2D, canh tại vị trí player
///     chiếu lên màn hình. Marker vẫn là UI con của markerContainer như cũ.
///
/// Hướng đặt marker LUÔN tính theo CAMERA-RELATIVE (dùng chung
/// ScreenEdgeMarkerCalculator.GetScreenDirection) — giống hệt logic mép màn hình
/// cũ, chỉ khác bán kính cố định quanh player thay vì bám full màn hình.
///
/// SETUP:
///   1. Gắn component này vào 1 GameObject trong scene (vd chung với QuestMarkerManager).
///   2. Chọn ringMode trong Inspector.
///   3. (Tuỳ chọn) Gán ringVisualWorld / ringVisualScreen nếu muốn vẽ vòng tròn
///      hiển thị thật (decal mặt đất / sprite UI hình khuyên). Nếu để trống,
///      hệ thống vẫn hoạt động (marker vẫn xoay quanh vị trí đúng) — chỉ là
///      không có đường viền vòng tròn hiển thị.
///   4. Để trống `player` sẽ tự lấy từ MinimapController.Instance.Player.
/// </summary>
[DisallowMultipleComponent]
public class PlayerMarkerRing : MonoBehaviour
{
    public enum RingMode { WorldSpaceRing, ScreenOverlayRing }

    public static PlayerMarkerRing Instance { get; private set; }

    [Header("Mode")]
    [Tooltip("WorldSpaceRing: vòng tròn thật trên mặt đất.\n" +
             "ScreenOverlayRing: vòng tròn UI 2D canh giữa player trên màn hình.")]
    [SerializeField] private RingMode ringMode = RingMode.WorldSpaceRing;

    [Header("Player")]
    [Tooltip("Để trống sẽ tự lấy từ MinimapController.Instance.Player.")]
    [SerializeField] private Transform player;

    [Header("World Space Ring")]
    [Tooltip("Bán kính vòng tròn quanh chân player (world units).")]
    [SerializeField] private float ringRadiusWorld = 1.8f;
    [Tooltip("Độ cao vòng tròn/marker so với chân player (world units) — tránh z-fighting với mặt đất.")]
    [SerializeField] private float ringHeightOffset = 0.05f;
    [Tooltip("Transform của decal/mesh vòng tròn hiển thị trên mặt đất. Để trống nếu không cần vẽ " +
             "viền tròn, chỉ dùng ring để định vị marker.")]
    [SerializeField] private Transform ringVisualWorld;

    [Header("Screen Overlay Ring")]
    [Tooltip("Bán kính vòng tròn trên màn hình (pixel).")]
    [SerializeField] private float ringRadiusScreen = 150f;
    [Tooltip("RectTransform của vòng tròn UI (sprite hình khuyên) hiển thị quanh player trên màn hình. " +
             "Để trống nếu không cần vẽ viền tròn.")]
    [SerializeField] private RectTransform ringVisualScreen;
    [Tooltip("RectTransform của Canvas UI chứa ringVisualScreen — dùng để convert world→local khi " +
             "định vị vòng. Thường chính là markerContainer của QuestMarkerManager.")]
    [SerializeField] private RectTransform screenCanvasRect;
    [SerializeField] private Camera uiCamera;

    private Camera _mainCamera;

    public RingMode Mode           => ringMode;
    public float    RingRadiusWorld  => ringRadiusWorld;
    public float    RingRadiusScreen => ringRadiusScreen;
    public float    RingHeightOffset => ringHeightOffset;
    public Transform Player        => player;
    public Camera    UICamera      => uiCamera;
    public RectTransform ScreenCanvasRect => screenCanvasRect;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player == null && MinimapController.Instance != null)
            player = MinimapController.Instance.Player;

        if (player == null)
            Debug.LogWarning("[PlayerMarkerRing] Chưa gán player và không tìm được qua MinimapController.Instance.Player.");
    }

    private void Update()
    {
        if (_mainCamera == null || !_mainCamera.gameObject.activeInHierarchy)
            _mainCamera = Camera.main;

        if (player == null || _mainCamera == null) return;

        if (ringMode == RingMode.WorldSpaceRing)
            UpdateWorldRingVisual();
        else
            UpdateScreenRingVisual();
    }

    // ── API dùng bởi QuestMarkerUI ───────────────────────────────────────────

    /// <summary>Tâm vòng tròn world-space (chân player + offset chiều cao).</summary>
    public Vector3 GetWorldRingCenter()
    {
        if (player == null) return Vector3.zero;
        return player.position + Vector3.up * ringHeightOffset;
    }

    /// <summary>Vị trí world-space trên vòng tròn ứng với hướng (camera-relative) tới targetWorldPos.</summary>
    public Vector3 GetWorldRingPosition(Vector3 targetWorldPos, Camera cam)
    {
        return ScreenEdgeMarkerCalculator.CalculateRingWorldPos(
            GetWorldRingCenter(), targetWorldPos, cam, ringRadiusWorld);
    }

    /// <summary>Tâm vòng tròn trên màn hình (vị trí player chiếu qua camera).</summary>
    public Vector3 GetScreenRingCenter(Camera cam)
    {
        if (player == null || cam == null) return Vector3.zero;
        return cam.WorldToScreenPoint(player.position);
    }

    /// <summary>Vị trí screen-space trên vòng tròn ứng với hướng tới targetWorldPos.</summary>
    public Vector2 GetScreenRingPosition(Vector3 targetWorldPos, Camera cam)
    {
        Vector3 center = GetScreenRingCenter(cam);
        Vector2 dir    = ScreenEdgeMarkerCalculator.GetScreenDirection(targetWorldPos, cam);
        return new Vector2(center.x, center.y) + dir * ringRadiusScreen;
    }

    // ── Ring visuals (tuỳ chọn) ──────────────────────────────────────────────

    private void UpdateWorldRingVisual()
    {
        if (ringVisualWorld == null) return;
        ringVisualWorld.position = GetWorldRingCenter();
        // Nằm phẳng trên mặt đất — chỉnh lại nếu mesh/sprite ring của bạn có trục local khác.
        ringVisualWorld.rotation = Quaternion.Euler(90f, 0f, 0f);
        ringVisualWorld.localScale = Vector3.one * (ringRadiusWorld * 2f);
    }

    private void UpdateScreenRingVisual()
    {
        if (ringVisualScreen == null || screenCanvasRect == null) return;

        Vector3 screenCenter = GetScreenRingCenter(_mainCamera);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                screenCanvasRect, screenCenter, uiCamera, out Vector2 localPos))
        {
            ringVisualScreen.anchoredPosition = localPos;
        }
        ringVisualScreen.sizeDelta = Vector2.one * (ringRadiusScreen * 2f);
    }
}
