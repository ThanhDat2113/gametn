using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Singleton manager quản lý quest marker UI.
///
/// LUỒNG:
///   1. Lắng nghe QuestManager.OnStepChanged.
///   2. Lấy questId của quest đang chạy + currentStepIndex.
///   3. Tìm tất cả QuestMarkerBridge trong scene khớp (questId + stepIndex).
///   4. Spawn marker cho từng bridge tìm được.
///   5. Khi OnStepCompleted → xóa toàn bộ marker.
/// </summary>
[DisallowMultipleComponent]
public class QuestMarkerManager : MonoBehaviour
{
    public static QuestMarkerManager Instance { get; private set; }

    [Header("Prefab References")]
    [SerializeField] private QuestMarkerUI    markerPrefab;
    [SerializeField] private MinimapMarkerUI  minimapMarkerPrefab;

    [Header("UI Container")]
    [Tooltip("RectTransform của Canvas chứa marker. Để trống sẽ tự tìm Canvas.")]
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
        // Chờ QuestManager sẵn sàng (có thể chưa kịp Start quest)
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestMarkerManager] QuestManager.Instance is NULL! Sẽ thử lại sau 0.5s...");
            Invoke(nameof(DelayedStart), 0.5f);
            return;
        }

        QuestManager.Instance.OnStepChanged.AddListener(OnStepChanged);
        QuestManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);
        
        // Lắng nghe scene load — khi Map load xong, re-evaluate để tìm bridge
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Quest có thể đã start trước khi manager này Awake — evaluate ngay
        EvaluateCurrentStep();
    }

    private void DelayedStart()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("[QuestMarkerManager] QuestManager.Instance vẫn NULL sau delay!");
            return;
        }

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
        Debug.Log($"[QuestMarkerManager] Scene loaded: '{scene.name}' — re-evaluating current step.");
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

        // Tìm tất cả bridge trong scene khớp quest + step
        var allBridges = FindObjectsByType<QuestMarkerBridge>(FindObjectsSortMode.None);
        Debug.Log($"[QuestMarkerManager] Found {allBridges.Length} bridges total in scene");
        
        foreach (var bridge in allBridges)
        {
            Debug.Log($"[QuestMarkerManager] Bridge '{bridge.name}': questId='{bridge.QuestId}' stepIndex={bridge.StepIndex}");
        }
        
        bool anyFound  = false;

        foreach (var bridge in allBridges)
        {
            if (!bridge.MatchesCurrentStep(questId, stepIndex)) continue;
            SpawnMarker(bridge);
            anyFound = true;
        }

        if (!anyFound)
            Debug.LogWarning($"[QuestMarkerManager] Không tìm thấy QuestMarkerBridge nào cho questId='{questId}' stepIndex={stepIndex}.");
    }

    private void SpawnMarker(QuestMarkerBridge bridge)
    {
        if (_activeMarkers.ContainsKey(bridge)) return;
        if (!IsReadyToSpawn()) return;

        // Màn hình chính
        var marker = Instantiate(markerPrefab, markerContainer);
        marker.InitializeFromBridge(bridge, markerContainer);
        _activeMarkers[bridge] = marker;

        // Minimap (optional)
        if (minimapMarkerPrefab != null
            && MinimapController.Instance      != null
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
        foreach (var m in _activeMarkers.Values)        if (m  != null) Destroy(m.gameObject);
        foreach (var m in _activeMinimapMarkers.Values) if (m  != null) Destroy(m.gameObject);
        _activeMarkers.Clear();
        _activeMinimapMarkers.Clear();
        Debug.Log("[QuestMarkerManager] Cleared all markers.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ResolveMarkerContainer()
    {
        if (markerContainer != null) return;
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            markerContainer = canvas.GetComponent<RectTransform>();
            Debug.Log($"[QuestMarkerManager] Auto-found Canvas: {canvas.name}");
        }
        else
            Debug.LogWarning("[QuestMarkerManager] Canvas not found. Hãy gán markerContainer trong Inspector.");
    }

    private bool IsReadyToSpawn()
    {
        if (markerPrefab != null && markerContainer != null) return true;
        Debug.LogError("[QuestMarkerManager] markerPrefab hoặc markerContainer chưa được set.");
        return false;
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