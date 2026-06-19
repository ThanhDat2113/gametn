using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Singleton điều phối dẫn đường — dùng NavMesh để TỰ TÍNH đường đi,
/// không cần đặt waypoint tay. Player vẫn di chuyển bằng CharacterController/Rigidbody
/// riêng của bạn; NavMesh chỉ dùng để QUERY path (NavMesh.CalculatePath),
/// không có NavMeshAgent nào di chuyển thực tế.
///
/// YÊU CẦU SCENE:
///   - Đã Bake NavMesh cho địa hình (Window → AI → Navigation → Bake)
///   - QuestMarkerManager + QuestMarkerBridge đã setup như cũ
///
/// LUỒNG:
///   QuestStep.Talk → tìm QuestMarkerBridge theo triggerID
///   → NavMesh.CalculatePath(player, npc) → lấy corners[]
///   → corner kế tiếp = waypoint hiện tại → QuestMarkerUI.SetWaypointTarget()
///   → player đến gần corner → chuyển sang corner tiếp theo
///   → hết corner (đã ở conner cuối = vị trí NPC) → QuestMarkerUI.SetNPCMode()
/// </summary>
[DisallowMultipleComponent]
public class WaypointNavigator : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static WaypointNavigator Instance { get; private set; }

    // ── Serialized ────────────────────────────────────────────────────────────

    [Header("References (tự tìm nếu để trống)")]
    [SerializeField] private Transform playerTransform;

    [Header("NavMesh Settings")]
    [Tooltip("Bán kính để coi là 'đã đến' một corner trên path (units)")]
    [SerializeField] private float cornerArrivalRadius = 1.2f;

    [Tooltip("Khoảng thời gian (giây) giữa các lần tính lại path. " +
             "Không cần tính mỗi frame vì NPC thường đứng yên.")]
    [SerializeField] private float recalculateInterval = 1f;

    [Tooltip("NavMesh area mask cho phép đi (mặc định: tất cả)")]
    [SerializeField] private int areaMask = NavMesh.AllAreas;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;
    [SerializeField] private bool drawPathGizmo = true;

    // ── Private State ─────────────────────────────────────────────────────────

    private QuestMarkerBridge _targetBridge;
    private QuestMarkerUI     _activeMarkerUI;

    private NavMeshPath _navPath;
    private int     _currentCornerIndex;
    private float   _recalcTimer;
    private bool    _pathValid;

    // ── Public Properties ─────────────────────────────────────────────────────

    public bool IsNavigating => _targetBridge != null;

    /// <summary>Đã đi hết các corner trung gian → corner cuối chính là NPC.</summary>
    public bool IsPointingAtNPC =>
        IsNavigating && (!_pathValid || _navPath == null || _currentCornerIndex >= _navPath.corners.Length - 1);

    public Vector3? CurrentTargetPosition
    {
        get
        {
            if (!IsNavigating) return null;

            if (_pathValid && _navPath != null && _currentCornerIndex < _navPath.corners.Length)
                return _navPath.corners[_currentCornerIndex];

            // Path lỗi/không tính được → fallback chỉ thẳng NPC
            return _targetBridge.MarkerPosition;
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _navPath = new NavMeshPath();

        ResolvePlayer();
    }

    private void Start()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[WaypointNavigator] QuestManager.Instance not found.");
            return;
        }

        QuestManager.Instance.OnStepChanged.AddListener(OnStepChanged);
        QuestManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);
        EvaluateStep(QuestManager.Instance.CurrentStep);
    }

    private void Update()
    {
        if (!IsNavigating || playerTransform == null) return;

        // Tính lại path định kỳ (NPC/player có thể đã di chuyển)
        _recalcTimer -= Time.deltaTime;
        if (_recalcTimer <= 0f)
        {
            RecalculatePath();
            _recalcTimer = recalculateInterval;
        }

        CheckCornerArrival();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (QuestManager.Instance == null) return;
        QuestManager.Instance.OnStepChanged.RemoveListener(OnStepChanged);
        QuestManager.Instance.OnStepCompleted.RemoveListener(OnStepCompleted);
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    private void ResolvePlayer()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
        else Debug.LogWarning("[WaypointNavigator] Không tìm thấy Player tag!");
    }

    // ── Quest Events ──────────────────────────────────────────────────────────

    private void OnStepChanged(QuestStep step) => EvaluateStep(step);

    private void OnStepCompleted(QuestStep step)
    {
        if (_targetBridge != null && step?.targetId == _targetBridge.TriggerID)
            StopNavigation();
    }

    private void EvaluateStep(QuestStep step)
    {
        if (step == null || step.type != QuestStepType.Talk || step.isCompleted)
        {
            StopNavigation();
            return;
        }

        QuestMarkerBridge bridge = FindBridgeByID(step.targetId);
        if (bridge == null)
        {
            Log($"Không tìm thấy QuestMarkerBridge cho '{step.targetId}'.");
            StopNavigation();
            return;
        }

        StartNavigation(bridge);
    }

    // ── Navigation Logic ──────────────────────────────────────────────────────

    private void StartNavigation(QuestMarkerBridge bridge)
    {
        if (_targetBridge == bridge) return;

        _targetBridge       = bridge;
        _activeMarkerUI      = FindMarkerUIByBridge(bridge);
        _currentCornerIndex = 1; // corner[0] luôn = vị trí player lúc tính path

        Log($"Bắt đầu dẫn đường (NavMesh) → '{bridge.TriggerID}'");
        RecalculatePath();
        _recalcTimer = recalculateInterval;
        NotifyUI();
    }

    private void StopNavigation()
    {
        if (_targetBridge == null) return;
        Log("Dừng dẫn đường.");

        _activeMarkerUI?.SetNPCMode();

        _targetBridge       = null;
        _activeMarkerUI      = null;
        _pathValid          = false;
        _currentCornerIndex = 0;
    }

    private void RecalculatePath()
    {
        if (_targetBridge == null || playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 npcPos     = _targetBridge.MarkerPosition;

        var reason = NavMeshPathfinder.TryCalculatePath(playerPos, npcPos, _navPath, areaMask);

#if UNITY_EDITOR
        _debugPlayerPos    = playerPos;
        _debugNpcPos       = npcPos;
        _debugPlayerOnMesh = NavMeshPathfinder.SampleOnNavMesh(playerPos, out _, areaMask);
        _debugNpcOnMesh    = NavMeshPathfinder.SampleOnNavMesh(npcPos, out _, areaMask);
#endif

        if (reason != NavMeshPathfinder.FailReason.None)
        {
            string detail = reason switch
            {
                NavMeshPathfinder.FailReason.FromNotOnNavMesh =>
                    $"Player tại {playerPos} KHÔNG nằm gần NavMesh (trong 5m không thấy NavMesh nào).",
                NavMeshPathfinder.FailReason.ToNotOnNavMesh =>
                    $"NPC tại {npcPos} KHÔNG nằm gần NavMesh.",
                NavMeshPathfinder.FailReason.PathPartial =>
                    "Có path nhưng KHÔNG TRỌN VẸN — NavMesh bị đứt đoạn/chặn giữa 2 điểm.",
                NavMeshPathfinder.FailReason.PathInvalid =>
                    "NavMesh.CalculatePath() thất bại hoàn toàn.",
                _ => "Không xác định."
            };

            LogFailReasonThrottled(reason, detail);
            _pathValid = false;
            NotifyUI();
            return;
        }

        _lastLoggedReason = NavMeshPathfinder.FailReason.None;
        _pathValid = true;

        // Giữ currentCornerIndex hợp lệ trong path mới (path có thể đổi số corner)
        if (_currentCornerIndex >= _navPath.corners.Length)
            _currentCornerIndex = Mathf.Max(1, _navPath.corners.Length - 1);

        NotifyUI();
    }

    private void CheckCornerArrival()
    {
        if (!_pathValid) return;
        if (_currentCornerIndex >= _navPath.corners.Length) return;

        // Đang ở corner cuối → đó chính là NPC, không cần advance thêm
        if (IsPointingAtNPC) return;

        Vector3 corner = _navPath.corners[_currentCornerIndex];
        Vector3 delta = corner - playerTransform.position;
        delta.y = 0f;

        if (delta.sqrMagnitude <= cornerArrivalRadius * cornerArrivalRadius)
        {
            _currentCornerIndex++;
            Log(IsPointingAtNPC
                ? "Đã qua hết corner → chỉ thẳng NPC."
                : $"Chuyển sang corner [{_currentCornerIndex}/{_navPath.corners.Length - 1}]");
            NotifyUI();
        }
    }

    // ── UI Communication ──────────────────────────────────────────────────────

    private void NotifyUI()
    {
        if (_activeMarkerUI == null) return;

        if (IsPointingAtNPC)
        {
            _activeMarkerUI.SetNPCMode();
        }
        else
        {
            Vector3? pos = CurrentTargetPosition;
            if (pos.HasValue)
                _activeMarkerUI.SetWaypointTarget(pos.Value);
        }
    }

    /// <summary>Gọi bởi QuestMarkerManager ngay sau khi spawn marker mới.</summary>
    public void OnMarkerRegistered(QuestMarkerBridge bridge, QuestMarkerUI markerUI)
    {
        if (_targetBridge != bridge) return;
        _activeMarkerUI = markerUI;
        NotifyUI();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private QuestMarkerBridge FindBridgeByID(string id)
    {
        foreach (QuestMarkerBridge b in FindObjectsOfType<QuestMarkerBridge>())
            if (b.TriggerID == id) return b;
        return null;
    }

    private QuestMarkerUI FindMarkerUIByBridge(QuestMarkerBridge bridge)
    {
        if (bridge == null || QuestMarkerManager.Instance == null) return null;
        return QuestMarkerManager.Instance.GetMarkerUI(bridge);
    }

    private NavMeshPathfinder.FailReason _lastLoggedReason = NavMeshPathfinder.FailReason.None;

    private void Log(string msg)
    {
        if (verboseLog) Debug.Log($"[WaypointNavigator] {msg}");
    }

    /// <summary>Chỉ log khi lý do thất bại THAY ĐỔI, tránh spam Console mỗi giây.</summary>
    private void LogFailReasonThrottled(NavMeshPathfinder.FailReason reason, string detail)
    {
        if (reason == _lastLoggedReason) return;
        _lastLoggedReason = reason;
        Log($"⚠ NavMesh path đến '{_targetBridge.TriggerID}' thất bại. Lý do: {detail} Fallback chỉ thẳng NPC.");
    }

#if UNITY_EDITOR
    private Vector3? _debugPlayerPos;
    private Vector3? _debugNpcPos;
    private bool      _debugPlayerOnMesh;
    private bool      _debugNpcOnMesh;

    private void OnDrawGizmos()
    {
        if (!drawPathGizmo) return;

        // Vẽ path nếu hợp lệ
        if (_pathValid && _navPath != null && _navPath.corners != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _navPath.corners.Length - 1; i++)
                Gizmos.DrawLine(_navPath.corners[i], _navPath.corners[i + 1]);

            for (int i = 0; i < _navPath.corners.Length; i++)
            {
                Gizmos.color = i == _currentCornerIndex ? Color.yellow : Color.cyan;
                Gizmos.DrawWireSphere(_navPath.corners[i], 0.2f);
            }
        }

        // Vẽ vị trí player/NPC + trạng thái sample lên NavMesh (đỏ = lỗi, xanh = OK)
        if (_debugPlayerPos.HasValue)
        {
            Gizmos.color = _debugPlayerOnMesh ? Color.green : Color.red;
            Gizmos.DrawWireSphere(_debugPlayerPos.Value, 0.4f);
        }
        if (_debugNpcPos.HasValue)
        {
            Gizmos.color = _debugNpcOnMesh ? Color.green : Color.red;
            Gizmos.DrawWireSphere(_debugNpcPos.Value, 0.4f);
        }
    }
#endif
}