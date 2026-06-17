using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton manager quản lý toàn bộ quest marker UI.
/// Đặt trên một GameObject tên "QuestMarkerManager" trong scene.
///
/// TÍCH HỢP WAYPOINT:
///   Sau khi spawn QuestMarkerUI, thông báo WaypointNavigator để nó
///   có thể override chế độ hiển thị (waypoint vs NPC trực tiếp).
/// </summary>
[DisallowMultipleComponent]
public class QuestMarkerManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static QuestMarkerManager Instance { get; private set; }

    // ── Serialized ────────────────────────────────────────────────────────────

    [Header("Prefab References")]
    [SerializeField] private QuestMarkerUI markerPrefab;

    [Header("UI Container")]
    [SerializeField] private RectTransform markerContainer;

    // ── Private ───────────────────────────────────────────────────────────────

    private readonly Dictionary<QuestMarkerBridge, QuestMarkerUI> _activeMarkers =
        new Dictionary<QuestMarkerBridge, QuestMarkerUI>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

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
                Debug.LogWarning("[QuestMarkerManager] Canvas not found.");
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

    // ── Public API ────────────────────────────────────────────────────────────

    public void RegisterBridge(QuestMarkerBridge bridge)
    {
        if (bridge == null || _activeMarkers.ContainsKey(bridge)) return;
        if (!IsReadyToSpawn()) return;

        QuestMarkerUI marker = Instantiate(markerPrefab, markerContainer);
        marker.InitializeFromBridge(bridge, markerContainer);
        _activeMarkers[bridge] = marker;

        Debug.Log($"[QuestMarkerManager] Marker ON → '{bridge.TriggerID}'");

        // Thông báo WaypointNavigator để override chế độ nếu cần
        WaypointNavigator.Instance?.OnMarkerRegistered(bridge, marker);
    }

    public void UnregisterBridge(QuestMarkerBridge bridge)
    {
        if (bridge == null) return;

        if (_activeMarkers.TryGetValue(bridge, out QuestMarkerUI marker))
        {
            if (marker != null) Destroy(marker.gameObject);
            _activeMarkers.Remove(bridge);
            Debug.Log($"[QuestMarkerManager] Marker OFF → '{bridge.TriggerID}'");
        }
    }

    /// <summary>Lấy QuestMarkerUI đang hiển thị cho một bridge cụ thể.</summary>
    public QuestMarkerUI GetMarkerUI(QuestMarkerBridge bridge)
    {
        if (bridge == null) return null;
        _activeMarkers.TryGetValue(bridge, out QuestMarkerUI ui);
        return ui;
    }

    public void SetAllMarkersActive(bool active)
    {
        foreach (QuestMarkerUI marker in _activeMarkers.Values)
            if (marker != null) marker.SetActive(active);
    }

    public int ActiveMarkerCount => _activeMarkers.Count;
}