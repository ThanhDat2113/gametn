using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Vùng encounter ngẫu nhiên kiểu "cỏ dại Pokémon" — gắn lên GameObject có Collider (isTrigger=true).
/// Khi player ở trong vùng, hệ thống tự roll % để bắt đầu combat với 1 enemy group
/// được chọn ngẫu nhiên theo trọng số (weight) trong list cấu hình.
///
/// TÁI SỬ DỤNG TOÀN BỘ pipeline có sẵn của MapEnemy:
///   FormationManager.SaveFormation() → CombatSessionData.Set() → FadeController →
///   ẩn MapRoot/PersistentContainer → SceneLoaderManager.LoadCombatScene()
///
/// LEVEL SCALING: vì EnemyGroupData là ScriptableObject (asset chung, có thể được
/// nhiều EncounterZone/MapEnemy khác tham chiếu), KHÔNG sửa trực tiếp asset gốc.
/// Thay vào đó, tạo 1 RUNTIME CLONE (ScriptableObject.Instantiate) mỗi khi trigger
/// combat, áp dụng level scaling lên bản clone, rồi truyền clone đó vào CombatSessionData.
/// Asset gốc trên disk không bao giờ bị thay đổi.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EncounterZone : MonoBehaviour
{
    public enum TriggerMode
    {
        ByStep,   // roll % mỗi khi player di chuyển qua 1 khoảng cách nhất định
        ByTime,   // roll % mỗi X giây khi đang ở trong vùng
        Both      // roll cả 2 cách — gặp ở bất kỳ điều kiện nào trước
    }

    [System.Serializable]
    public class WeightedEncounter
    {
        [Tooltip("Enemy group dùng làm nguồn dữ liệu (asset gốc, KHÔNG bị sửa khi scale level).")]
        public EnemyGroupData enemyGroup;

        [Tooltip("Trọng số xuất hiện so với các entry khác trong list. " +
                 "Trọng số càng cao, tỉ lệ được chọn càng lớn. Vd: weight=50 xuất hiện gấp 5 lần weight=10.")]
        [Min(0.01f)] public float weight = 1f;

        [Header("Level Scaling (áp dụng lên bản clone runtime)")]
        [Tooltip("Level tối thiểu sẽ cộng thêm vào level gốc của mỗi enemy trong group.")]
        public int levelOffsetMin = 0;
        [Tooltip("Level tối đa sẽ cộng thêm vào level gốc của mỗi enemy trong group.")]
        public int levelOffsetMax = 0;
    }

    [Header("Encounter Pool")]
    [Tooltip("Danh sách enemy group có thể gặp trong vùng này, kèm trọng số + level scaling riêng.")]
    [SerializeField] private List<WeightedEncounter> encounterPool = new List<WeightedEncounter>();

    [Header("Trigger Mode")]
    [Tooltip("ByStep: roll theo khoảng cách di chuyển. ByTime: roll theo thời gian. Both: cả 2.")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.Both;

    [Header("By Step Settings")]
    [Tooltip("Khoảng cách (world units) player phải di chuyển trong vùng trước khi roll 1 lần.")]
    [SerializeField] private float stepDistance = 1.5f;
    [Tooltip("Tỉ lệ % (0-100) gặp encounter mỗi lần roll theo step.")]
    [Range(0f, 100f)] [SerializeField] private float stepEncounterChance = 10f;

    [Header("By Time Settings")]
    [Tooltip("Khoảng thời gian (giây) giữa mỗi lần roll theo thời gian.")]
    [SerializeField] private float timeInterval = 2f;
    [Tooltip("Tỉ lệ % (0-100) gặp encounter mỗi lần roll theo thời gian.")]
    [Range(0f, 100f)] [SerializeField] private float timeEncounterChance = 8f;

    [Header("Cooldown")]
    [Tooltip("Bật cooldown sau mỗi lần encounter (hoặc sau khi rời vùng) để tránh combat liên tục.")]
    [SerializeField] private bool useCooldown = true;
    [Tooltip("Thời gian (giây) phải chờ sau 1 lần encounter trước khi có thể roll lại — " +
             "tính từ lúc PLAYER QUAY LẠI vùng (sau combat) hoặc từ lúc rời vùng rồi vào lại.")]
    [SerializeField] private float cooldownDuration = 3f;

    [Header("Transition")]
    [Tooltip("LƯU Ý: field này hiện KHÔNG được dùng trực tiếp — FadeController.Instance.FadeToBlack() " +
             "dùng fadeDuration riêng của chính FadeController (giống pattern trong MapEnemy.cs gốc). " +
             "Giữ field này để dễ mở rộng sau nếu muốn override fade duration theo từng zone.")]
    [SerializeField] private float fadeDuration = 0.5f;

    // ── Runtime state ────────────────────────────────────────────────────────
    private bool _playerInZone = false;
    private Transform _playerTransform;
    private Vector3 _lastStepCheckPosition;
    private float _timeSinceLastTimeRoll = 0f;
    private float _cooldownTimer = 0f;
    private bool _isTransitioning = false; // chặn double-trigger trong lúc đang fade/load scene

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[EncounterZone] Collider trên '{gameObject.name}' chưa set isTrigger=true — " +
                              "đã tự động bật để vùng hoạt động đúng.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInZone = true;
        _playerTransform = other.transform;
        _lastStepCheckPosition = _playerTransform.position;
        _timeSinceLastTimeRoll = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (other.transform != _playerTransform) return;

        _playerInZone = false;
        _playerTransform = null;
    }

    private void Update()
    {
        // Cooldown đếm ngược liên tục, không phụ thuộc việc player có trong vùng hay không —
        // để player rời vùng rồi vào lại vẫn phải tôn trọng cooldown nếu chưa hết giờ.
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        if (!_playerInZone || _isTransitioning || _playerTransform == null) return;
        if (useCooldown && _cooldownTimer > 0f) return;
        if (encounterPool == null || encounterPool.Count == 0) return;

        // An toàn bổ sung: nếu EncounterZone KHÔNG phải con của MapRoot (nên không tự
        // deactivate cùng map khi vào combat), vẫn phải chặn roll khi đang trong combat —
        // CombatSessionData.HasData = true nghĩa là 1 session combat đang chờ/đang diễn ra.
        if (CombatSessionData.HasData) return;

        bool checkByStep = triggerMode == TriggerMode.ByStep || triggerMode == TriggerMode.Both;
        bool checkByTime = triggerMode == TriggerMode.ByTime || triggerMode == TriggerMode.Both;

        if (checkByStep) TickStepCheck();
        if (checkByTime) TickTimeCheck();
    }

    private void TickStepCheck()
    {
        float distMoved = Vector3.Distance(_playerTransform.position, _lastStepCheckPosition);
        if (distMoved < stepDistance) return;

        _lastStepCheckPosition = _playerTransform.position;
        RollEncounter(stepEncounterChance);
    }

    private void TickTimeCheck()
    {
        _timeSinceLastTimeRoll += Time.deltaTime;
        if (_timeSinceLastTimeRoll < timeInterval) return;

        _timeSinceLastTimeRoll = 0f;
        RollEncounter(timeEncounterChance);
    }

    /// <summary>Roll % để xem có gặp encounter không. Nếu trúng, chọn enemy group theo weight và bắt đầu combat.</summary>
    private void RollEncounter(float chancePercent)
    {
        if (_isTransitioning) return;

        float roll = Random.Range(0f, 100f);
        if (roll > chancePercent) return; // không trúng lần này

        WeightedEncounter chosen = PickWeightedEncounter();
        if (chosen == null || chosen.enemyGroup == null)
        {
            Debug.LogWarning("[EncounterZone] Roll trúng nhưng encounterPool rỗng hoặc enemyGroup null — bỏ qua.");
            return;
        }

        _isTransitioning = true;
        StartCoroutine(StartRandomCombatTransition(chosen));
    }

    /// <summary>Random weighted pick — trọng số càng cao, khả năng được chọn càng lớn.</summary>
    private WeightedEncounter PickWeightedEncounter()
    {
        var validEntries = encounterPool.Where(e => e.enemyGroup != null && e.weight > 0f).ToList();
        if (validEntries.Count == 0) return null;

        float totalWeight = validEntries.Sum(e => e.weight);
        float roll = Random.Range(0f, totalWeight);

        float cumulative = 0f;
        foreach (var entry in validEntries)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return entry;
        }
        return validEntries[validEntries.Count - 1]; // fallback an toàn cho sai số float
    }

    private IEnumerator StartRandomCombatTransition(WeightedEncounter chosen)
    {
        var formationManager = FindFirstObjectByType<FormationManager>();
        if (formationManager == null)
        {
            Debug.LogError("[EncounterZone] Không tìm thấy FormationManager!");
            _isTransitioning = false;
            yield break;
        }
        formationManager.SaveFormation();

        // Tạo runtime clone của EnemyGroupData để scale level mà KHÔNG sửa asset gốc.
        EnemyGroupData scaledGroup = CreateScaledClone(chosen);

        CombatSessionData.Set(FormationDataStorage.PendingFormation, scaledGroup, fromMap: true);
        // Lưu ý: KHÔNG gọi CombatSceneStarter.RegisterLastEnemy vì đây không phải MapEnemy cố định
        // trong scene — không cần MarkAsDefeated sau combat (zone vẫn còn để gặp lại lần khác).

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

        var mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            SceneLoaderManager.MapRoot = mapRoot;
            mapRoot.SetActive(false);
            Debug.Log("[EncounterZone] MapRoot found and deactivated.");
        }
        else
        {
            Debug.LogError("[EncounterZone] Không tìm thấy MapRoot! Hãy tạo một GameObject tên 'MapRoot' chứa toàn bộ map.");
        }

        var persistentContainer = GameObject.Find("PersistentContainer");
        if (persistentContainer != null)
        {
            SceneLoaderManager.PersistentContainer = persistentContainer;
            persistentContainer.SetActive(false);
            Debug.Log("[EncounterZone] PersistentContainer found and deactivated.");
        }
        else
        {
            Debug.LogWarning("[EncounterZone] Không tìm thấy PersistentContainer (không bắt buộc).");
        }

        SceneLoaderManager.LoadCombatScene();

        if (useCooldown) _cooldownTimer = cooldownDuration;

        // Cooldown bắt đầu tính ngay từ lúc này (không chờ quay lại map) — vì Update() của
        // EncounterZone sẽ tự dừng khi MapRoot.SetActive(false) (GameObject bị deactivate
        // cùng toàn bộ children). Time.deltaTime cũng dừng tính theo. Khi quay lại map,
        // MapRoot.SetActive(true) → EncounterZone active lại → OnEnable() reset _isTransitioning,
        // còn _cooldownTimer (đã set ở trên) tiếp tục đếm ngược đúng từ giá trị cũ trong Update().
    }

    /// <summary>
    /// Tạo bản sao runtime của EnemyGroupData, áp dụng level offset ngẫu nhiên
    /// (trong khoảng [levelOffsetMin, levelOffsetMax]) lên từng enemy. Bản clone này
    /// chỉ tồn tại trong RAM của session combat hiện tại — không ghi vào asset trên disk.
    /// </summary>
    private EnemyGroupData CreateScaledClone(WeightedEncounter chosen)
    {
        EnemyGroupData clone = ScriptableObject.Instantiate(chosen.enemyGroup);
        clone.name = chosen.enemyGroup.name + "_RuntimeScaled";

        int offset = Random.Range(chosen.levelOffsetMin, chosen.levelOffsetMax + 1);

        if (clone.enemies != null)
        {
            // Deep-copy từng EnemyEntry để không vô tình share reference với asset gốc
            // (ScriptableObject.Instantiate chỉ shallow-copy array of class, các EnemyEntry
            // bên trong VẪN LÀ CÙNG REFERENCE với asset gốc nếu không deep-copy thủ công).
            var clonedEntries = new EnemyGroupData.EnemyEntry[clone.enemies.Length];
            for (int i = 0; i < clone.enemies.Length; i++)
            {
                var original = clone.enemies[i];
                clonedEntries[i] = new EnemyGroupData.EnemyEntry
                {
                    data = original.data,
                    level = Mathf.Max(1, original.level + offset),
                    gridSlot = original.gridSlot
                };
            }
            clone.enemies = clonedEntries;
        }

        Debug.Log($"[EncounterZone] Tạo encounter '{clone.name}' với level offset +{offset} " +
                  $"(min={chosen.levelOffsetMin}, max={chosen.levelOffsetMax}).");

        return clone;
    }

    private void OnEnable()
    {
        // Reset transition flag khi zone được active lại (vd map hiện lại sau khi unload combat scene).
        // Quan trọng: nếu không reset, zone sẽ bị "khóa" vĩnh viễn sau lần encounter đầu tiên.
        _isTransitioning = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.localScale.x);
        }

        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
            $"EncounterZone\nMode: {triggerMode}\nPool: {(encounterPool?.Count ?? 0)} groups");
    }
#endif
}
