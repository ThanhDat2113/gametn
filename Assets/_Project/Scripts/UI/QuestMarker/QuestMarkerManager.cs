using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class QuestMarkerManager : MonoBehaviour
{
    public static QuestMarkerManager Instance { get; private set; }

    [Header("Prefab References")]
    [SerializeField] private QuestMarkerUI   markerPrefab;
    [SerializeField] private MinimapMarkerUI minimapMarkerPrefab;

    [Header("UI Container")]
    [Tooltip("Gán tay Canvas cụ thể trong Inspector. Để trống sẽ tự tìm — nhưng nên gán tay " +
             "để tránh tìm sai Canvas sau scene transition.")]
    [SerializeField] private RectTransform markerContainer;

    private readonly Dictionary<QuestMarkerBridge, QuestMarkerUI>   _activeMarkers        = new();
    private readonly Dictionary<QuestMarkerBridge, MinimapMarkerUI> _activeMinimapMarkers = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolveMarkerContainer();
    }

    private void Start()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestMarkerManager] QuestManager.Instance is NULL! Sẽ thử lại sau 0.5s...");
            Invoke(nameof(DelayedStart), 0.5f);
            return;
        }
        SubscribeAndEvaluate();
    }

    private void DelayedStart()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("[QuestMarkerManager] QuestManager.Instance vẫn NULL sau delay!");
            return;
        }
        SubscribeAndEvaluate();
    }

    private void SubscribeAndEvaluate()
    {
        QuestManager.Instance.OnStepChanged.AddListener(OnStepChanged);
        QuestManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);
        SceneManager.sceneLoaded += OnSceneLoaded;
        EvaluateCurrentStep();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStepChanged.RemoveListener(OnStepChanged);
            QuestManager.Instance.OnStepCompleted.RemoveListener(OnStepCompleted);
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[QuestMarkerManager] Scene loaded: '{scene.name}' — queuing re-evaluate.");

        // Re-resolve container sau scene load (canvas cũ có thể đã bị destroy)
        if (markerContainer == null)
            ResolveMarkerContainer();

        // FIX: Delay 1 frame để các QuestMarkerBridge trong scene mới kịp Awake()
        // trước khi FindObjectsByType chạy. Nếu gọi ngay thì sẽ tìm được 0 bridges.
        StartCoroutine(EvaluateNextFrame());
    }

    private IEnumerator EvaluateNextFrame()
    {
        yield return null;
        ClearAllMarkers();
        EvaluateCurrentStep();
    }

    // ── Quest Event Handlers ──────────────────────────────────────────────────

    private void OnStepChanged(QuestStep _)
    {
        ClearAllMarkers();
        EvaluateCurrentStep();
    }

    private void OnStepCompleted(QuestStep _)
    {
        ClearAllMarkers();
    }

    // ── Core Logic ────────────────────────────────────────────────────────────

    private void EvaluateCurrentStep()
    {
        var qm = QuestManager.Instance;
        if (qm == null || qm.CurrentQuest == null || qm.CurrentStep == null) return;

        string questId   = qm.CurrentQuest.questId;
        int    stepIndex = qm.CurrentStepIndex;

        Debug.Log($"[QuestMarkerManager] Looking for bridges: questId='{questId}' stepIndex={stepIndex}");

        var allBridges = FindObjectsByType<QuestMarkerBridge>(FindObjectsSortMode.None);
        Debug.Log($"[QuestMarkerManager] Found {allBridges.Length} bridges total in scene");

        foreach (var bridge in allBridges)
            Debug.Log($"[QuestMarkerManager] Bridge '{bridge.name}': questId='{bridge.QuestId}' stepIndex={bridge.StepIndex}");

        bool anyFound = false;
        foreach (var bridge in allBridges)
        {
            if (!bridge.MatchesCurrentStep(questId, stepIndex)) continue;
            SpawnMarker(bridge);
            anyFound = true;
        }

        if (!anyFound)
            Debug.LogWarning($"[QuestMarkerManager] Không tìm thấy bridge nào cho questId='{questId}' stepIndex={stepIndex}.");
    }

    private void SpawnMarker(QuestMarkerBridge bridge)
    {
        if (_activeMarkers.ContainsKey(bridge)) return;

        // FIX: Re-resolve container nếu bị null sau scene transition
        if (markerContainer == null) ResolveMarkerContainer();
        if (!IsReadyToSpawn()) return;

        // WorldSpaceRing: marker tự quản lý transform thật trong world (tự thêm
        // Canvas World Space cho chính nó), nên KHÔNG parent vào markerContainer (UI canvas).
        // ScreenOverlayRing: giữ nguyên hành vi cũ — marker là con của markerContainer.
        bool worldSpaceRing = PlayerMarkerRing.Instance != null &&
                               PlayerMarkerRing.Instance.Mode == PlayerMarkerRing.RingMode.WorldSpaceRing;

        QuestMarkerUI marker = worldSpaceRing
            ? Instantiate(markerPrefab)
            : Instantiate(markerPrefab, markerContainer);

        marker.InitializeFromBridge(bridge, markerContainer);
        _activeMarkers[bridge] = marker;

        if (minimapMarkerPrefab != null
            && MinimapController.Instance != null
            && MinimapController.Instance.MarkerContainer != null)
        {
            var mm = Instantiate(minimapMarkerPrefab, MinimapController.Instance.MarkerContainer);
            mm.InitializeFromBridge(bridge);
            _activeMinimapMarkers[bridge] = mm;
            Debug.Log($"[QuestMarkerManager] Minimap Marker ON → {bridge.name}");
        }

        Debug.Log($"[QuestMarkerManager] Marker ON → {bridge.name} (quest='{bridge.QuestId}' step={bridge.StepIndex})");
    }

    private void ClearAllMarkers()
    {
        foreach (var m in _activeMarkers.Values)        if (m != null) Destroy(m.gameObject);
        foreach (var m in _activeMinimapMarkers.Values) if (m != null) Destroy(m.gameObject);
        _activeMarkers.Clear();
        _activeMinimapMarkers.Clear();
        Debug.Log("[QuestMarkerManager] Cleared all markers.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ResolveMarkerContainer()
    {
        if (markerContainer != null) return;

        // Tìm Canvas trong scene thường (bỏ qua DontDestroyOnLoad scene)
        var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c.gameObject.scene.buildIndex == -1) continue; // skip DontDestroyOnLoad
            markerContainer = c.GetComponent<RectTransform>();
            Debug.Log($"[QuestMarkerManager] Auto-found Canvas: '{c.name}' in '{c.gameObject.scene.name}'");
            return;
        }
        Debug.LogWarning("[QuestMarkerManager] Canvas not found. Gán markerContainer trong Inspector.");
    }

    private bool IsReadyToSpawn()
    {
        if (markerPrefab == null)
        {
            Debug.LogError("[QuestMarkerManager] markerPrefab chưa được set.");
            return false;
        }

        // markerContainer chỉ bắt buộc ở ScreenOverlayRing (marker world-space tự quản lý transform).
        bool worldSpaceRing = PlayerMarkerRing.Instance != null &&
                               PlayerMarkerRing.Instance.Mode == PlayerMarkerRing.RingMode.WorldSpaceRing;
        if (!worldSpaceRing && markerContainer == null)
        {
            Debug.LogError("[QuestMarkerManager] markerContainer chưa được set (bắt buộc ở ScreenOverlayRing).");
            return false;
        }

        return true;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetAllMarkersActive(bool active)
    {
        foreach (var m in _activeMarkers.Values)        if (m != null) m.SetActive(active);
        foreach (var m in _activeMinimapMarkers.Values) if (m != null) m.SetActive(active);
    }

    public int ActiveMarkerCount        => _activeMarkers.Count;
    public int ActiveMinimapMarkerCount => _activeMinimapMarkers.Count;
}