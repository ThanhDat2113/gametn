using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Gắn lên cùng GameObject với QuestMarkerBridge (hoặc NPC bất kỳ).
/// Tự động theo dõi khi đúng quest/step hoàn thành + dialogue kết thúc,
/// sau đó phát 1 Timeline (cutscene, hiệu ứng...) rồi DỪNG LẠI — không teleport.
///
/// FLOW:
///   OnStepCompleted (QuestManager) đúng questId/stepIndex
///     → chờ DialogueBubbleUI.IsShowing == false
///       → bật object chứa Timeline lên → phát Timeline → xoá object chứa Timeline
///
/// SETUP NHANH:
///   1. Gắn component này lên NPC/object có QuestMarkerBridge.
///   2. Điền questId + stepIndex khớp với bridge tương ứng.
///   3. Kéo PlayableDirector vào field timeline (object chứa nó nên để mặc định TẮT
///      trong scene — script sẽ tự bật lên khi cần rồi xoá sau khi chạy xong).
/// </summary>
public class QuestTimelineTrigger : MonoBehaviour
{
    [Header("Quest Binding — phải khớp với QuestMarkerBridge trên cùng NPC")]
    [Tooltip("questId của QuestData asset mà trigger này theo dõi.")]
    [SerializeField] private string questId;

    [Tooltip("Index của step trong QuestData.steps[] mà trigger này phản ứng (bắt đầu từ 0).")]
    [SerializeField] private int stepIndex;

    [Header("Timeline")]
    [Tooltip("PlayableDirector sẽ phát khi step hoàn thành. Object chứa nó nên để mặc định TẮT " +
             "trong scene — script sẽ tự bật lên (kể cả object cha nếu đang tắt), phát Timeline, " +
             "rồi xoá hẳn object này sau khi chạy xong.")]
    [SerializeField] private PlayableDirector timeline;

    [Tooltip("Nếu bật: chờ Timeline phát xong rồi mới xoá object. " +
             "Nếu tắt: xoá object ngay khi Timeline vừa bắt đầu.")]
    [SerializeField] private bool waitForTimeline = true;

    [Header("Hide While Timeline Playing")]
    [Tooltip("Các GameObject sẽ tự ẩn ngay trước khi Timeline chạy (vd: quest marker, HUD, minimap, " +
             "NPC thật nếu Timeline có diễn viên cắt cảnh riêng...), và tự hiện lại ngay sau khi " +
             "Timeline chạy xong. Object nào đang tắt sẵn từ trước thì giữ nguyên tắt, không bị tự bật lại.")]
    [SerializeField] private GameObject[] objectsToHideDuringTimeline;

    [Tooltip("Tự động tìm và ẩn Player thật trong lúc Timeline chạy (chỉ tắt các Renderer, KHÔNG " +
             "SetActive cả GameObject để không ảnh hưởng script di chuyển/camera con gắn trên Player). " +
             "Cần bật cái này thay vì kéo tay vào objectsToHideDuringTimeline vì Player thường nằm ở " +
             "Persistent Scene khác, không thể assign chéo scene qua Inspector.")]
    [SerializeField] private bool hidePlayerRenderersDuringTimeline = true;

    [Header("Camera Handling")]
    [Tooltip("Camera dùng bên trong Timeline này (nếu Timeline có Camera Track riêng). " +
             "Trước khi Play, script sẽ tự TẮT tất cả Camera khác đang active trong toàn bộ scene " +
             "đã load (kể cả camera của Player nếu nó ở scene khác), tránh 2 camera cùng active " +
             "đè lên nhau. Sau khi Timeline xong sẽ bật lại đúng những camera đã tắt.")]
    [SerializeField] private Camera timelineCamera;

    [Header("Dialogue Wait")]
    [Tooltip("Thời gian tối đa chờ dialogue kết thúc (giây) trước khi bỏ qua và chạy Timeline luôn.")]
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
                Debug.LogError($"[QuestTimelineTrigger] '{name}': QuestManager.Instance vẫn NULL sau {giveUpAfter}s — trigger sẽ không hoạt động. " +
                               "Kiểm tra lại thứ tự load scene hoặc QuestManager có được khởi tạo không.");
                yield break;
            }

            if (elapsed >= warnAfter && elapsed - Time.unscaledDeltaTime < warnAfter)
                Debug.LogWarning($"[QuestTimelineTrigger] '{name}': Đang chờ QuestManager... ({elapsed:F1}s)");

            yield return null; // thử lại frame sau
        }

        Debug.Log($"[QuestTimelineTrigger] '{name}': QuestManager sẵn sàng sau {elapsed:F2}s.");
        Subscribe();
    }

    private void Subscribe()
    {
        QuestManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);
        Debug.Log($"[QuestTimelineTrigger] '{name}' subscribed — watching questId='{questId}' stepIndex={stepIndex}");
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
        Debug.Log($"[QuestTimelineTrigger] '{name}': Step {stepIndex} of '{questId}' completed — starting timeline flow.");
        // Dùng CoroutineRunner để coroutine không bị kill khi GameObject này bị deactivate
        CoroutineRunner.Instance.Run(TimelineFlow());
    }

    // ── Timeline Flow ─────────────────────────────────────────────────────────

    private IEnumerator TimelineFlow()
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
            Debug.LogWarning($"[QuestTimelineTrigger] '{name}': Dialogue wait timeout ({dialogueWaitTimeout}s) — chạy timeline luôn.");

        // 2. Phát Timeline (nếu có)
        if (timeline == null)
        {
            Debug.LogWarning($"[QuestTimelineTrigger] '{name}': Chưa gán timeline — không có gì để chạy.");
            yield break;
        }

        // Kích hoạt toàn bộ chain cha-con — không chỉ riêng object chứa Timeline.
        // Nếu Timeline nằm trong 1 container cha đang bị tắt, chỉ SetActive
        // object con thôi sẽ không đủ (activeInHierarchy vẫn false → Play() không chạy).
        ActivateHierarchy(timeline.transform);

        // Ẩn các object được cấu hình (quest marker, HUD...) ngay trước khi Timeline chạy
        HideObjectsBeforeTimeline();
        HidePlayerRenderers();

        // Tắt hết camera khác đang active (kể cả camera Player ở scene khác) để tránh
        // xung đột render/AudioListener với camera của Timeline
        // Chỉ tắt camera khác nếu đã gán timelineCamera — nếu để trống, không biết
        // camera nào cần giữ lại nên KHÔNG tắt gì cả (tránh tắt sạch mọi camera
        // trong scene → "No cameras rendering").
        if (timelineCamera != null)
        {
            timelineCamera.enabled = true;
            DisableOtherCameras();
        }
        else
        {
            Debug.LogWarning($"[QuestTimelineTrigger] '{name}': timelineCamera chưa được gán — bỏ qua bước tắt camera khác.");
        }

        timeline.Play();
        Debug.Log($"[QuestTimelineTrigger] '{name}': Playing timeline '{timeline.name}'...");

        if (waitForTimeline)
        {
            yield return null; // chờ 1 frame để Timeline kịp khởi động

            float timelineTimeout = (float)timeline.duration + 5f;
            float timelineElapsed = 0f;

            while (timeline.state == PlayState.Playing && timelineElapsed < timelineTimeout)
            {
                timelineElapsed += Time.deltaTime;
                yield return null;
            }

            if (timelineElapsed >= timelineTimeout)
                Debug.LogWarning($"[QuestTimelineTrigger] '{name}': Timeline wait timeout — dừng lại luôn.");
            else
                Debug.Log($"[QuestTimelineTrigger] '{name}': Timeline finished.");
        }

        // Timeline chạy xong — dừng, tắt lại object cha về trạng thái ẩn ban đầu,
        // rồi xoá hẳn object chứa Timeline (dùng 1 lần, không cần giữ lại).
        timeline.Stop();
        timeline.time = 0;
        timeline.Evaluate();

        var timelineGO = timeline.gameObject;
        RestoreActivatedHierarchy();
        Destroy(timelineGO);

        // Bật lại các camera khác đã tắt tạm
        RestoreOtherCameras();

        // Hiện lại các object đã ẩn trước đó
        RestoreHiddenObjects();
        RestorePlayerRenderers();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private readonly System.Collections.Generic.List<Camera> _disabledCameras =
        new System.Collections.Generic.List<Camera>();
    private readonly System.Collections.Generic.List<AudioListener> _disabledListeners =
        new System.Collections.Generic.List<AudioListener>();

    /// <summary>
    /// Tắt mọi Camera đang active trong TẤT CẢ scene đã load (kể cả scene khác với
    /// scene chứa Timeline này), ngoại trừ timelineCamera. Cũng tắt AudioListener đi kèm
    /// để tránh warning "2 AudioListeners". Chỉ ghi nhớ những camera/listener thực sự
    /// đang bật để bật lại đúng trạng thái sau khi Timeline xong.
    /// </summary>
    private void DisableOtherCameras()
    {
        _disabledCameras.Clear();
        _disabledListeners.Clear();

        foreach (var cam in Camera.allCameras)
        {
            if (cam == null || cam == timelineCamera) continue;

            if (cam.enabled)
            {
                cam.enabled = false;
                _disabledCameras.Add(cam);
            }

            var listener = cam.GetComponent<AudioListener>();
            if (listener != null && listener.enabled)
            {
                listener.enabled = false;
                _disabledListeners.Add(listener);
            }
        }
    }

    /// <summary>Bật lại đúng những camera/listener đã bị DisableOtherCameras tắt.</summary>
    private void RestoreOtherCameras()
    {
        foreach (var cam in _disabledCameras)
        {
            if (cam != null) cam.enabled = true;
        }
        foreach (var listener in _disabledListeners)
        {
            if (listener != null) listener.enabled = true;
        }
        _disabledCameras.Clear();
        _disabledListeners.Clear();
    }

    private readonly System.Collections.Generic.List<GameObject> _hiddenByUs =
        new System.Collections.Generic.List<GameObject>();

    /// <summary>
    /// Tắt các object trong objectsToHideDuringTimeline. Chỉ ghi nhớ (để bật lại sau)
    /// những object đang active thực sự — object nào vốn đã tắt sẵn thì bỏ qua,
    /// tránh vô tình bật nó lên sau khi Timeline xong.
    /// </summary>
    private void HideObjectsBeforeTimeline()
    {
        _hiddenByUs.Clear();
        if (objectsToHideDuringTimeline == null) return;

        foreach (var go in objectsToHideDuringTimeline)
        {
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
                _hiddenByUs.Add(go);
            }
        }
    }

    /// <summary>Bật lại đúng những object đã bị HideObjectsBeforeTimeline tắt.</summary>
    private void RestoreHiddenObjects()
    {
        foreach (var go in _hiddenByUs)
        {
            if (go != null) go.SetActive(true);
        }
        _hiddenByUs.Clear();
    }

    private readonly System.Collections.Generic.List<Renderer> _hiddenPlayerRenderers =
        new System.Collections.Generic.List<Renderer>();

    /// <summary>
    /// Tắt tất cả Renderer (SpriteRenderer, MeshRenderer...) trên Player và con của nó,
    /// KHÔNG SetActive cả GameObject — tránh ảnh hưởng script di chuyển, CharacterController,
    /// camera con gắn trên Player... Dùng cho trường hợp Player nằm ở scene khác (Persistent
    /// Scene) nên không thể kéo tay vào objectsToHideDuringTimeline.
    /// </summary>
    private void HidePlayerRenderers()
    {
        _hiddenPlayerRenderers.Clear();
        if (!hidePlayerRenderersDuringTimeline) return;

        Transform player = ResolvePlayer();
        if (player == null) return;

        foreach (var r in player.GetComponentsInChildren<Renderer>(true))
        {
            if (r.enabled)
            {
                r.enabled = false;
                _hiddenPlayerRenderers.Add(r);
            }
        }
    }

    /// <summary>Bật lại đúng những Renderer đã bị HidePlayerRenderers tắt.</summary>
    private void RestorePlayerRenderers()
    {
        foreach (var r in _hiddenPlayerRenderers)
        {
            if (r != null) r.enabled = true;
        }
        _hiddenPlayerRenderers.Clear();
    }

    /// <summary>Tìm Player qua MinimapController.Instance.Player, fallback tag "Player".</summary>
    private Transform ResolvePlayer()
    {
        if (MinimapController.Instance != null && MinimapController.Instance.Player != null)
            return MinimapController.Instance.Player;

        var go = GameObject.FindWithTag("Player");
        return go != null ? go.transform : null;
    }

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

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi thủ công để kick timeline flow ngay lập tức (không cần chờ quest event).
    /// Dùng cho debug hoặc trigger từ script bên ngoài.
    /// </summary>
    public void TriggerManually()
    {
        if (_triggered) { Debug.LogWarning($"[QuestTimelineTrigger] '{name}': Đã trigger rồi, bỏ qua."); return; }
        _triggered = true;
        CoroutineRunner.Instance.Run(TimelineFlow());
    }

    /// <summary>Reset để trigger có thể kích hoạt lại (dùng khi test).</summary>
    public void ResetTrigger() => _triggered = false;
}