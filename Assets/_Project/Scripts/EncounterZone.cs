using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Vùng encounter ngẫu nhiên kiểu "cỏ dại Pokémon" — gắn lên GameObject có Collider (isTrigger=true).
/// Khi player di chuyển trong vùng, hệ thống tích lũy thời gian:
///   - 0 → 5 giây: không thể gặp quái (an toàn).
///   - 5 → 8 giây: có xác suất gặp quái (theo encounterChance).
///   - Sau 8 giây: chắc chắn gặp quái.
/// Đứng yên thì tạm dừng timer.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EncounterZone : MonoBehaviour
{
    [System.Serializable]
    public class WeightedEncounter
    {
        public EnemyGroupData enemyGroup;
        [Min(0.01f)] public float weight = 1f;
        public int levelOffsetMin = 0;
        public int levelOffsetMax = 0;
    }

    [Header("Encounter Pool")]
    [SerializeField] private List<WeightedEncounter> encounterPool = new List<WeightedEncounter>();

    [Header("Movement Time Encounter")]
    [Tooltip("Thời gian di chuyển an toàn tối thiểu (giây) — không thể gặp quái trước khi đạt mốc này.")]
    [SerializeField] private float safeTime = 5f;
    [Tooltip("Thời gian di chuyển tối đa (giây) — sau mốc này sẽ chắc chắn gặp quái.")]
    [SerializeField] private float maxTime = 8f;
    [Tooltip("Xác suất gặp quái (%) trong khoảng thời gian từ safeTime đến maxTime.")]
    [Range(0f, 100f)] [SerializeField] private float encounterChance = 30f;

    [Header("Cooldown")]
    [SerializeField] private bool useCooldown = true;
    [SerializeField] private float cooldownDuration = 3f;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.5f;

    // ── Runtime state ────────────────────────────────────────────────────────
    private bool _playerInZone = false;
    private Transform _playerTransform;
    private Vector3 _lastPosition;
    private float _movingTimeAccumulated = 0f;
    private float _cooldownTimer = 0f;
    private bool _isTransitioning = false;
    private bool _hasTriggeredEncounter = false;

    // Theo dõi trạng thái đã dừng player để restore đúng cách
    private bool _hasStoppedPlayer = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[EncounterZone] Collider trên '{gameObject.name}' chưa set isTrigger=true — đã tự động bật.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInZone = true;
        _playerTransform = other.transform;
        _lastPosition = _playerTransform.position;
        _movingTimeAccumulated = 0f;
        _hasTriggeredEncounter = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (other.transform != _playerTransform) return;

        _playerInZone = false;
        _playerTransform = null;
        _movingTimeAccumulated = 0f;
        _hasTriggeredEncounter = false;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        if (!_playerInZone || _isTransitioning || _playerTransform == null) return;
        if (useCooldown && _cooldownTimer > 0f) return;
        if (encounterPool == null || encounterPool.Count == 0) return;
        if (CombatSessionData.HasData) return;
        if (_hasTriggeredEncounter) return;

        Vector3 currentPos = _playerTransform.position;
        float distanceMoved = Vector3.Distance(currentPos, _lastPosition);
        _lastPosition = currentPos;

        if (distanceMoved < 0.001f) return;

        _movingTimeAccumulated += Time.deltaTime;

        if (_movingTimeAccumulated < safeTime)
            return;

        if (_movingTimeAccumulated >= maxTime)
        {
            TriggerEncounter();
            return;
        }

        float roll = Random.Range(0f, 100f);
        if (roll <= encounterChance)
            TriggerEncounter();
    }

    private void TriggerEncounter()
    {
        if (_isTransitioning || _hasTriggeredEncounter) return;

        WeightedEncounter chosen = PickWeightedEncounter();
        if (chosen == null || chosen.enemyGroup == null)
        {
            Debug.LogWarning("[EncounterZone] Không có enemy group hợp lệ.");
            return;
        }

        _hasTriggeredEncounter = true;
        _isTransitioning = true;

        // Dừng player ngay lập tức trước khi fade
        StopPlayer();

        StartCoroutine(StartRandomCombatTransition(chosen));
    }

    // ==================== PLAYER STOP / RESTORE ====================

    /// <summary>
    /// Dừng player ngay lập tức: tắt script điều khiển, reset velocity, set idle animation.
    /// </summary>
    private void StopPlayer()
    {
        if (_hasStoppedPlayer) return;

        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        // Reset animation về Idle trước khi disable script
        // (OnDisable() trong HSRPlayerController cũng sẽ gọi ResetToIdle() tự động)
        var hsrController = player.GetComponent<HSRPlayerController>();
        if (hsrController != null)
            hsrController.ResetToIdle();

        // Tắt script điều khiển
        var movementScript = PlayerManager.Instance?.playerMovementScript;
        if (movementScript != null && movementScript.enabled)
        {
            movementScript.enabled = false;
            Debug.Log("[EncounterZone] Disabled player movement script.");
        }

        // Disable CharacterController
        var cc = player.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            Debug.Log("[EncounterZone] Disabled CharacterController.");
        }

        // Reset Rigidbody velocity
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _hasStoppedPlayer = true;
        Debug.Log("[EncounterZone] Player stopped for encounter.");
    }

    /// <summary>
    /// Khôi phục movement sau khi quay về từ combat scene.
    /// </summary>
    private void RestorePlayer()
    {
        if (!_hasStoppedPlayer) return;

        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        // Bật lại CharacterController
        var cc = player.GetComponent<CharacterController>();
        if (cc != null && !cc.enabled)
        {
            cc.enabled = true;
            Debug.Log("[EncounterZone] Re-enabled CharacterController.");
        }

        // Bật lại script điều khiển
        var movementScript = PlayerManager.Instance?.playerMovementScript;
        if (movementScript != null && !movementScript.enabled)
        {
            movementScript.enabled = true;
            Debug.Log("[EncounterZone] Re-enabled player movement script.");
        }

        _hasStoppedPlayer = false;
        Debug.Log("[EncounterZone] Player movement restored.");
    }

    // ==================== ENCOUNTER FLOW ====================

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
        return validEntries[validEntries.Count - 1];
    }

    private IEnumerator StartRandomCombatTransition(WeightedEncounter chosen)
    {
        var formationManager = FindFirstObjectByType<FormationManager>();
        if (formationManager == null)
        {
            Debug.LogError("[EncounterZone] Không tìm thấy FormationManager!");
            _isTransitioning = false;
            RestorePlayer(); // Đảm bảo player được restore nếu có lỗi
            yield break;
        }
        formationManager.SaveFormation();

        EnemyGroupData scaledGroup = CreateScaledClone(chosen);
        CombatSessionData.Set(FormationDataStorage.PendingFormation, scaledGroup, fromMap: true);

        // Fade to black — player đã đứng yên từ TriggerEncounter()
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

        var mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            SceneLoaderManager.MapRoot = mapRoot;
            mapRoot.SetActive(false);
        }
        else
        {
            Debug.LogError("[EncounterZone] Không tìm thấy MapRoot!");
        }

        var persistentContainer = GameObject.Find("PersistentContainer");
        if (persistentContainer != null)
        {
            SceneLoaderManager.PersistentContainer = persistentContainer;
            persistentContainer.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[EncounterZone] Không tìm thấy PersistentContainer.");
        }

        SceneLoaderManager.LoadCombatScene();

        if (useCooldown) _cooldownTimer = cooldownDuration;
        _movingTimeAccumulated = 0f;
    }

    private EnemyGroupData CreateScaledClone(WeightedEncounter chosen)
    {
        EnemyGroupData clone = ScriptableObject.Instantiate(chosen.enemyGroup);
        clone.name = chosen.enemyGroup.name + "_RuntimeScaled";

        int offset = Random.Range(chosen.levelOffsetMin, chosen.levelOffsetMax + 1);

        if (clone.enemies != null)
        {
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

        Debug.Log($"[EncounterZone] Tạo encounter '{clone.name}' với level offset +{offset}.");
        return clone;
    }

    /// <summary>
    /// OnEnable được gọi khi MapRoot được bật lại sau khi quay về từ combat scene.
    /// Đây là thời điểm khôi phục movement cho player.
    /// </summary>
    private void OnEnable()
    {
        _isTransitioning = false;

        // Khôi phục movement nếu đã bị dừng trước đó
        // Dùng coroutine để đảm bảo PlayerManager và các component đã sẵn sàng
        StartCoroutine(RestorePlayerDelayed());

        if (_playerInZone)
        {
            _movingTimeAccumulated = 0f;
            _hasTriggeredEncounter = false;
        }
    }

    /// <summary>
    /// Chờ 1 frame để đảm bảo tất cả component (PlayerManager, CharacterController...)
    /// đã được khởi tạo/enable trước khi restore.
    /// </summary>
    private IEnumerator RestorePlayerDelayed()
    {
        yield return null; // Chờ 1 frame
        RestorePlayer();
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
            $"EncounterZone\nSafe: {safeTime}s | Max: {maxTime}s\nChance: {encounterChance}%");
    }
#endif
}