using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Cấu hình minimap riêng cho từng map/scene (dùng cho StaticTexture mode).
/// Khi đổi map, MinimapController tự tìm entry khớp tên scene và áp dụng.
/// </summary>
[System.Serializable]
public struct MapMinimapData
{
    [Tooltip("Tên scene (phải khớp EXACT với tên file scene, không có đuôi .unity).")]
    public string sceneName;
    [Tooltip("Ảnh tĩnh minimap cho map này. Để trống nếu map này dùng RenderTextureCamera.")]
    public Texture2D staticTexture;
    [Tooltip("Kích thước vùng world mà staticTexture bao phủ (world units, hình vuông).")]
    public float worldSize;
    [Tooltip("Tâm world của staticTexture (thường là tâm terrain map này).")]
    public Vector3 worldCenter;
}

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

    public enum HideBehavior
    {
        DeactivateRoot,      // tắt hẳn GameObject UI minimap
        DisableUpdatesOnly   // ẩn UI (alpha=0) nhưng giữ active — camera/update vẫn tắt để đỡ tốn perf
    }

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

    [Header("Multi-Map Data (StaticTexture mode)")]
    [Tooltip("Danh sách snapshot riêng cho từng map. Khi đổi scene, hệ thống tự " +
             "tìm entry khớp tên scene hiện tại và áp dụng staticTexture/worldSize/worldCenter tương ứng. " +
             "Để trống nếu chỉ dùng RenderTextureCamera, hoặc nếu map nào không có trong " +
             "danh sách này sẽ giữ nguyên giá trị staticTexture phía trên.")]
    [SerializeField] private List<MapMinimapData> perMapData = new List<MapMinimapData>();

    [Header("UI References")]
    [Tooltip("RawImage hiển thị RenderTexture (cho mode RenderTextureCamera).")]
    [SerializeField] private RawImage minimapRawImage;
    [Tooltip("Image hiển thị mask hình dạng minimap.")]
    [SerializeField] private Image minimapMaskImage;
    [Tooltip("RectTransform chứa các marker NPC trên minimap (con của minimap, không xoay theo camera).")]
    [SerializeField] private RectTransform markerContainer;
    [Tooltip("Icon player ở giữa minimap (xoay theo hướng nhìn nếu rotateWithPlayer = false).")]
    [SerializeField] private RectTransform playerIcon;
    [Tooltip("Root GameObject của toàn bộ UI minimap (vd 'MinimapRoot'). Để trống sẽ tự dùng " +
             "GameObject cha gần nhất chứa minimapMaskImage, hoặc gameObject này nếu không tìm được.")]
    [SerializeField] private GameObject minimapUIRoot;

    [Header("Auto-Hide khi UI Panel mở")]
    [Tooltip("Kéo các panel UI vào đây (vd mainPanel, characterPanel, equipmentPanel của MapMenuManager). " +
             "Minimap tự ẩn khi BẤT KỲ panel nào trong list đang active, hiện lại khi TẤT CẢ đã đóng. " +
             "Không cần sửa script của các panel đó — MinimapController tự poll mỗi frame.")]
    [SerializeField] private GameObject[] uiPanelsToWatch;

    [Header("Auto-Hide trong Combat Scene")]
    [Tooltip("Bật để tự ẩn minimap khi vào scene combat.")]
    [SerializeField] private bool autoHideInCombat = true;
    [Tooltip("DeactivateRoot: tắt hẳn GameObject UI minimap.\n" +
             "DisableUpdatesOnly: ẩn UI (alpha=0) nhưng giữ active, tắt camera/update để đỡ tốn perf.")]
    [SerializeField] private HideBehavior hideBehavior = HideBehavior.DeactivateRoot;
    [Tooltip("Tên các scene được coi là combat scene (so khớp EXACT, không phân biệt hoa thường). " +
             "Khi scene này load (kể cả Additive), minimap tự ẩn.")]
    [SerializeField] private List<string> combatSceneNames = new List<string>();

    // ── Internal state ──────────────────────────────────────────────────────────
    private Camera _minimapCamera;
    private CanvasGroup _uiRootCanvasGroup;
    private bool _isHiddenByCombat = false;
    private bool _isHiddenByUI = false;
    private bool _externalCombatFlag = false;
    private bool _isHiddenByDialogue = false; // ✅ Ẩn khi dialogue đang mở

    // ── Multi-terrain support ─────────────────────────────────────────────────
    private Terrain[] _allTerrains;

    // ── Events ──────────────────────────────────────────────────────────────────
    public event System.Action<string> OnMapChanged;
    public event System.Action<bool> OnCombatVisibilityChanged;

    // ── Public properties ──────────────────────────────────────────────────────
    public Transform Player => player;
    public bool RotateWithPlayer => rotateWithPlayer;
    public RectTransform MarkerContainer => markerContainer;
    public DisplayMode CurrentDisplayMode => displayMode;
    public bool IsHiddenByCombat => _isHiddenByCombat;
    public float EffectiveWorldSize => mapScale * zoomMultiplier;

    // ── Static API ──────────────────────────────────────────────────────────────
    public static void NotifyCombatStateChanged(bool isInCombat)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[MinimapController] NotifyCombatStateChanged được gọi nhưng " +
                              "chưa có Instance — bỏ qua (minimap có thể chưa khởi tạo).");
            return;
        }
        Instance.SetExternalCombatFlag(isInCombat);
    }

    private void SetExternalCombatFlag(bool isInCombat)
    {
        _externalCombatFlag = isInCombat;
        RefreshCombatVisibility();
    }

    // ── Unity lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (terrain == null) terrain = Terrain.activeTerrain;
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        CacheAllTerrains();
        ResolveUIRoot();
        SetupDisplayMode();
        ApplyMaskShape();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void Start()
    {
        if (displayMode == DisplayMode.RenderTextureCamera)
            SetupMinimapCamera();

        RefreshCombatVisibility();
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // Cập nhật flag dialogue mỗi frame
        _isHiddenByDialogue = DialogueBubbleUI.IsDialogueActive;

        // Cập nhật flag UI panel
        CheckUIPanelVisibility();

        // Luôn áp dụng trạng thái hiển thị (ẩn/hiện) dựa trên các flag
        ApplyMinimapVisibility();

        // Nếu đang ẩn vì bất kỳ lý do gì, bỏ qua cập nhật camera
        if (_isHiddenByCombat || _isHiddenByUI || _isHiddenByDialogue)
            return;

        // Cập nhật camera và map
        if (displayMode == DisplayMode.RenderTextureCamera && _minimapCamera != null)
            UpdateCameraFollow();

        if (displayMode == DisplayMode.StaticTexture)
            UpdateStaticTextureUV();

        if (rotateWithPlayer)
            ApplyMapRotation();
        else
            ApplyPlayerIconRotation();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        if (Instance == this) Instance = null;
    }

    // ─── Scene / Map Change Handling ───────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[MinimapController] Scene loaded: '{scene.name}' (mode={mode}) → refresh minimap.");
        CacheAllTerrains();
        RefreshForMap(scene);
        RefreshCombatVisibility();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"[MinimapController] Scene unloaded: '{scene.name}'.");
        if (terrain != null && terrain.gameObject.scene == scene) terrain = null;
        if (player != null && player.gameObject.scene == scene) player = null;
        CacheAllTerrains();
        RefreshCombatVisibility();
    }

    private void RefreshForMap(Scene scene)
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                Debug.Log($"[MinimapController] Tìm thấy lại Player: '{player.name}'.");
            }
            else
            {
                Debug.LogWarning("[MinimapController] Không tìm thấy Player sau khi đổi map!");
            }
        }

        ApplyMapDataIfAvailable(scene.name);

        if (displayMode == DisplayMode.RenderTextureCamera && _minimapCamera != null)
            UpdateCameraOrthoSize();

        OnMapChanged?.Invoke(scene.name);
    }

    // ─── Combat Auto-Hide ─────────────────────────────────────────────────────

    private bool IsAnyCombatSceneLoaded()
    {
        if (combatSceneNames == null || combatSceneNames.Count == 0) return false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;

            for (int j = 0; j < combatSceneNames.Count; j++)
            {
                if (string.Equals(s.name, combatSceneNames[j], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private void RefreshCombatVisibility()
    {
        bool shouldHide = autoHideInCombat && (_externalCombatFlag || IsAnyCombatSceneLoaded());

        if (shouldHide != _isHiddenByCombat)
        {
            _isHiddenByCombat = shouldHide;
            ApplyMinimapVisibility();
            OnCombatVisibilityChanged?.Invoke(shouldHide);
            Debug.Log($"[MinimapController] Combat visibility → {(shouldHide ? "ẨN" : "HIỆN")} minimap.");
        }
    }

    private void CheckUIPanelVisibility()
    {
        if (uiPanelsToWatch == null || uiPanelsToWatch.Length == 0) return;

        bool anyPanelOpen = false;
        foreach (var panel in uiPanelsToWatch)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                anyPanelOpen = true;
                break;
            }
        }

        if (anyPanelOpen == _isHiddenByUI) return;
        _isHiddenByUI = anyPanelOpen;
        ApplyMinimapVisibility();
    }

    private void ApplyMinimapVisibility()
    {
        bool shouldHide = _isHiddenByCombat || _isHiddenByUI || _isHiddenByDialogue;

        // Chỉ thay đổi khi cần, nhưng vẫn set đúng trạng thái mỗi lần
        switch (hideBehavior)
        {
            case HideBehavior.DeactivateRoot:
                if (minimapUIRoot != null && minimapUIRoot.activeSelf == shouldHide)
                    minimapUIRoot.SetActive(!shouldHide);
                if (_minimapCamera != null && _minimapCamera.enabled == shouldHide)
                    _minimapCamera.enabled = !shouldHide;
                break;
            case HideBehavior.DisableUpdatesOnly:
                if (_uiRootCanvasGroup != null)
                {
                    if (_uiRootCanvasGroup.alpha == (shouldHide ? 0f : 1f)) return;
                    _uiRootCanvasGroup.alpha = shouldHide ? 0f : 1f;
                    _uiRootCanvasGroup.blocksRaycasts = !shouldHide;
                    _uiRootCanvasGroup.interactable = !shouldHide;
                }
                if (_minimapCamera != null && _minimapCamera.enabled == shouldHide)
                    _minimapCamera.enabled = !shouldHide;
                break;
        }
    }

    // ─── Terrain helpers ────────────────────────────────────────────────────────

    private void CacheAllTerrains()
    {
        _allTerrains = FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[MinimapController] Cached {_allTerrains.Length} terrains.");
    }

    private Terrain GetTerrainAtPosition(Vector3 worldPos)
    {
        if (terrain != null)
        {
            Bounds terrainBounds = terrain.terrainData.bounds;
            terrainBounds.center += terrain.transform.position;
            if (terrainBounds.Contains(worldPos))
                return terrain;
        }

        foreach (Terrain t in _allTerrains)
        {
            if (t == null) continue;
            Bounds bounds = t.terrainData.bounds;
            bounds.center += t.transform.position;
            if (bounds.Contains(worldPos))
                return t;
        }

        return _allTerrains.Length > 0 ? _allTerrains[0] : null;
    }

    private float GetTerrainHeightOrFallback(Vector3 worldPos)
    {
        Terrain currentTerrain = GetTerrainAtPosition(worldPos);
        if (currentTerrain != null)
        {
            return currentTerrain.SampleHeight(worldPos) + currentTerrain.transform.position.y;
        }
        // Fallback: nếu không tìm thấy terrain, dùng height = 0
        return 0f;
    }

    // ─── UI Setup ──────────────────────────────────────────────────────────────

    private void ResolveUIRoot()
    {
        if (minimapUIRoot == null && minimapMaskImage != null)
            minimapUIRoot = minimapMaskImage.transform.parent != null
                ? minimapMaskImage.transform.parent.gameObject
                : minimapMaskImage.gameObject;

        if (minimapUIRoot == null)
        {
            Debug.LogWarning("[MinimapController] Không xác định được minimapUIRoot — " +
                              "auto-hide combat sẽ không hoạt động đầy đủ. Gán thủ công trong Inspector.");
            return;
        }

        _uiRootCanvasGroup = minimapUIRoot.GetComponent<CanvasGroup>();
        if (_uiRootCanvasGroup == null)
            _uiRootCanvasGroup = minimapUIRoot.AddComponent<CanvasGroup>();
    }

    private void SetupDisplayMode()
    {
        bool useCamera = displayMode == DisplayMode.RenderTextureCamera;

        if (minimapRawImage != null)
            minimapRawImage.gameObject.SetActive(true);

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
        camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        UpdateCameraOrthoSize();
    }

    private void ApplyMaskShape()
    {
        if (minimapMaskImage == null) return;

        minimapMaskImage.sprite = maskShape == MaskShape.Circle ? circleMaskSprite : squareMaskSprite;

        Mask mask = minimapMaskImage.GetComponent<Mask>();
        if (mask == null) mask = minimapMaskImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
    }

    // ─── Update logic ──────────────────────────────────────────────────────────

    private void UpdateCameraFollow()
    {
        if (player == null || _minimapCamera == null) return;

        Vector3 pos = player.position;
        float terrainHeight = GetTerrainHeightOrFallback(pos);

        // ✅ Đảm bảo camera luôn ở trên mặt đất, ngay cả khi không có terrain
        float yPos = terrainHeight + cameraHeight;
        if (terrain == null && _allTerrains.Length == 0)
        {
            // Không có terrain nào, dùng y = 0 + cameraHeight
            yPos = cameraHeight;
        }
        pos.y = yPos;
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
        _minimapCamera.orthographicSize = EffectiveWorldSize * 0.5f;
    }

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

    // ─── Rotation ──────────────────────────────────────────────────────────────

    private void ApplyMapRotation()
    {
        Transform pivot = markerContainer != null ? markerContainer.parent : null;
        if (pivot != null)
            pivot.localRotation = Quaternion.Euler(0f, 0f, player.eulerAngles.y);

        if (playerIcon != null)
            playerIcon.localRotation = Quaternion.identity;
    }

    private void ApplyPlayerIconRotation()
    {
        if (playerIcon != null)
            playerIcon.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
    }

    // ─── World ↔ Minimap conversion ──────────────────────────────────────────

    public Vector2 WorldToMinimapPosition(Vector3 worldPos, out bool isWithinRange)
    {
        if (player == null || markerContainer == null)
        {
            isWithinRange = false;
            return Vector2.zero;
        }

        Vector3 rel = worldPos - player.position;
        Vector2 flat = new Vector2(rel.x, rel.z);

        float halfWorld = EffectiveWorldSize * 0.5f;
        float pixelsPerWorldUnit = (minimapUISize * 0.5f) / halfWorld;

        Vector2 mapPos = flat * pixelsPerWorldUnit;

        float halfUI = minimapUISize * 0.5f;
        isWithinRange = Mathf.Abs(mapPos.x) <= halfUI && Mathf.Abs(mapPos.y) <= halfUI;

        return mapPos;
    }

    public Vector2 ClampToMinimapEdge(Vector2 mapPos, float padding = 10f)
    {
        float halfUI = minimapUISize * 0.5f - padding;

        if (maskShape == MaskShape.Circle)
        {
            float dist = mapPos.magnitude;
            if (dist < 0.0001f) return Vector2.zero;
            if (dist <= halfUI) return mapPos;
            return mapPos.normalized * halfUI;
        }
        else
        {
            float clampedX = Mathf.Clamp(mapPos.x, -halfUI, halfUI);
            float clampedY = Mathf.Clamp(mapPos.y, -halfUI, halfUI);
            return new Vector2(clampedX, clampedY);
        }
    }

    // ─── Apply per-map data ────────────────────────────────────────────────────

    private void ApplyMapDataIfAvailable(string sceneName)
    {
        for (int i = 0; i < perMapData.Count; i++)
        {
            if (perMapData[i].sceneName == sceneName)
            {
                staticTexture = perMapData[i].staticTexture;
                staticTextureWorldSize = perMapData[i].worldSize;
                staticTextureWorldCenter = perMapData[i].worldCenter;

                if (displayMode == DisplayMode.StaticTexture && minimapRawImage != null)
                    minimapRawImage.texture = staticTexture;

                Debug.Log($"[MinimapController] Áp dụng map data cho scene '{sceneName}' " +
                          $"(worldSize={staticTextureWorldSize}, center={staticTextureWorldCenter}).");
                return;
            }
        }

        Debug.Log($"[MinimapController] Không có perMapData cho scene '{sceneName}' — " +
                  "giữ staticTexture/worldSize hiện tại (nếu đang dùng StaticTexture mode, hãy thêm entry).");
    }

    // ─── Editor tools ─────────────────────────────────────────────────────────

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