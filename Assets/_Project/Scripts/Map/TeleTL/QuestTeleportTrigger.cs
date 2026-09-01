using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// Clear màn đen "giữ" từ trigger teleport khi scene mới load xong (BEFORE render).
/// Subscribe vào SceneManager.sceneLoaded 1 lần duy nhất ở AppStart; mỗi lần load
/// scene mới sẽ clear canvas đen TRƯỚC khi Unity render frame đầu của scene đó.
/// </summary>
internal static class QuestTeleportBlackScreenBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Clear mọi màn đen còn sống từ scene trước (DontDestroyOnLoad canvas).
        // sceneLoaded callback chạy TRƯỚC khi Unity render frame đầu của scene mới
        // → không có frame nào của scene mới bị che đen.
        QuestTeleportTrigger.ClearBlackScreen();
    }
}

/// <summary>
/// Gắn lên cùng GameObject với QuestMarkerBridge (hoặc NPC bất kỳ).
/// Tự động theo dõi khi đúng quest/step hoàn thành + dialogue kết thúc,
/// sau đó phát 1 Timeline (vd: fade-out, hiệu ứng tele...) rồi teleport player.
///
/// FLOW:
///   OnStepCompleted (QuestManager) đúng questId/stepIndex
///     → chờ DialogueBubbleUI.IsShowing == false
///       → phát _timelineDirector (nếu có)
///         → teleport player (cùng scene hoặc load scene mới)
///
/// SETUP NHANH:
///   1. Gắn component này lên NPC/object có QuestMarkerBridge.
///   2. Điền questId + stepIndex khớp với bridge tương ứng.
///   3. Kéo Transform đích vào destinationTransform
///      (tạo 1 empty GameObject đặt đúng vị trí tele, đặt tên "TeleportTarget_...").
///   4. (Tuỳ chọn) Tick useTimeline nếu muốn phát hiệu ứng trước khi tele, rồi kéo
///      PlayableDirector vào teleportTimeline. Bỏ tick useTimeline để bỏ qua Timeline
///      hoàn toàn — teleport ngay sau khi dialogue kết thúc.
///   5. Nếu tele sang scene khác: bật useSceneTransition, điền targetSceneName.
///      destinationTransform lúc này là spawn point trong scene HIỆN TẠI (dùng để
///      lấy toạ độ truyền sang scene mới qua PlayerSpawnPoint hoặc PlayerPrefs).
/// </summary>
public class QuestTeleportTrigger : MonoBehaviour
{
    [Header("Quest Binding — phải khớp với QuestMarkerBridge trên cùng NPC")]
    [Tooltip("questId của QuestData asset mà trigger này theo dõi.")]
    [SerializeField] private string questId;

    [Tooltip("Index của step trong QuestData.steps[] mà trigger này phản ứng (bắt đầu từ 0).")]
    [SerializeField] private int stepIndex;

    [Header("Destination")]
    [Tooltip("Empty GameObject đặt tại vị trí teleport đích trong scene hiện tại.\n" +
             "Khi useSceneTransition = true, vị trí/rotation của Transform này sẽ được lưu " +
             "qua PlayerPrefs để scene mới spawn player đúng chỗ.")]
    [SerializeField] private Transform destinationTransform;

    [Header("Timeline")]
    [Tooltip("Bật để phát Timeline trước khi teleport. Tắt để bỏ qua hoàn toàn bước Timeline " +
             "(teleport ngay sau khi dialogue kết thúc), kể cả khi teleportTimeline có gán sẵn.")]
    [SerializeField] private bool useTimeline = true;

    [Tooltip("PlayableDirector chứa Timeline sẽ phát trước khi teleport (fade-out, hiệu ứng...). " +
             "Chỉ được dùng khi useTimeline = true. Để trống nếu muốn teleport ngay không có hiệu ứng.")]
    [SerializeField] private PlayableDirector teleportTimeline;

    [Tooltip("Nếu bật: chờ Timeline phát xong mới teleport. " +
             "Nếu tắt: teleport ngay khi Timeline bắt đầu (dùng khi Timeline tự xử lý scene load).")]
    [SerializeField] private bool waitForTimeline = true;

    [Tooltip("Chỉ áp dụng khi useSceneTransition = true. Bật để giữ màn hình đen từ lúc " +
             "Timeline kết thúc cho tới khi scene mới load xong (che đi khoảng đen giữa " +
             "2 scene). Khi tắt hoặc tele trong cùng scene: hành vi giữ nguyên như cũ.")]
    [SerializeField] private bool keepBlackUntilSceneLoaded = true;

    [Header("Scene Transition")]
    [Tooltip("Bật để load scene mới sau Timeline. Tắt để teleport trong cùng scene.")]
    [SerializeField] private bool useSceneTransition = false;

    [Tooltip("Tên scene đích (phải có trong Build Settings). Chỉ dùng khi useSceneTransition = true.")]
    [SerializeField] private string targetSceneName;

    [Tooltip("PlayerPrefs key để lưu spawn position X khi chuyển scene. " +
             "Scene đích đọc key này để spawn player đúng vị trí.\n" +
             "Mặc định: 'SpawnX', 'SpawnY', 'SpawnZ', 'SpawnRotY'")]
    [SerializeField] private string spawnPosKeyPrefix = "Spawn";

    [Header("Player Reference")]
    [Tooltip("Để trống sẽ tự tìm qua MinimapController.Instance.Player hoặc tag 'Player'.")]
    [SerializeField] private Transform playerTransform;

    [Header("Dialogue Wait")]
    [Tooltip("Thời gian tối đa chờ dialogue kết thúc (giây) trước khi bỏ qua và teleport luôn.")]
    [SerializeField] private float dialogueWaitTimeout = 30f;

    private bool _triggered = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (QuestManager.Instance != null)
            Subscribe();
        else
            StartCoroutine(WaitForQuestManager());
    }

    /// <summary>
    /// Retry mỗi frame cho đến khi QuestManager.Instance sẵn sàng.
    /// Xử lý đúng trường hợp QuestManager nằm ở scene khác / DontDestroyOnLoad
    /// và chưa Awake() xong khi scene này Start().
    /// </summary>
    private IEnumerator WaitForQuestManager()
    {
        float elapsed = 0f;
        const float warnAfter  = 2f;
        const float giveUpAfter = 30f;

        while (QuestManager.Instance == null)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= giveUpAfter)
            {
                Debug.LogError($"[QuestTeleportTrigger] '{name}': QuestManager.Instance vẫn NULL sau {giveUpAfter}s — trigger sẽ không hoạt động. " +
                               "Kiểm tra lại thứ tự load scene hoặc QuestManager có được khởi tạo không.");
                yield break;
            }

            if (elapsed >= warnAfter && elapsed - Time.unscaledDeltaTime < warnAfter)
                Debug.LogWarning($"[QuestTeleportTrigger] '{name}': Đang chờ QuestManager... ({elapsed:F1}s)");

            yield return null; // thử lại frame sau
        }

        Debug.Log($"[QuestTeleportTrigger] '{name}': QuestManager sẵn sàng sau {elapsed:F2}s.");
        Subscribe();
    }

    private void Subscribe()
    {
        QuestManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);
        Debug.Log($"[QuestTeleportTrigger] '{name}' subscribed — watching questId='{questId}' stepIndex={stepIndex}");
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnStepCompleted.RemoveListener(OnStepCompleted);
    }

    // ── Event Handler ─────────────────────────────────────────────────────────

    private void OnStepCompleted(QuestStep completedStep)
    {
        if (_triggered) return;

        // Kiểm tra đúng quest + step mà trigger này quan tâm
        var qm = QuestManager.Instance;
        if (qm == null || qm.CurrentQuest == null) return;

        // OnStepCompleted fired ngay khi step vừa done — lúc này CurrentQuest vẫn đúng
        // nhưng CurrentStepIndex đã tăng lên step kế. Nên ta check step VỪA XONG = stepIndex.
        // QuestManager cần expose PreviousStepIndex hoặc ta truyền index qua event.
        // Cách an toàn nhất: so sánh questId + completedStep object với step trong QuestData.
        if (qm.CurrentQuest.questId != questId) return;

        // Tìm index của completedStep trong QuestData.steps[]
        var steps = qm.CurrentQuest.steps;
        int completedIndex = -1;
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] == completedStep) { completedIndex = i; break; }
        }

        if (completedIndex != stepIndex) return;

        _triggered = true;
        Debug.Log($"[QuestTeleportTrigger] '{name}': Step {stepIndex} of '{questId}' completed — starting teleport flow.");
        // Dùng CoroutineRunner để coroutine không bị kill khi GameObject này bị deactivate
        CoroutineRunner.Instance.Run(TeleportFlow());
    }

    // ── Teleport Flow ─────────────────────────────────────────────────────────

    private IEnumerator TeleportFlow()
    {
        // 1. Chờ dialogue kết thúc
        float elapsed = 0f;
        while (elapsed < dialogueWaitTimeout)
        {
            bool dialogueActive = DialogueBubbleUI.Instance != null && DialogueBubbleUI.Instance.IsShowing;
            if (!dialogueActive) break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= dialogueWaitTimeout)
            Debug.LogWarning($"[QuestTeleportTrigger] '{name}': Dialogue wait timeout ({dialogueWaitTimeout}s) — teleporting anyway.");

        // 2. Phát Timeline (nếu bật useTimeline và có gán teleportTimeline)
        if (useTimeline && teleportTimeline != null)
        {
            // Kích hoạt toàn bộ chain cha-con — không chỉ riêng object chứa Timeline.
            // Nếu Timeline nằm trong 1 container cha đang bị tắt, chỉ SetActive
            // object con thôi sẽ không đủ (activeInHierarchy vẫn false → Play() không chạy).
            ActivateHierarchy(teleportTimeline.transform);
            TimelinePlaybackManager.BeginTimeline();
            teleportTimeline.Play();
            Debug.Log($"[QuestTeleportTrigger] '{name}': Playing timeline '{teleportTimeline.name}'...");

            if (waitForTimeline)
            {
                yield return null; // chờ 1 frame để Timeline kịp khởi động

                float timelineTimeout = (float)teleportTimeline.duration + 5f;
                float timelineElapsed = 0f;

                while (teleportTimeline.state == PlayState.Playing && timelineElapsed < timelineTimeout)
                {
                    timelineElapsed += Time.deltaTime;
                    yield return null;
                }

                if (timelineElapsed >= timelineTimeout)
                    Debug.LogWarning($"[QuestTeleportTrigger] '{name}': Timeline wait timeout — tiếp tục teleport.");
                else
                    Debug.Log($"[QuestTeleportTrigger] '{name}': Timeline finished.");
            }

            // Timeline chạy xong — báo TimelinePlaybackManager để nhân vật/UI được mở khoá
            TimelinePlaybackManager.EndTimeline();

            // Nếu sắp chuyển scene + bật keepBlackUntilSceneLoaded: tạo màn đen
            // NGAY TRƯỚC khi Stop/Destroy timeline, để che khoảng "map lộ + timeline tắt"
            // và khoảng "timeline tắt → scene mới load xong". Scene mới sẽ tự clear
            // trong BeforeSceneLoad (xem dưới).
            bool needBlackScreen = useSceneTransition && keepBlackUntilSceneLoaded;
            if (needBlackScreen) CreateBlackScreen();

            // Dừng timeline, khôi phục hierarchy, xoá object Timeline
            teleportTimeline.Stop();
            teleportTimeline.time = 0;
            teleportTimeline.Evaluate();

            var timelineGO = teleportTimeline.gameObject;
            RestoreActivatedHierarchy();
            Destroy(timelineGO);
        }

        // 3. Teleport
        if (useSceneTransition)
            yield return StartCoroutine(TeleportToScene());
        else
        {
            // Tele trong cùng scene — nếu lỡ tạo black screen thì clear ngay
            ClearBlackScreen();
            TeleportSameScene();
        }
    }

    // ── Scene Fade (giữ màn hình đen khi chuyển scene) ───────────────────────

    /// <summary>
    /// Tạo fullscreen black image phía trên cùng mọi UI/Camera để giữ màn hình đen.
    /// KHÔNG tự huỷ — gọi ClearBlackScreen() khi scene mới đã load xong và muốn fade-in.
    /// Lưu qua DontDestroyOnLoad để sống xuyên scene transition.
    /// </summary>
    private void CreateBlackScreen()
    {
        if (_blackScreenGO != null) return;

        // Tạo Canvas riêng với sortingOrder cực cao
        var canvasGO = new GameObject("QuestTeleport_BlackScreen");
        Object.DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        // CanvasScaler/GraphicRaycaster không cần — ta chỉ vẽ ảnh đen
        var imgGO = new GameObject("Black");
        imgGO.transform.SetParent(canvasGO.transform, false);

        var rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = imgGO.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        _blackScreenGO = canvasGO;
    }

    /// <summary>Huỷ màn đen (gọi từ scene mới sau khi load xong).</summary>
    public static void ClearBlackScreen()
    {
        if (_blackScreenGO != null)
        {
            Object.Destroy(_blackScreenGO);
            _blackScreenGO = null;
        }
    }

    private static GameObject _blackScreenGO;

    // ── Teleport Implementations ──────────────────────────────────────────────

    /// <summary>Teleport trong cùng scene — dịch chuyển thẳng transform player.</summary>
    private void TeleportSameScene()
    {
        Transform player = ResolvePlayer();
        if (player == null)
        {
            Debug.LogError($"[QuestTeleportTrigger] '{name}': Không tìm được player transform — teleport thất bại.");
            return;
        }
        if (destinationTransform == null)
        {
            Debug.LogError($"[QuestTeleportTrigger] '{name}': destinationTransform chưa gán — teleport thất bại.");
            return;
        }

        // Tắt CharacterController tạm (nếu có) để tránh Unity block teleport
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.SetPositionAndRotation(destinationTransform.position, destinationTransform.rotation);

        if (cc != null) cc.enabled = true;

        Debug.Log($"[QuestTeleportTrigger] '{name}': Teleported player to {destinationTransform.position} (same scene).");
    }

    /// <summary>
    /// Lưu spawn point qua PlayerPrefs rồi load scene mới.
    /// Scene đích cần có script đọc PlayerPrefs key này và spawn player đúng vị trí.
    /// </summary>
    private IEnumerator TeleportToScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"[QuestTeleportTrigger] '{name}': targetSceneName trống — không thể load scene.");
            yield break;
        }

        // Lưu spawn destination để scene mới đọc
        if (destinationTransform != null)
        {
            Vector3 p = destinationTransform.position;
            PlayerPrefs.SetFloat(spawnPosKeyPrefix + "X", p.x);
            PlayerPrefs.SetFloat(spawnPosKeyPrefix + "Y", p.y);
            PlayerPrefs.SetFloat(spawnPosKeyPrefix + "Z", p.z);
            PlayerPrefs.SetFloat(spawnPosKeyPrefix + "RotY", destinationTransform.eulerAngles.y);
            PlayerPrefs.Save();
            Debug.Log($"[QuestTeleportTrigger] '{name}': Saved spawn point {p} for scene '{targetSceneName}'.");
        }

        Debug.Log($"[QuestTeleportTrigger] '{name}': Loading scene '{targetSceneName}'...");
        yield return SceneManager.LoadSceneAsync(targetSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private readonly System.Collections.Generic.List<GameObject> _activatedByUs =
        new System.Collections.Generic.List<GameObject>();

    /// <summary>
    /// Kích hoạt (SetActive true) toàn bộ chuỗi object cha lên tới root.
    /// Cần thiết vì Timeline object có thể nằm trong 1 container cha đang bị tắt —
    /// nếu chỉ SetActive riêng object con thì activeInHierarchy vẫn false.
    /// Ghi nhớ những object mình chủ động bật để sau đó tắt lại đúng những object đó.
    /// </summary>
    private void ActivateHierarchy(Transform t)
    {
        _activatedByUs.Clear();
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
                _activatedByUs.Add(t.gameObject);
            }
            t = t.parent;
        }
    }

    /// <summary>Tắt lại các object cha đã bật tạm ở ActivateHierarchy (khôi phục trạng thái ẩn).</summary>
    private void RestoreActivatedHierarchy()
    {
        foreach (var go in _activatedByUs)
        {
            if (go != null) go.SetActive(false);
        }
        _activatedByUs.Clear();
    }

    private Transform ResolvePlayer()
    {
        if (playerTransform != null) return playerTransform;

        // Thử lấy từ MinimapController
        if (MinimapController.Instance != null && MinimapController.Instance.Player != null)
            return MinimapController.Instance.Player;

        // Fallback: tìm qua tag
        var go = GameObject.FindWithTag("Player");
        if (go != null) return go.transform;

        return null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi thủ công để kick teleport flow ngay lập tức (không cần chờ quest event).
    /// Dùng cho debug hoặc trigger từ script bên ngoài.
    /// </summary>
    public void TriggerManually()
    {
        if (_triggered) { Debug.LogWarning($"[QuestTeleportTrigger] '{name}': Đã trigger rồi, bỏ qua."); return; }
        _triggered = true;
        CoroutineRunner.Instance.Run(TeleportFlow());
    }

    /// <summary>Reset để trigger có thể kích hoạt lại (dùng khi test).</summary>
    public void ResetTrigger() => _triggered = false;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (destinationTransform == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(destinationTransform.position, 0.4f);
        Gizmos.DrawLine(transform.position, destinationTransform.position);
        UnityEditor.Handles.Label(
            destinationTransform.position + Vector3.up * 0.6f,
            $"Tele Dest\nQuest: {questId} / Step: {stepIndex}");
    }
#endif
}