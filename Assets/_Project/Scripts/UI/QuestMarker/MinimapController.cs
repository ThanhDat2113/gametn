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

[DisallowMultipleComponent]
public class MinimapController : MonoBehaviour
{
    public enum DisplayMode { RenderTextureCamera, StaticTexture }
    public enum MaskShape    { Circle, Square }

    public enum HideBehavior
    {
        DeactivateRoot,
        DisableUpdatesOnly
    }

    // ── Singleton ──────────────────────────────────────────────────
    public static MinimapController Instance { get; private set; }

    [Header("Display Mode")]
    [SerializeField] private DisplayMode displayMode = DisplayMode.RenderTextureCamera;

    [Header("Mask Shape")]
    [SerializeField] private MaskShape maskShape = MaskShape.Circle;
    [SerializeField] private Sprite circleMaskSprite;
    [SerializeField] private Sprite squareMaskSprite;

    [Header("Rotation")]
    [SerializeField] private bool rotateWithPlayer = false;

    [Header("Player & Terrain")]
    [SerializeField] private Transform player;
    [SerializeField] private Terrain terrain;
    [SerializeField] private float cameraHeight = 100f;

    [Header("Scale (Tỷ lệ)")]
    [SerializeField] private float mapScale = 300f;
    [SerializeField] private float zoomMultiplier = 1f;
    [SerializeField] private float minimapUISize = 200f;

    [Header("Render Texture Mode")]
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private int renderTextureResolution = 512;
    [SerializeField] private LayerMask cameraCullingMask = ~0;

    [Header("Static Texture Mode")]
    [SerializeField] private Texture2D staticTexture;
    [SerializeField] private float staticTextureWorldSize = 1000f;
    [SerializeField] private Vector3 staticTextureWorldCenter = Vector3.zero;

    [Header("Multi-Map Data (StaticTexture mode)")]
    [SerializeField] private List<MapMinimapData> perMapData = new List<MapMinimapData>();

    [Header("UI References")]
    [SerializeField] private RawImage minimapRawImage;
    [SerializeField] private Image minimapMaskImage;
    [SerializeField] private RectTransform markerContainer;
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private GameObject minimapUIRoot;

    [Header("Auto-Hide khi UI Panel mở")]
    [SerializeField] private GameObject[] uiPanelsToWatch;

    [Header("Auto-Hide trong Combat Scene")]
    [SerializeField] private bool autoHideInCombat = true;
    [SerializeField] private HideBehavior hideBehavior = HideBehavior.DeactivateRoot;
    [SerializeField] private List<string> combatSceneNames = new List<string>();

    // ── Internal state ────────────────────────────────────────────
    private Camera _minimapCamera;
    private CanvasGroup _uiRootCanvasGroup;
    private bool _isHiddenByCombat = false;
    private bool _isHiddenByUI = false;
    private bool _externalCombatFlag = false;
    private bool _isHiddenByDialogue = false;

    // ── Multi-terrain support ────────────────────────────────────
    private Terrain[] _allTerrains;
    private Terrain _currentTerrain;

    // ── Events & Properties ──────────────────────────────────────
    public event System.Action<string> OnMapChanged;
    public event System.Action<bool> OnCombatVisibilityChanged;

    public Transform Player => player;
    public bool RotateWithPlayer => rotateWithPlayer;
    public RectTransform MarkerContainer => markerContainer;
    public DisplayMode CurrentDisplayMode => displayMode;
    public bool IsHiddenByCombat => _isHiddenByCombat;
    public float EffectiveWorldSize => mapScale * zoomMultiplier;

    // ── Static API ─────────────────────────────────────────────────
    public static void NotifyCombatStateChanged(bool isInCombat)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[MinimapController] NotifyCombatStateChanged được gọi nhưng " +
                              "chưa có Instance — bỏ qua.");
            return;
        }
        Instance.SetExternalCombatFlag(isInCombat);
    }

    private void SetExternalCombatFlag(bool isInCombat)
    {
        _externalCombatFlag = isInCombat;
        RefreshCombatVisibility();
    }

    // ─── Unity Lifecycle ──────────────────────────────────────────

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
        // ✅ Đảm bảo minimap hiển thị lúc đầu
        ApplyMinimapVisibility();
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // ✅ Cập nhật flag dialogue
        _isHiddenByDialogue = DialogueBubbleUI.IsDialogueActive;
        CheckUIPanelVisibility();

        // ✅ LUÔN áp dụng trạng thái ẩn/hiện (fix lỗi không hiện lại sau cutscene)
        ApplyMinimapVisibility();

        // Nếu đang ẩn vì bất kỳ lý do gì, bỏ qua cập nhật camera
        if (_isHiddenByCombat || _isHiddenByUI || _isHiddenByDialogue)
            return;

        // Cập nhật camera, UV, rotation...
        if (displayMode == DisplayMode.RenderTextureCamera && _minimapCamera != null)
        {
            UpdateTerrainForCurrentPosition();
            UpdateCameraFollow();
        }

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

    // ─── Scene / Map Change ───────────────────────────────────────

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

    // ─── Combat / UI Auto-Hide ────────────────────────────────────

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
    }

    // ─── Apply Visibility (GỌI MỖI FRAME) ─────────────────────────

    private void ApplyMinimapVisibility()
    {
        bool shouldHide = _isHiddenByCombat || _isHiddenByUI || _isHiddenByDialogue;

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
                    float targetAlpha = shouldHide ? 0f : 1f;
                    if (_uiRootCanvasGroup.alpha != targetAlpha)
                    {
                        _uiRootCanvasGroup.alpha = targetAlpha;
                        _uiRootCanvasGroup.blocksRaycasts = !shouldHide;
                        _uiRootCanvasGroup.interactable = !shouldHide;
                    }
                }
                if (_minimapCamera != null && _minimapCamera.enabled == shouldHide)
                    _minimapCamera.enabled = !shouldHide;
                break;
        }
    }

    // ─── Terrain Helpers ──────────────────────────────────────────

    private void CacheAllTerrains()
    {
        _allTerrains = FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _currentTerrain = null;
        Debug.Log($"[MinimapController] Cached {_allTerrains.Length} terrains.");
    }

    private Terrain GetTerrainAtPosition(Vector3 worldPos)
    {
        foreach (Terrain t in _allTerrains)
        {
            if (t == null) continue;
            Bounds bounds = t.terrainData.bounds;
            bounds.center += t.transform.position;
            bounds.Expand(0.5f);
            if (bounds.Contains(worldPos))
                return t;
        }
        return _allTerrains.Length > 0 ? _allTerrains[0] : null;
    }

    private void UpdateTerrainForCurrentPosition()
    {
        if (player == null) return;
        Terrain found = GetTerrainAtPosition(player.position);
        if (found != null && found != _currentTerrain)
        {
            _currentTerrain = found;
            Debug.Log($"[MinimapController] Terrain changed to: {found.name}");
        }
    }

    private float GetTerrainHeight(Vector3 worldPos)
    {
        if (_currentTerrain != null)
            return _currentTerrain.SampleHeight(worldPos) + _currentTerrain.transform.position.y;

        UpdateTerrainForCurrentPosition();
        if (_currentTerrain != null)
            return _currentTerrain.SampleHeight(worldPos) + _currentTerrain.transform.position.y;

        return 0f;
    }

    // ─── UI Setup ─────────────────────────────────────────────────

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

    // ─── Update Logic ─────────────────────────────────────────────

    private void UpdateCameraFollow()
    {
        if (player == null || _minimapCamera == null) return;

        Vector3 pos = player.position;
        float terrainHeight = GetTerrainHeight(pos);
        pos.y = terrainHeight + cameraHeight;
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

        uvX = Mathf.Clamp(uvX, 0f, 1f);
        uvY = Mathf.Clamp(uvY, 0f, 1f);

        float halfUV = (EffectiveWorldSize / staticTextureWorldSize) * 0.5f;
        halfUV = Mathf.Clamp(halfUV, 0f, 0.5f);

        minimapRawImage.uvRect = new Rect(
            uvX - halfUV, uvY - halfUV, halfUV * 2f, halfUV * 2f);
    }

    // ─── Rotation ─────────────────────────────────────────────────

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

    // ─── World ↔ Minimap ──────────────────────────────────────────

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

    // ─── Apply per-map data ───────────────────────────────────────

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
                {
                    minimapRawImage.texture = staticTexture;
                    minimapRawImage.uvRect = new Rect(0, 0, 1, 1);
                }

                Debug.Log($"[MinimapController] Áp dụng map data cho scene '{sceneName}' " +
                          $"(worldSize={staticTextureWorldSize}, center={staticTextureWorldCenter})");
                return;
            }
        }

        Debug.Log($"[MinimapController] Không có perMapData cho scene '{sceneName}' — " +
                  "giữ staticTexture/worldSize hiện tại.");
    }

    // ─── Editor tools ─────────────────────────────────────────────

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