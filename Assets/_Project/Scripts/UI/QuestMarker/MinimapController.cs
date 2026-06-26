using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hệ thống Minimap chiếu từ Terrain.
///
/// 2 MODE HIỂN THỊ (chọn trong Inspector, không cần sửa code):
///   • RenderTextureCamera — camera top-down render real-time vào RenderTexture.
///     Theo player, luôn cập nhật (thấy NPC di chuyển, thời tiết, v.v.)
///   • StaticTexture — dùng 1 ảnh tĩnh chụp sẵn từ trên xuống (đỡ tốn performance,
///     không cập nhật real-time). Camera vẫn cần để CHỤP ảnh này 1 lần qua nút
///     "Capture Static Snapshot" trong Inspector (context menu), sau đó chuyển
///     sang mode StaticTexture để dùng ảnh đã chụp.
///
/// MASK SHAPE: Circle hoặc Square — set qua enum, áp dụng Image.sprite mask
/// hoặc dùng UI Mask component tùy bạn (xem hướng dẫn ở MinimapMaskShape).
///
/// TỶ LỆ (mapScale): "world units mỗi đơn vị minimap khi zoom = 1".
///   mapScale = 300 (default) nghĩa là: camera orthographic được tính sao cho
///   300 world units world tương ứng với kích thước hiển thị của minimap.
///   Số CÀNG LỚN → minimap zoom xa hơn (thấy nhiều bản đồ hơn).
///   Số CÀNG NHỎ → minimap zoom gần hơn (thấy ít, chi tiết hơn).
///   Điều chỉnh trực tiếp trong Play Mode để thấy hiệu ứng ngay (camera cập nhật mỗi frame).
/// </summary>
[DisallowMultipleComponent]
public class MinimapController : MonoBehaviour
{
    public enum DisplayMode { RenderTextureCamera, StaticTexture }
    public enum MaskShape    { Circle, Square }

    // ── Singleton (để marker system truy cập world→minimap conversion) ───────
    public static MinimapController Instance { get; private set; }

    [Header("Display Mode")]
    [Tooltip("RenderTextureCamera: camera real-time chiếu từ trên xuống.\n" +
             "StaticTexture: dùng ảnh tĩnh đã chụp sẵn (xem context menu 'Capture Static Snapshot').")]
    [SerializeField] private DisplayMode displayMode = DisplayMode.RenderTextureCamera;

    [Header("Mask Shape")]
    [Tooltip("Hình dạng khung minimap hiển thị trên UI.")]
    [SerializeField] private MaskShape maskShape = MaskShape.Circle;
    [Tooltip("Sprite mask hình tròn (gán sprite UI tròn, vd built-in UI/Skin/Knob hoặc sprite riêng).")]
    [SerializeField] private Sprite circleMaskSprite;
    [Tooltip("Sprite mask hình vuông (để trống = dùng full rect, không cần mask riêng).")]
    [SerializeField] private Sprite squareMaskSprite;

    [Header("Rotation")]
    [Tooltip("True: minimap xoay theo hướng nhìn player (player luôn hướng lên trên).\n" +
             "False: minimap cố định theo hướng Bắc world space.")]
    [SerializeField] private bool rotateWithPlayer = false;

    [Header("Player & Terrain")]
    [SerializeField] private Transform player;
    [Tooltip("Terrain để chiếu minimap. Để trống sẽ tự tìm Terrain.activeTerrain.")]
    [SerializeField] private Terrain terrain;
    [Tooltip("Độ cao camera minimap phía trên terrain (world units).")]
    [SerializeField] private float cameraHeight = 100f;

    [Header("Scale (Tỷ lệ)")]
    [Tooltip("World units mỗi đơn vị minimap khi zoomMultiplier = 1.\n" +
             "Số lớn hơn → zoom xa (thấy nhiều map hơn). Số nhỏ hơn → zoom gần.")]
    [SerializeField] private float mapScale = 300f;
    [Tooltip("Hệ số zoom thêm, nhân với mapScale. Dùng để zoom in/out runtime (vd lăn chuột).")]
    [SerializeField] private float zoomMultiplier = 1f;
    [Tooltip("Kích thước minimap hiển thị trên UI (pixel), dùng để tính orthographic size đúng tỷ lệ.")]
    [SerializeField] private float minimapUISize = 200f;

    [Header("Render Texture Mode")]
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private int renderTextureResolution = 512;
    [SerializeField] private LayerMask cameraCullingMask = ~0;

    [Header("Static Texture Mode")]
    [SerializeField] private Texture2D staticTexture;
    [Tooltip("Kích thước vùng world mà staticTexture bao phủ (world units, hình vuông).")]
    [SerializeField] private float staticTextureWorldSize = 1000f;
    [Tooltip("Tâm world của staticTexture (thường là tâm terrain).")]
    [SerializeField] private Vector3 staticTextureWorldCenter = Vector3.zero;

    [Header("UI References")]
    [Tooltip("RawImage hiển thị RenderTexture (cho mode RenderTextureCamera).")]
    [SerializeField] private RawImage minimapRawImage;
    [Tooltip("Image hiển thị mask hình dạng minimap.")]
    [SerializeField] private Image minimapMaskImage;
    [Tooltip("RectTransform chứa các marker NPC trên minimap (con của minimap, không xoay theo camera).")]
    [SerializeField] private RectTransform markerContainer;
    [Tooltip("Icon player ở giữa minimap (xoay theo hướng nhìn nếu rotateWithPlayer = false).")]
    [SerializeField] private RectTransform playerIcon;

    private Camera _minimapCamera;

    public Transform Player => player;
    public bool RotateWithPlayer => rotateWithPlayer;
    public RectTransform MarkerContainer => markerContainer;
    public DisplayMode CurrentDisplayMode => displayMode;

    /// <summary>World units hiển thị theo chiều cao/rộng minimap, đã áp dụng zoom.</summary>
    public float EffectiveWorldSize => mapScale * zoomMultiplier;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (terrain == null) terrain = Terrain.activeTerrain;
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        SetupDisplayMode();
        ApplyMaskShape();
    }

    private void Start()
    {
        if (displayMode == DisplayMode.RenderTextureCamera)
            SetupMinimapCamera();
    }

    private void LateUpdate()
    {
        if (player == null) return;

        if (displayMode == DisplayMode.RenderTextureCamera && _minimapCamera != null)
            UpdateCameraFollow();

        if (displayMode == DisplayMode.StaticTexture)
            UpdateStaticTextureUV();

        if (rotateWithPlayer)
            ApplyMapRotation();
        else
            ApplyPlayerIconRotation();
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    private void SetupDisplayMode()
    {
        bool useCamera = displayMode == DisplayMode.RenderTextureCamera;

        if (minimapRawImage != null)
            minimapRawImage.gameObject.SetActive(true); // RawImage dùng cho cả 2 mode (texture khác nhau)

        if (useCamera)
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(renderTextureResolution, renderTextureResolution, 16);
                renderTexture.name = "Minimap_RT_Runtime";
            }
            if (minimapRawImage != null) minimapRawImage.texture = renderTexture;
        }
        else
        {
            if (minimapRawImage != null && staticTexture != null)
                minimapRawImage.texture = staticTexture;
            else if (staticTexture == null)
                Debug.LogWarning("[MinimapController] StaticTexture mode nhưng chưa gán staticTexture!");
        }
    }

    private void SetupMinimapCamera()
    {
        GameObject camGO = new GameObject("MinimapCamera_Runtime");
        camGO.transform.SetParent(transform);
        _minimapCamera = camGO.AddComponent<Camera>();
        _minimapCamera.orthographic = true;
        _minimapCamera.targetTexture = renderTexture;
        _minimapCamera.cullingMask = cameraCullingMask;
        _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        _minimapCamera.backgroundColor = Color.black;
        _minimapCamera.nearClipPlane = 0.3f;
        _minimapCamera.farClipPlane = cameraHeight + 50f;

        // Nhìn thẳng xuống terrain
        camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        UpdateCameraOrthoSize();
    }

    private void UpdateCameraFollow()
    {
        Vector3 pos = player.position;
        pos.y = GetTerrainHeightOrFallback(pos) + cameraHeight;
        _minimapCamera.transform.position = pos;

        if (rotateWithPlayer)
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
        else
            _minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        UpdateCameraOrthoSize();
    }

    private void UpdateCameraOrthoSize()
    {
        if (_minimapCamera == null) return;
        // orthographicSize = nửa chiều cao world hiển thị
        _minimapCamera.orthographicSize = EffectiveWorldSize * 0.5f;
    }

    private float GetTerrainHeightOrFallback(Vector3 worldPos)
    {
        if (terrain != null) return terrain.SampleHeight(worldPos) + terrain.transform.position.y;
        return worldPos.y;
    }

    // ── Static Texture mode: pan ảnh tĩnh theo player bằng UV offset ─────────

    private void UpdateStaticTextureUV()
    {
        if (minimapRawImage == null || staticTexture == null || staticTextureWorldSize <= 0f) return;

        Vector3 rel = player.position - staticTextureWorldCenter;
        float uvX = 0.5f + (rel.x / staticTextureWorldSize);
        float uvY = 0.5f + (rel.z / staticTextureWorldSize);

        float halfUV = (EffectiveWorldSize / staticTextureWorldSize) * 0.5f;

        minimapRawImage.uvRect = new Rect(
            uvX - halfUV, uvY - halfUV, halfUV * 2f, halfUV * 2f);
    }

    // ── Mask shape ───────────────────────────────────────────────────────────

    private void ApplyMaskShape()
    {
        if (minimapMaskImage == null) return;

        minimapMaskImage.sprite = maskShape == MaskShape.Circle ? circleMaskSprite : squareMaskSprite;

        Mask mask = minimapMaskImage.GetComponent<Mask>();
        if (mask == null) mask = minimapMaskImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
    }

    // ── Rotation ─────────────────────────────────────────────────────────────

    private void ApplyMapRotation()
    {
        // markerContainer và minimapRawImage phải là CON CỦA CÙNG MỘT PIVOT để xoay
        // đồng bộ tuyệt đối. Auto Setup đặt cả 2 làm con trực tiếp của MinimapMask,
        // nên ta xoay chính MinimapMask (cha chung) — không xoay từng cái riêng lẻ.
        Transform pivot = markerContainer != null ? markerContainer.parent : null;
        if (pivot != null)
            pivot.localRotation = Quaternion.Euler(0f, 0f, player.eulerAngles.y);

        if (playerIcon != null)
            playerIcon.localRotation = Quaternion.identity; // player icon luôn thẳng lên
    }

    private void ApplyPlayerIconRotation()
    {
        // Map cố định hướng Bắc → chỉ xoay icon player theo hướng nhìn
        if (playerIcon != null)
            playerIcon.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
    }

    // ── World → Minimap conversion (dùng bởi MinimapMarkerUI) ─────────────────

    /// <summary>
    /// Convert world position → anchored position trong markerContainer.
    /// Trả về (position, isWithinRange) — isWithinRange = false nếu world point
    /// nằm ngoài phạm vi minimap hiển thị hiện tại.
    ///
    /// LƯU Ý: vị trí trả về KHÔNG tự xoay theo player, vì rotateWithPlayer được
    /// xử lý bằng cách xoay markerContainer.localRotation (xem ApplyMapRotation).
    /// Marker là con của markerContainer nên tự động xoay theo khi container xoay —
    /// tính rotation 2 lần (cả ở đây và ở container) sẽ làm marker xoay gấp đôi.
    /// </summary>
    public Vector2 WorldToMinimapPosition(Vector3 worldPos, out bool isWithinRange)
    {
        if (player == null || markerContainer == null)
        {
            isWithinRange = false;
            return Vector2.zero;
        }

        Vector3 rel = worldPos - player.position;
        Vector2 flat = new Vector2(rel.x, rel.z); // top-down: x→x, z→y(map lên)

        float halfWorld = EffectiveWorldSize * 0.5f;
        float pixelsPerWorldUnit = (minimapUISize * 0.5f) / halfWorld;

        Vector2 mapPos = flat * pixelsPerWorldUnit;

        float halfUI = minimapUISize * 0.5f;
        isWithinRange = Mathf.Abs(mapPos.x) <= halfUI && Mathf.Abs(mapPos.y) <= halfUI;

        return mapPos;
    }

    /// <summary>Clamp một vị trí minimap (đã tính bởi WorldToMinimapPosition) về mép minimap.</summary>
    public Vector2 ClampToMinimapEdge(Vector2 mapPos, float padding = 10f)
    {
        float halfUI = minimapUISize * 0.5f - padding;

        if (maskShape == MaskShape.Circle)
        {
            float dist = mapPos.magnitude;
            if (dist < 0.0001f) return Vector2.zero;
            if (dist <= halfUI) return mapPos; // đã trong phạm vi, không cần clamp
            return mapPos.normalized * halfUI;
        }
        else
        {
            float clampedX = Mathf.Clamp(mapPos.x, -halfUI, halfUI);
            float clampedY = Mathf.Clamp(mapPos.y, -halfUI, halfUI);
            return new Vector2(clampedX, clampedY);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Capture Static Snapshot")]
    private void CaptureStaticSnapshot()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("[MinimapController] Không tìm thấy Terrain để chụp snapshot.");
            return;
        }

        GameObject camGO = new GameObject("TempSnapshotCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;

        Vector3 terrainCenter = terrain.transform.position +
            new Vector3(terrain.terrainData.size.x * 0.5f, 0f, terrain.terrainData.size.z * 0.5f);
        float worldSize = Mathf.Max(terrain.terrainData.size.x, terrain.terrainData.size.z);

        cam.transform.position = terrainCenter + Vector3.up * (terrain.terrainData.size.y + 50f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.orthographicSize = worldSize * 0.5f;
        cam.cullingMask = cameraCullingMask;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.farClipPlane = terrain.terrainData.size.y + 100f;

        int res = renderTextureResolution;
        RenderTexture tempRT = new RenderTexture(res, res, 16);
        cam.targetTexture = tempRT;
        cam.Render();

        RenderTexture.active = tempRT;
        Texture2D snapshot = new Texture2D(res, res, TextureFormat.RGB24, false);
        snapshot.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        snapshot.Apply();
        RenderTexture.active = null;

        cam.targetTexture = null;
        DestroyImmediate(camGO);
        tempRT.Release();

        staticTexture = snapshot;
        staticTextureWorldSize = worldSize;
        staticTextureWorldCenter = terrainCenter;

        Debug.Log($"[MinimapController] Đã chụp snapshot {res}x{res}, worldSize={worldSize}. " +
                   "Lưu ý: Texture2D này chỉ tồn tại runtime trong session Editor — " +
                   "hãy Save as Asset (Assets > Create > ... hoặc kéo vào project) để dùng lại sau.");
    }
#endif
}
