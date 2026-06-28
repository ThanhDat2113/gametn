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

    [Header("Auto-Hide trong Combat Scene")]
    [Tooltip("Bật để tự ẩn minimap khi vào scene combat.")]
    [SerializeField] private bool autoHideInCombat = true;
    [Tooltip("DeactivateRoot: tắt hẳn GameObject UI minimap.\n" +
             "DisableUpdatesOnly: ẩn UI (alpha=0) nhưng giữ active, tắt camera/update để đỡ tốn perf.")]
    [SerializeField] private HideBehavior hideBehavior = HideBehavior.DeactivateRoot;
    [Tooltip("Tên các scene được coi là combat scene (so khớp EXACT, không phân biệt hoa thường). " +
             "Khi scene này load (kể cả Additive), minimap tự ẩn.")]
    [SerializeField] private List<string> combatSceneNames = new List<string>();

    private Camera _minimapCamera;
    private CanvasGroup _uiRootCanvasGroup; // dùng cho DisableUpdatesOnly
    private bool _isHiddenByCombat = false;
    private bool _externalCombatFlag = false; // set bởi NotifyCombatStateChanged từ bên ngoài

    /// <summary>Fire sau khi minimap đã refresh xong cho map mới (terrain/player/data đã cập nhật).</summary>
    public event System.Action<string> OnMapChanged;

    /// <summary>Fire khi minimap bị ẩn/hiện do combat (true = vừa ẩn, false = vừa hiện lại).</summary>
    public event System.Action<bool> OnCombatVisibilityChanged;

    public Transform Player => player;
    public bool RotateWithPlayer => rotateWithPlayer;
    public RectTransform MarkerContainer => markerContainer;
    public DisplayMode CurrentDisplayMode => displayMode;
    public bool IsHiddenByCombat => _isHiddenByCombat;

    /// <summary>World units hiển thị theo chiều cao/rộng minimap, đã áp dụng zoom.</summary>
    public float EffectiveWorldSize => mapScale * zoomMultiplier;

    /// <summary>
    /// Gọi từ bất kỳ script nào (FormationManager, SceneLoaderManager, SceneTransitionManager, v.v.)
    /// để báo hiệu trạng thái combat thay đổi, KHÔNG cần biết tên scene hay chờ scene load event.
    /// An toàn gọi khi Instance chưa tồn tại — sẽ bị bỏ qua, không lỗi.
    ///
    /// Ví dụ dùng trong FormationManager trước khi gọi SceneManager.LoadScene("CombatScene"):
    ///   MinimapController.NotifyCombatStateChanged(true);
    /// Và khi rời combat (về lại map chính):
    ///   MinimapController.NotifyCombatStateChanged(false);
    /// </summary>
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

        ResolveUIRoot();
        SetupDisplayMode();
        ApplyMaskShape();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    /// <summary>
    /// Tìm GameObject root của UI minimap để Activate/Deactivate khi vào combat.
    /// Ưu tiên field minimapUIRoot; nếu trống, leo lên cha của minimapMaskImage.
    /// </summary>
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

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (displayMode == DisplayMode.RenderTextureCamera)
            SetupMinimapCamera();

        // Game có thể khởi động sẵn trong combat scene (vd test trực tiếp scene combat
        // trong Editor) — check ngay từ đầu, không chỉ chờ OnSceneLoaded.
        RefreshCombatVisibility();
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // Khi đang ẩn do combat, bỏ qua toàn bộ update (camera follow, rotation, v.v.)
        // để đỡ tốn performance — không có ý nghĩa cập nhật UI không hiển thị.
        if (_isHiddenByCombat) return;

        if (displayMode == DisplayMode.RenderTextureCamera && _minimapCamera != null)
            UpdateCameraFollow();

        if (displayMode == DisplayMode.StaticTexture)
            UpdateStaticTextureUV();

        if (rotateWithPlayer)
            ApplyMapRotation();
        else
            ApplyPlayerIconRotation();
    }

    // ── Scene / Map Change Handling ───────────────────────────────────────────

    /// <summary>
    /// Fire khi BẤT KỲ scene nào load xong — kể cả LoadScene thường, LoadSceneAsync,
    /// hay Additive (qua SceneLoaderManager/SceneTransitionManager). Không cần biết
    /// project dùng cách load nào, event này luôn fire đúng lúc.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[MinimapController] Scene loaded: '{scene.name}' (mode={mode}) → refresh minimap.");
        RefreshForMap(scene);
        RefreshCombatVisibility();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"[MinimapController] Scene unloaded: '{scene.name}'.");

        // Nếu Terrain/Player hiện tại thuộc scene vừa unload, object đó đã chết
        // theo Unity (destroy cùng scene) → null ra, đợi OnSceneLoaded của scene mới gán lại.
        if (terrain != null && terrain.gameObject.scene == scene) terrain = null;
        if (player != null && player.gameObject.scene == scene) player = null;

        // Quan trọng: nếu combat scene (Additive) vừa unload, phải re-check visibility
        // ngay — không chờ scene khác load, vì có thể không có scene mới nào load tiếp theo
        // (chỉ đơn giản là rời combat scene, quay lại scene chính đã loaded từ trước).
        RefreshCombatVisibility();
    }

    /// <summary>
    /// Tìm lại Terrain + Player ĐÚNG TRONG scene vừa load (không lấy nhầm từ scene
    /// khác đang tồn tại song song do Additive), rồi áp dụng map data tương ứng.
    /// </summary>
    private void RefreshForMap(Scene scene)
    {
        Terrain foundTerrain = FindTerrainInScene(scene);
        if (foundTerrain != null)
        {
            terrain = foundTerrain;
            Debug.Log($"[MinimapController] Tìm thấy Terrain '{terrain.name}' trong scene '{scene.name}'.");
        }
        else if (terrain == null)
        {
            // Scene này không có terrain riêng (vd UI scene, scene phụ) — không log lỗi,
            // có thể terrain vẫn còn từ scene khác đang active song song (Additive).
            Debug.Log($"[MinimapController] Scene '{scene.name}' không có Terrain — giữ terrain hiện tại (nếu có).");
        }

        // Player: nếu player cũ đã null (do scene cũ unload) hoặc chưa từng có, tìm lại.
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

        // Camera RT cần re-parent tâm theo terrain mới (vị trí Y sample lại đúng địa hình mới)
        if (displayMode == DisplayMode.RenderTextureCamera && _minimapCamera != null)
            UpdateCameraOrthoSize();

        OnMapChanged?.Invoke(scene.name);
    }

    // ── Combat Auto-Hide ─────────────────────────────────────────────────────

    /// <summary>
    /// Quét TẤT CẢ scene đang loaded (an toàn với Additive — combat scene có thể
    /// tồn tại song song với scene chính) để xem có scene nào khớp combatSceneNames không.
    /// </summary>
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

    /// <summary>
    /// Tính lại trạng thái ẩn/hiện dựa trên: (a) tên scene combat đang loaded HOẶC
    /// (b) cờ ngoài được set qua NotifyCombatStateChanged. Chỉ cần 1 trong 2 đúng → ẩn.
    /// Gọi sau mỗi lần scene load/unload và mỗi lần NotifyCombatStateChanged được gọi.
    /// </summary>
    private void RefreshCombatVisibility()
    {
        if (!autoHideInCombat)
        {
            if (_isHiddenByCombat) SetCombatHidden(false);
            return;
        }

        bool shouldHide = _externalCombatFlag || IsAnyCombatSceneLoaded();

        if (shouldHide != _isHiddenByCombat)
            SetCombatHidden(shouldHide);
    }

    private void SetCombatHidden(bool hide)
    {
        _isHiddenByCombat = hide;

        Debug.Log($"[MinimapController] Combat visibility → {(hide ? "ẨN minimap" : "HIỆN minimap")} " +
                  $"(behavior={hideBehavior}).");

        switch (hideBehavior)
        {
            case HideBehavior.DeactivateRoot:
                if (minimapUIRoot != null) minimapUIRoot.SetActive(!hide);
                if (_minimapCamera != null) _minimapCamera.enabled = !hide;
                break;

            case HideBehavior.DisableUpdatesOnly:
                if (_uiRootCanvasGroup != null)
                {
                    _uiRootCanvasGroup.alpha = hide ? 0f : 1f;
                    _uiRootCanvasGroup.blocksRaycasts = !hide;
                    _uiRootCanvasGroup.interactable = !hide;
                }
                // Tắt camera render để đỡ tốn perf dù UI vẫn active
                if (_minimapCamera != null) _minimapCamera.enabled = !hide;
                break;
        }

        OnCombatVisibilityChanged?.Invoke(hide);
    }

    /// <summary>Quét root objects của ĐÚNG scene này để tìm Terrain — an toàn với Additive.</summary>
    private Terrain FindTerrainInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Terrain t = root.GetComponentInChildren<Terrain>(true);
            if (t != null) return t;
        }
        return null;
    }

    /// <summary>Nếu có entry trong perMapData khớp tên scene, áp dụng staticTexture/worldSize/worldCenter.</summary>
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