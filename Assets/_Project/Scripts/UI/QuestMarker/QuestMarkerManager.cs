using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton manager quản lý toàn bộ quest marker UI.
/// Đặt trên một GameObject tên "QuestMarkerManager" trong scene.
///
/// TÍCH HỢP QUEST SYSTEM:
///   Nhận Register/Unregister từ QuestMarkerBridge (gắn trên DialogueTrigger)
///   thay vì NPCInteractable (không còn dùng nữa).
/// </summary>
[DisallowMultipleComponent]
public class QuestMarkerManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static QuestMarkerManager Instance { get; private set; }

    // ── Serialized Fields ────────────────────────────────────────────────────

    [Header("Prefab References")]
    [Tooltip("Prefab chứa QuestMarkerUI component")]
    [SerializeField] private QuestMarkerUI markerPrefab;

    [Tooltip("Prefab chứa MinimapMarkerUI component (marker hiển thị trên minimap). " +
             "Để trống nếu không dùng minimap.")]
    [SerializeField] private MinimapMarkerUI minimapMarkerPrefab;

    [Header("UI Container")]
    [Tooltip("RectTransform của Canvas dùng để chứa marker. Để trống sẽ tự tìm Canvas.")]
    [SerializeField] private RectTransform markerContainer;

    // ── Private Fields ───────────────────────────────────────────────────────

    // Key đổi từ NPCInteractable → QuestMarkerBridge
    private readonly Dictionary<QuestMarkerBridge, QuestMarkerUI> _activeMarkers =
        new Dictionary<QuestMarkerBridge, QuestMarkerUI>();

    // Marker minimap song song với marker màn hình chính (cùng key, container khác)
    private readonly Dictionary<QuestMarkerBridge, MinimapMarkerUI> _activeMinimapMarkers =
        new Dictionary<QuestMarkerBridge, MinimapMarkerUI>();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (markerPrefab == null)
        {
            markerPrefab = Resources.Load<QuestMarkerUI>("UI/QuestMarkerUI");
            if (markerPrefab == null)
                Debug.LogWarning("[QuestMarkerManager] Marker prefab not found. " +
                                 "Assign in Inspector hoặc đặt tại Resources/UI/QuestMarkerUI.");
        }

        if (markerContainer == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                markerContainer = canvas.GetComponent<RectTransform>();
                Debug.Log($"[QuestMarkerManager] Auto-found Canvas: {canvas.name}");
            }
            else
            {
                Debug.LogWarning("[QuestMarkerManager] Canvas not found. Hãy gán markerContainer trong Inspector.");
            }
        }
    }

    private bool IsReadyToSpawn()
    {
        if (markerPrefab == null || markerContainer == null)
        {
            Debug.LogError("[QuestMarkerManager] markerPrefab hoặc markerContainer chưa được set.");
            return false;
        }
        return true;
    }

    // ── Public API (Bridge-based) ─────────────────────────────────────────────

    /// <summary>
    /// Đăng ký một Bridge → tạo marker trỏ đến NPC đó.
    /// Gọi bởi QuestMarkerBridge khi step Talk khớp triggerID.
    /// </summary>
    public void RegisterBridge(QuestMarkerBridge bridge)
    {
        if (bridge == null) return;

        if (!_activeMarkers.ContainsKey(bridge) && IsReadyToSpawn())
        {
            QuestMarkerUI marker = Instantiate(markerPrefab, markerContainer);
            marker.InitializeFromBridge(bridge, markerContainer);
            _activeMarkers[bridge] = marker;

            Debug.Log($"[QuestMarkerManager] Marker ON → '{bridge.TriggerID}'");
        }

        // Minimap marker là optional — chỉ spawn nếu đã setup minimap
        if (!_activeMinimapMarkers.ContainsKey(bridge)
            && minimapMarkerPrefab != null
            && MinimapController.Instance != null
            && MinimapController.Instance.MarkerContainer != null)
        {
            MinimapMarkerUI minimapMarker = Instantiate(minimapMarkerPrefab, MinimapController.Instance.MarkerContainer);
            minimapMarker.InitializeFromBridge(bridge);
            _activeMinimapMarkers[bridge] = minimapMarker;

            Debug.Log($"[QuestMarkerManager] Minimap Marker ON → '{bridge.TriggerID}'");
        }
    }

    /// <summary>
    /// Hủy đăng ký Bridge → destroy marker.
    /// Gọi bởi QuestMarkerBridge khi step hoàn thành hoặc object bị destroy.
    /// </summary>
    public void UnregisterBridge(QuestMarkerBridge bridge)
    {
        if (bridge == null) return;

        if (_activeMarkers.TryGetValue(bridge, out QuestMarkerUI marker))
        {
            if (marker != null) Destroy(marker.gameObject);
            _activeMarkers.Remove(bridge);
            Debug.Log($"[QuestMarkerManager] Marker OFF → '{bridge.TriggerID}'");
        }

        if (_activeMinimapMarkers.TryGetValue(bridge, out MinimapMarkerUI minimapMarker))
        {
            if (minimapMarker != null) Destroy(minimapMarker.gameObject);
            _activeMinimapMarkers.Remove(bridge);
            Debug.Log($"[QuestMarkerManager] Minimap Marker OFF → '{bridge.TriggerID}'");
        }
    }

    /// <summary>Bật/tắt tất cả markers (dùng khi mở map, cutscene, v.v.).</summary>
    public void SetAllMarkersActive(bool active)
    {
        foreach (QuestMarkerUI marker in _activeMarkers.Values)
            if (marker != null) marker.SetActive(active);

        foreach (MinimapMarkerUI marker in _activeMinimapMarkers.Values)
            if (marker != null) marker.SetActive(active);
    }

    /// <summary>Số lượng marker đang hiển thị (màn hình chính).</summary>
    public int ActiveMarkerCount => _activeMarkers.Count;

    /// <summary>Số lượng marker đang hiển thị trên minimap.</summary>
    public int ActiveMinimapMarkerCount => _activeMinimapMarkers.Count;
}
