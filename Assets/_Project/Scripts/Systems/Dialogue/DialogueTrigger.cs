using UnityEngine;
using System.Collections;

[System.Serializable]
public class DialogueEntry
{
    public string questId;
    public int requiredStepIndex = -1;
    public bool requiredCompleted;
    public DialogueLineData[] lines;
    public bool playOnce = true;
    public GameObject customPrompt;
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Trigger Identity")]
    public string triggerID;

    [Header("Dialogue Entries")]
    public DialogueEntry[] dialogueEntries;

    [Header("Fallback Dialogue")]
    public DialogueLineData[] defaultLines;

    [Header("Visual")]
    public GameObject interactionPrompt;

    [Header("Black Screen")]
    public bool useBlackScreen = false;
    public bool useBlackScreenOnEnd = false;
    public float blackScreenDelay = 0.3f;

    [Header("Teleport")]
    public bool teleportPlayer = false;
    public Vector3 targetPlayerPosition;

    [Header("Dialogue Camera")]
    public bool switchCamera = false;
    [HideInInspector] public GameObject mainCameraObject;
    [HideInInspector] public GameObject dialogueCameraObject;
    public Transform dialogueCameraPoint;
    public Transform leftCameraPoint;
    public Transform rightCameraPoint;
    public Transform leftPlayerPoint;
    public Transform rightPlayerPoint;

    [Header("Hide on Complete")]
    [Tooltip("Nếu true, NPC (hoặc targetToHide) sẽ bị ẩn sau khi dialogue hoàn thành.")]
    public bool hideOnDialogueComplete = false;
    [Tooltip("Đối tượng cần ẩn. Để trống sẽ ẩn chính GameObject này.")]
    public GameObject targetToHide;
    [Tooltip("Delay (giây) trước khi ẩn, tính từ lúc dialogue hoàn thành.")]
    public float hideDelay = 0f;

    private InteractionSide _currentSide = InteractionSide.Left;
    private Vector3 _originalMainCamPos;
    private Quaternion _originalMainCamRot;
    private bool _playerInRange;
    private bool _hasPlayedForCurrentEntry;
    private bool _isPlaying;
    private Camera _mainCamera;
    private DialogueEntry _currentEntry;
    private bool sequential = true;
    private NPCInteraction _npcInteraction;

    // Lưu trạng thái gốc
    private bool _originalPlayerFacingRight;
    private Vector3 _originalNPCScale;
    private bool _scalesSaved = false;

    // Khóa di chuyển
    private bool _movementLocked = false;
    private bool _isDialogueTriggered = false;

    // Tham chiếu component
    private MonoBehaviour _playerMovementScript;
    private CharacterController _playerCharacterController;
    private Rigidbody _playerRigidbody;
    private HSRPlayerController _hsrController;

    // Cờ để tránh gọi quest nhiều lần
    private bool _questNotified = false;

    // ── Tìm main camera và dialogue camera ──────────────────
    private const string DIALOGUE_CAMERA_TAG = "DialogueCamera";

    void Start()
    {
        // Tự tìm main camera (ưu tiên object có MainCameraIdentifier)
        if (mainCameraObject == null)
        {
            mainCameraObject = FindMainCamera();
            if (mainCameraObject != null)
            {
                _originalMainCamPos = mainCameraObject.transform.position;
                _originalMainCamRot = mainCameraObject.transform.rotation;
                Debug.Log("[DialogueTrigger] Tự tìm thấy Main Camera.");
            }
            else
            {
                Debug.LogWarning("[DialogueTrigger] Không tìm thấy Main Camera! Hãy gắn MainCameraIdentifier vào camera chính.");
            }
        }

        // Tự tìm Dialogue Camera nếu chưa gán
        if (dialogueCameraObject == null)
        {
            FindDialogueCamera();
        }

        if (switchCamera)
        {
            if (dialogueCameraObject == null)
            {
                Debug.LogWarning("[DialogueTrigger] Bật switchCamera nhưng chưa có Dialogue Camera. Vui lòng tạo camera với tag 'DialogueCamera' hoặc component DialogueCamera.");
            }
            else
            {
                if (mainCameraObject != null)
                {
                    _originalMainCamPos = mainCameraObject.transform.position;
                    _originalMainCamRot = mainCameraObject.transform.rotation;
                }
            }
        }

        _npcInteraction = GetComponent<NPCInteraction>();
        FindPlayerComponents();

        // Nếu targetToHide chưa được gán, mặc định ẩn chính GameObject này
        if (targetToHide == null)
            targetToHide = gameObject;
    }

    private GameObject FindMainCamera()
    {
        // 1. Tìm object có component MainCameraIdentifier
        MainCameraIdentifier identifier = FindFirstObjectByType<MainCameraIdentifier>();
        if (identifier != null)
        {
            return identifier.gameObject;
        }

        // 2. Fallback: dùng Camera.main
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.gameObject;
        }

        // 3. Fallback cuối: tìm bất kỳ camera nào đang active
        Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in allCams)
        {
            if (c.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[DialogueTrigger] Không tìm thấy MainCameraIdentifier, dùng camera active đầu tiên làm fallback.");
                return c.gameObject;
            }
        }

        return null;
    }

    private void FindDialogueCamera()
    {
        if (dialogueCameraObject != null) return;

        // Cách 1: Tìm theo tag
        GameObject tagCam = GameObject.FindGameObjectWithTag(DIALOGUE_CAMERA_TAG);
        if (tagCam != null)
        {
            dialogueCameraObject = tagCam;
            Debug.Log("[DialogueTrigger] Tự tìm thấy Dialogue Camera (tag).");
            return;
        }

        // Cách 2: Tìm component DialogueCamera (script rỗng)
        DialogueCamera camComp = FindFirstObjectByType<DialogueCamera>(FindObjectsInactive.Include);
        if (camComp != null)
        {
            dialogueCameraObject = camComp.gameObject;
            Debug.Log("[DialogueTrigger] Tự tìm thấy Dialogue Camera (component).");
            return;
        }

        Debug.LogWarning("[DialogueTrigger] Không tìm thấy Dialogue Camera. Hãy gán tag 'DialogueCamera' hoặc thêm component DialogueCamera.");
    }

    private void FindPlayerComponents()
    {
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null)
        {
            Debug.LogWarning("[DialogueTrigger] Player not found.");
            return;
        }

        _playerCharacterController = player.GetComponent<CharacterController>();
        _playerRigidbody = player.GetComponent<Rigidbody>();
        _hsrController = player.GetComponent<HSRPlayerController>();

        if (PlayerManager.Instance != null && PlayerManager.Instance.playerMovementScript != null)
        {
            _playerMovementScript = PlayerManager.Instance.playerMovementScript;
            Debug.Log($"[DialogueTrigger] Movement script from PlayerManager: {_playerMovementScript.GetType().Name}");
            return;
        }

        // Fallback: tự tìm
        var scripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script == null || !script.enabled) continue;
            string typeName = script.GetType().Name;
            if (typeName.Contains("Controller") || typeName.Contains("Movement") || typeName.Contains("Input") || typeName.Contains("Player"))
            {
                _playerMovementScript = script;
                Debug.Log($"[DialogueTrigger] Movement script found: {typeName}");
                break;
            }
        }

        if (_playerMovementScript == null)
            Debug.LogWarning("[DialogueTrigger] Could not find player movement script.");
    }

    void Update()
    {
        if (_isPlaying && !_movementLocked)
            StopPlayerImmediately();
    }

    private DialogueEntry GetAppropriateEntry()
    {
        if (dialogueEntries == null || dialogueEntries.Length == 0) return null;
        var qm = QuestManager.Instance;
        var currentQuest = qm?.CurrentQuest;

        foreach (var entry in dialogueEntries)
        {
            if (!string.IsNullOrEmpty(entry.questId))
            {
                if (currentQuest == null) continue;
                if (currentQuest.questId != entry.questId) continue;
            }

            if (entry.requiredStepIndex >= 0)
            {
                if (qm == null || currentQuest == null) continue;
                if (entry.requiredStepIndex >= currentQuest.steps.Length) continue;

                bool stepCompleted = currentQuest.steps[entry.requiredStepIndex].isCompleted;
                if (entry.requiredCompleted != stepCompleted) continue;

                if (!entry.requiredCompleted && qm.CurrentStepIndex != entry.requiredStepIndex)
                    continue;
            }

            if (_currentEntry != entry)
            {
                _hasPlayedForCurrentEntry = false;
                _currentEntry = entry;
            }
            return entry;
        }
        return null;
    }

    public GameObject GetCurrentPrompt()
    {
        var entry = GetAppropriateEntry();
        return entry != null ? entry.customPrompt : null;
    }

    private InteractionSide DetermineInteractionSide()
    {
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return InteractionSide.Left;
        float diff = player.transform.position.x - transform.position.x;
        return diff < 0 ? InteractionSide.Left : InteractionSide.Right;
    }

    public void PlayDialogueAuto()
    {
        if (_isPlaying || _isDialogueTriggered) return;

        if (string.IsNullOrEmpty(triggerID))
        {
            Debug.LogWarning("[DialogueTrigger] triggerID rỗng.");
            return;
        }

        _currentSide = DetermineInteractionSide();
        var entry = GetAppropriateEntry();

        if (entry == null)
        {
            if (defaultLines != null && defaultLines.Length > 0)
            {
                StopPlayerImmediately();
                _isDialogueTriggered = true;
                StartDialogue(defaultLines);
                return;
            }
            return;
        }

        if (entry.playOnce && _hasPlayedForCurrentEntry)
            return;

        StopPlayerImmediately();
        _isDialogueTriggered = true;
        _hasPlayedForCurrentEntry = true;
        _currentEntry = entry;

        if (_npcInteraction == null && interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (useBlackScreen)
            StartCoroutine(PlayWithBlackScreenTransition(entry.lines));
        else
            StartDialogue(entry.lines);
    }

    // ==================== PLAYER STOP ====================

    private void StopPlayerImmediately()
    {
        if (_movementLocked) return;

        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        if (_playerMovementScript != null && _playerMovementScript.enabled)
        {
            _playerMovementScript.enabled = false;
            Debug.Log("[DialogueTrigger] Disabled movement script.");
        }

        if (_playerCharacterController != null && _playerCharacterController.enabled)
        {
            _playerCharacterController.enabled = false;
            Debug.Log("[DialogueTrigger] Disabled CharacterController.");
        }

        if (_playerRigidbody != null)
        {
            _playerRigidbody.linearVelocity = Vector3.zero;
            _playerRigidbody.angularVelocity = Vector3.zero;
        }

        if (_hsrController != null)
            _hsrController.ResetToIdle();

        _movementLocked = true;
    }

    private void UnlockPlayerMovement()
    {
        if (!_movementLocked) return;

        if (_playerMovementScript != null && !_playerMovementScript.enabled)
        {
            _playerMovementScript.enabled = true;
            Debug.Log("[DialogueTrigger] Re-enabled movement script.");
        }

        if (_playerCharacterController != null && !_playerCharacterController.enabled)
        {
            _playerCharacterController.enabled = true;
            Debug.Log("[DialogueTrigger] Re-enabled CharacterController.");
        }

        _movementLocked = false;
    }

    // ==================== TELEPORT & CAMERA ====================

    private Vector3? GetTeleportTarget()
    {
        if (!teleportPlayer) return null;

        if (_currentSide == InteractionSide.Left && leftPlayerPoint != null)
            return leftPlayerPoint.position;
        if (_currentSide == InteractionSide.Right && rightPlayerPoint != null)
            return rightPlayerPoint.position;

        return targetPlayerPosition;
    }

    private bool GetCameraPointTarget(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (!switchCamera) return false;

        if (_currentSide == InteractionSide.Left && leftCameraPoint != null)
        {
            position = leftCameraPoint.position;
            rotation = leftCameraPoint.rotation;
            return true;
        }
        if (_currentSide == InteractionSide.Right && rightCameraPoint != null)
        {
            position = rightCameraPoint.position;
            rotation = rightCameraPoint.rotation;
            return true;
        }
        if (dialogueCameraPoint != null)
        {
            position = dialogueCameraPoint.position;
            rotation = dialogueCameraPoint.rotation;
            return true;
        }

        return false;
    }

    private void ApplyTeleportToPosition(Vector3? target)
    {
        if (!teleportPlayer || target == null) return;

        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        bool ccWasEnabled = _playerCharacterController != null && _playerCharacterController.enabled;
        if (ccWasEnabled) _playerCharacterController.enabled = false;

        player.transform.position = target.Value;

        if (_playerCharacterController != null)
        {
            _playerCharacterController.enabled = true;
            _playerCharacterController.Move(Vector3.zero);
            _playerCharacterController.enabled = false;
        }
    }

    private void SwitchToDialogueCameraAtPoint(Vector3 position, Quaternion rotation)
    {
        FindDialogueCamera();

        if (dialogueCameraObject == null)
        {
            Debug.LogWarning("[DialogueTrigger] Không có Dialogue Camera, bỏ qua chuyển đổi.");
            return;
        }

        if (mainCameraObject != null)
        {
            _originalMainCamPos = mainCameraObject.transform.position;
            _originalMainCamRot = mainCameraObject.transform.rotation;
            mainCameraObject.SetActive(false);
        }

        Transform rig = dialogueCameraObject.transform.parent;
        if (rig != null)
            rig.SetPositionAndRotation(position, rotation);

        dialogueCameraObject.SetActive(true);
    }

    // ==================== DIALOGUE FLOW ====================

    private IEnumerator PlayWithBlackScreenTransition(DialogueLineData[] lines)
    {
        Vector3? teleportTarget = GetTeleportTarget();
        bool hasCamPoint = GetCameraPointTarget(out Vector3 camPos, out Quaternion camRot);

        yield return FadeController.Instance.FadeToBlack();

        ApplyCharacterFlips();
        ApplyTeleportToPosition(teleportTarget);
        if (hasCamPoint) SwitchToDialogueCameraAtPoint(camPos, camRot);

        yield return new WaitForSeconds(blackScreenDelay);
        yield return FadeController.Instance.FadeFromBlack();
        StartDialogueInternal(lines);
    }

    private void StartDialogue(DialogueLineData[] lines)
    {
        Vector3? teleportTarget = GetTeleportTarget();
        bool hasCamPoint = GetCameraPointTarget(out Vector3 camPos, out Quaternion camRot);

        ApplyCharacterFlips();
        ApplyTeleportToPosition(teleportTarget);
        if (hasCamPoint) SwitchToDialogueCameraAtPoint(camPos, camRot);
        StartDialogueInternal(lines);
    }

    private void StartDialogueInternal(DialogueLineData[] lines)
    {
        _isPlaying = true;
        _questNotified = false;

        if (_npcInteraction == null && interactionPrompt != null)
            interactionPrompt.SetActive(false);

        Transform playerTarget = null;
        if (PlayerManager.Instance?.GetPlayer() != null)
            playerTarget = PlayerManager.Instance.GetPlayer().transform;
        else
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) playerTarget = playerGO.transform;
        }

        if (sequential)
        {
            DialogueBubbleUI.Instance.ShowSequential(
                lines,
                transform,
                playerTarget,
                OnDialogueComplete,
                0,
                _currentSide
            );
        }
        else
        {
            DialogueBubbleUI.Instance.Show(
                lines[0],
                transform,
                playerTarget,
                _currentSide,
                OnDialogueComplete
            );
        }
    }

    // ==================== COMPLETE ====================

    private void OnDialogueComplete()
    {
        _isPlaying = false;
        _isDialogueTriggered = false;
        _questNotified = false;

        if (useBlackScreenOnEnd)
        {
            StartCoroutine(EndWithBlackScreenAndNotify());
        }
        else
        {
            // Không black screen, thực hiện ngay
            RestoreOriginalScales();
            RestoreCameraState();
            UnlockPlayerMovement();
            NotifyQuestIfNeeded();
            InvokeNPCInteractionComplete();
            // Xử lý ẩn sau khi đã hoàn tất mọi thứ (có thể delay)
            StartCoroutine(HideAfterDelayIfNeeded());
        }
    }

    private IEnumerator EndWithBlackScreenAndNotify()
    {
        yield return FadeController.Instance.FadeToBlack();

        RestoreOriginalScales();
        RestoreCameraState();

        yield return new WaitForSeconds(blackScreenDelay);
        yield return FadeController.Instance.FadeFromBlack();

        UnlockPlayerMovement();
        NotifyQuestIfNeeded();
        InvokeNPCInteractionComplete();

        // Ẩn sau khi đã hoàn tất mọi thứ (có delay)
        yield return StartCoroutine(HideAfterDelayIfNeeded());
    }

    private IEnumerator HideAfterDelayIfNeeded()
    {
        if (!hideOnDialogueComplete || targetToHide == null) yield break;

        if (hideDelay > 0f)
            yield return new WaitForSeconds(hideDelay);

        if (targetToHide != null && targetToHide.activeSelf)
        {
            targetToHide.SetActive(false);
            Debug.Log($"[DialogueTrigger] Ẩn {targetToHide.name} sau khi dialogue hoàn thành.");
        }
    }

    private void NotifyQuestIfNeeded()
    {
        if (!_questNotified && QuestManager.Instance != null && !string.IsNullOrEmpty(triggerID))
        {
            _questNotified = true;
            QuestManager.Instance.OnDialogueEnded(triggerID);
            Debug.Log($"[DialogueTrigger] Notified QuestManager for triggerID: {triggerID}");
        }
    }

    private void InvokeNPCInteractionComplete()
    {
        if (_npcInteraction != null)
        {
            _npcInteraction.OnDialogueComplete();
        }
        else if (_playerInRange)
        {
            var entry = GetAppropriateEntry();
            if (entry != null && (!entry.playOnce || !_hasPlayedForCurrentEntry))
            {
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
            }
        }
    }

    // ==================== FLIP LOGIC ====================

    private void ApplyCharacterFlips()
    {
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        var playerController = player.GetComponent<HSRPlayerController>();

        if (!_scalesSaved)
        {
            _originalPlayerFacingRight = playerController != null
                ? playerController.IsFacingRight()
                : true;
            _originalNPCScale = transform.localScale;
            _scalesSaved = true;
        }

        if (_currentSide == InteractionSide.Left)
        {
            if (playerController != null)
                playerController.SetFacingDirection(true);

            transform.localScale = new Vector3(
                Mathf.Abs(_originalNPCScale.x) * (_originalNPCScale.x >= 0 ? -1f : 1f),
                _originalNPCScale.y,
                _originalNPCScale.z
            );
        }
        else
        {
            if (playerController != null)
                playerController.SetFacingDirection(false);

            transform.localScale = _originalNPCScale;
        }
    }

    private void RestoreOriginalScales()
    {
        if (!_scalesSaved) return;

        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player != null)
        {
            var playerController = player.GetComponent<HSRPlayerController>();
            if (playerController != null)
                playerController.SetFacingDirection(_originalPlayerFacingRight);
        }

        transform.localScale = _originalNPCScale;
        _scalesSaved = false;
    }

    private void RestoreCameraState()
    {
        if (!switchCamera) return;
        if (dialogueCameraObject != null) dialogueCameraObject.SetActive(false);
        if (mainCameraObject != null)
        {
            mainCameraObject.SetActive(true);
            mainCameraObject.transform.SetPositionAndRotation(_originalMainCamPos, _originalMainCamRot);
        }
        else
        {
            // Fallback: tìm lại main camera nếu đã bị mất tham chiếu
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.gameObject.SetActive(true);
                mainCam.transform.SetPositionAndRotation(_originalMainCamPos, _originalMainCamRot);
            }
        }
    }

    // ==================== TRIGGER EVENTS ====================

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (_npcInteraction != null) return;

        var entry = GetAppropriateEntry();
        if (entry != null && (!entry.playOnce || !_hasPlayedForCurrentEntry) && !_isPlaying && !_isDialogueTriggered)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (_npcInteraction != null) return;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void OnDestroy()
    {
        RestoreOriginalScales();
        UnlockPlayerMovement();
    }

    private void OnDisable()
    {
        RestoreOriginalScales();
        UnlockPlayerMovement();
    }
}