using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

public enum SideOverrideMode
{
    Auto,       // Tự động xác định dựa trên vị trí player
    ForceLeft,  // Luôn coi như player đứng bên trái NPC
    ForceRight  // Luôn coi như player đứng bên phải NPC
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Trigger Identity")]
    public string triggerID;

    [Header("Dialogue Entries")]
    public DialogueEntry[] dialogueEntries;

    [Header("Fallback Dialogue")]
    public DialogueLineData[] defaultLines;

    [Header("Side Override")]
    [Tooltip("Chọn hướng tương tác cố định. Auto = tự động theo vị trí player, ForceLeft/ForceRight = luôn dùng hướng đó.")]
    public SideOverrideMode sideOverride = SideOverrideMode.Auto;

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
    public bool hideOnDialogueComplete = false;
    public GameObject targetToHide;
    public float hideDelay = 0f;

    // ─── CAMERA & BILLBOARD ────────────────────────────────────────
    private HSRCameraController _cameraController;

    // Offsets cho các điểm con
    private Vector3 _leftCamOffset, _rightCamOffset, _leftPlayerOffset, _rightPlayerOffset;
    private Quaternion _leftCamRot, _rightCamRot, _leftPlayerRot, _rightPlayerRot;

    // ─── STATE ──────────────────────────────────────────────────────
    private InteractionSide _currentSide = InteractionSide.Left;
    private Vector3 _originalMainCamPos;
    private Quaternion _originalMainCamRot;
    private bool _playerInRange;
    private bool _isPlaying;
    private Camera _mainCamera;
    private DialogueEntry _currentEntry;
    private bool sequential = true;
    private NPCInteraction _npcInteraction;

    private bool _originalPlayerFacingRight;
    private Vector3 _originalNPCScale;
    private bool _scalesSaved = false;

    private bool _movementLocked = false;
    private bool _isDialogueTriggered = false;

    private MonoBehaviour _playerMovementScript;
    private CharacterController _playerCharacterController;
    private Rigidbody _playerRigidbody;
    private HSRPlayerController _hsrController;

    private bool _questNotified = false;
    private HashSet<int> _playedEntryIndices = new HashSet<int>();

    private const string DIALOGUE_CAMERA_TAG = "DialogueCamera";

    // ──────────────────────────────────────────────────────────────

    void Start()
    {
        Debug.Log($"[DialogueTrigger] Start on {gameObject.name}, switchCamera={switchCamera}");

        _cameraController = FindObjectOfType<HSRCameraController>();

        if (leftCameraPoint != null)
        {
            _leftCamOffset = leftCameraPoint.position - transform.position;
            _leftCamRot = leftCameraPoint.rotation;
        }
        if (rightCameraPoint != null)
        {
            _rightCamOffset = rightCameraPoint.position - transform.position;
            _rightCamRot = rightCameraPoint.rotation;
        }
        if (leftPlayerPoint != null)
        {
            _leftPlayerOffset = leftPlayerPoint.position - transform.position;
            _leftPlayerRot = leftPlayerPoint.rotation;
        }
        if (rightPlayerPoint != null)
        {
            _rightPlayerOffset = rightPlayerPoint.position - transform.position;
            _rightPlayerRot = rightPlayerPoint.rotation;
        }

        mainCameraObject = FindMainCamera();
        if (mainCameraObject != null)
        {
            _originalMainCamPos = mainCameraObject.transform.position;
            _originalMainCamRot = mainCameraObject.transform.rotation;
            Debug.Log($"[DialogueTrigger] Main Camera found: {mainCameraObject.name} at {_originalMainCamPos}");
        }
        else
        {
            Debug.LogError("[DialogueTrigger] Main Camera NOT found!");
        }

        FindDialogueCamera();
        if (dialogueCameraObject != null)
        {
            Debug.Log($"[DialogueTrigger] Dialogue Camera found: {dialogueCameraObject.name} (active={dialogueCameraObject.activeInHierarchy})");
        }
        else
        {
            Debug.LogError("[DialogueTrigger] Dialogue Camera NOT found!");
        }

        _npcInteraction = GetComponent<NPCInteraction>();
        FindPlayerComponents();

        if (targetToHide == null)
            targetToHide = gameObject;

        if (hideOnDialogueComplete && QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStepChanged.AddListener(OnQuestStepChanged);
            QuestManager.Instance.OnQuestCompleted.AddListener(OnQuestCompleted);
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStepChanged.RemoveListener(OnQuestStepChanged);
            QuestManager.Instance.OnQuestCompleted.RemoveListener(OnQuestCompleted);
        }
        RestoreOriginalScales();
        UnlockPlayerMovement();
    }

    private void OnDisable()
    {
        RestoreOriginalScales();
        UnlockPlayerMovement();
    }

    private void OnQuestStepChanged(QuestStep step) => CheckAndHideIfNeeded();
    private void OnQuestCompleted(QuestData quest) => CheckAndHideIfNeeded();

    private GameObject FindMainCamera()
    {
        MainCameraIdentifier identifier = FindObjectOfType<MainCameraIdentifier>();
        if (identifier != null)
        {
            return identifier.gameObject;
        }

        Camera cam = Camera.main;
        if (cam != null) return cam.gameObject;

        Camera[] allCams = FindObjectsOfType<Camera>();
        foreach (var c in allCams)
            if (c.gameObject.activeInHierarchy) return c.gameObject;

        Debug.LogError("[DialogueTrigger] No Main Camera found!");
        return null;
    }

    private void FindDialogueCamera()
    {
        if (dialogueCameraObject != null) return;

        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(DIALOGUE_CAMERA_TAG);
        foreach (var obj in taggedObjects)
        {
            if (obj != null)
            {
                dialogueCameraObject = obj;
                Debug.Log($"[DialogueTrigger] Found Dialogue Camera via tag: {obj.name} (active={obj.activeInHierarchy})");
                return;
            }
        }

        DialogueCamera[] camComps = FindObjectsOfType<DialogueCamera>(true);
        if (camComps.Length > 0)
        {
            dialogueCameraObject = camComps[0].gameObject;
            Debug.Log($"[DialogueTrigger] Found Dialogue Camera via component: {dialogueCameraObject.name} (active={dialogueCameraObject.activeInHierarchy})");
            return;
        }

        Camera[] allCams = FindObjectsOfType<Camera>(true);
        foreach (var cam in allCams)
        {
            if (cam.gameObject.name.ToLower().Contains("dialogue"))
            {
                dialogueCameraObject = cam.gameObject;
                Debug.Log($"[DialogueTrigger] Found Dialogue Camera by name fallback: {cam.gameObject.name}");
                return;
            }
        }

        Debug.LogError("[DialogueTrigger] No Dialogue Camera found! Please add tag 'DialogueCamera' or component DialogueCamera to a camera.");
    }

    private void FindPlayerComponents()
    {
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        _playerCharacterController = player.GetComponent<CharacterController>();
        _playerRigidbody = player.GetComponent<Rigidbody>();
        _hsrController = player.GetComponent<HSRPlayerController>();

        if (PlayerManager.Instance != null && PlayerManager.Instance.playerMovementScript != null)
        {
            _playerMovementScript = PlayerManager.Instance.playerMovementScript;
            return;
        }

        var scripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script == null || !script.enabled) continue;
            string typeName = script.GetType().Name;
            if (typeName.Contains("Controller") || typeName.Contains("Movement") || typeName.Contains("Input") || typeName.Contains("Player"))
            {
                _playerMovementScript = script;
                break;
            }
        }
    }

    void Update()
    {
        if (_isPlaying && !_movementLocked)
            StopPlayerImmediately();
    }

    // ─── VALIDATION ───────────────────────────────────────────────

    private bool IsEntryValid(DialogueEntry entry)
    {
        if (entry == null) return false;
        var qm = QuestManager.Instance;
        var currentQuest = qm?.CurrentQuest;

        if (!string.IsNullOrEmpty(entry.questId))
        {
            if (currentQuest == null || currentQuest.questId != entry.questId) return false;
        }

        if (entry.requiredStepIndex >= 0)
        {
            if (qm == null || currentQuest == null) return false;
            if (entry.requiredStepIndex >= currentQuest.steps.Length) return false;
            bool stepCompleted = currentQuest.steps[entry.requiredStepIndex].isCompleted;
            if (entry.requiredCompleted != stepCompleted) return false;
            if (!entry.requiredCompleted && qm.CurrentStepIndex != entry.requiredStepIndex) return false;
        }

        return true;
    }

    private List<DialogueEntry> GetAllValidEntries()
    {
        List<DialogueEntry> valid = new List<DialogueEntry>();
        foreach (var entry in dialogueEntries)
            if (IsEntryValid(entry)) valid.Add(entry);
        return valid;
    }

    private bool ShouldHideNow()
    {
        if (!hideOnDialogueComplete) return false;

        var validEntries = GetAllValidEntries();
        if (validEntries.Count == 0) return true;

        foreach (var entry in validEntries)
        {
            int index = System.Array.IndexOf(dialogueEntries, entry);
            if (entry.playOnce && !_playedEntryIndices.Contains(index))
                return false;
        }
        return true;
    }

    private void CheckAndHideIfNeeded()
    {
        if (ShouldHideNow())
        {
            CoroutineRunner.Instance.Run(HideAfterDelayIfNeeded());
        }
    }

    private IEnumerator HideAfterDelayIfNeeded()
    {
        if (!hideOnDialogueComplete || targetToHide == null) yield break;

        if (hideDelay > 0f)
            yield return new WaitForSeconds(hideDelay);

        if (targetToHide != null && targetToHide.activeSelf)
        {
            targetToHide.SetActive(false);
            Debug.Log($"[DialogueTrigger] Ẩn {targetToHide.name} sau khi tất cả dialogue đã hoàn thành.");
        }
    }

    private DialogueEntry GetAppropriateEntry()
    {
        if (dialogueEntries == null || dialogueEntries.Length == 0) return null;
        foreach (var entry in dialogueEntries)
            if (IsEntryValid(entry)) return entry;
        return null;
    }

    public GameObject GetCurrentPrompt()
    {
        var entry = GetAppropriateEntry();
        return entry != null ? entry.customPrompt : null;
    }

    // ─── XÁC ĐỊNH HƯỚNG TƯƠNG TÁC (CÓ GHI ĐÈ) ──────────────────

    private InteractionSide DetermineInteractionSide()
    {
        // Nếu đã chọn ForceLeft hoặc ForceRight thì dùng luôn
        if (sideOverride == SideOverrideMode.ForceLeft) return InteractionSide.Left;
        if (sideOverride == SideOverrideMode.ForceRight) return InteractionSide.Right;

        // Mặc định Auto: tính dựa trên vị trí player so với NPC
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return InteractionSide.Left;
        float diff = player.transform.position.x - transform.position.x;
        return diff < 0 ? InteractionSide.Left : InteractionSide.Right;
    }

    // ─── PLAYER STOP ──────────────────────────────────────────────

    private void StopPlayerImmediately()
    {
        if (_movementLocked) return;

        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        if (_playerMovementScript != null && _playerMovementScript.enabled)
            _playerMovementScript.enabled = false;

        if (_playerCharacterController != null && _playerCharacterController.enabled)
            _playerCharacterController.enabled = false;

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
            _playerMovementScript.enabled = true;

        if (_playerCharacterController != null && !_playerCharacterController.enabled)
            _playerCharacterController.enabled = true;

        _movementLocked = false;
    }

    // ─── TELEPORT & CAMERA ─────────────────────────────────────────

    private Vector3? GetTeleportTarget()
    {
        if (!teleportPlayer) return null;

        if (_currentSide == InteractionSide.Left && leftPlayerPoint != null)
            return transform.position + _leftPlayerOffset;
        if (_currentSide == InteractionSide.Right && rightPlayerPoint != null)
            return transform.position + _rightPlayerOffset;
        return targetPlayerPosition;
    }

    private bool GetCameraPointTarget(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (!switchCamera) return false;

        if (_currentSide == InteractionSide.Left && leftCameraPoint != null)
        {
            position = transform.position + _leftCamOffset;
            rotation = _leftCamRot;
            return true;
        }
        if (_currentSide == InteractionSide.Right && rightCameraPoint != null)
        {
            position = transform.position + _rightCamOffset;
            rotation = _rightCamRot;
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

    // ─── CHUYỂN CAMERA ────────────────────────────────────────────

    private void SwitchToDialogueCameraAtPoint(Vector3 position, Quaternion rotation)
    {
        Debug.Log("[DialogueTrigger] === SWITCH CAMERA START ===");

        if (!switchCamera) return;

        if (mainCameraObject == null)
            mainCameraObject = FindMainCamera();

        if (mainCameraObject == null)
        {
            Debug.LogError("[DialogueTrigger] Main Camera is NULL! Cannot switch.");
            return;
        }

        if (dialogueCameraObject == null)
            FindDialogueCamera();

        if (dialogueCameraObject == null)
        {
            Debug.LogError("[DialogueTrigger] Dialogue Camera is NULL! Cannot switch.");
            return;
        }

        if (_originalMainCamPos == Vector3.zero)
        {
            _originalMainCamPos = mainCameraObject.transform.position;
            _originalMainCamRot = mainCameraObject.transform.rotation;
        }

        mainCameraObject.SetActive(false);
        Debug.Log($"[DialogueTrigger] Main Camera '{mainCameraObject.name}' DISABLED.");

        Transform parent = dialogueCameraObject.transform.parent;
        if (parent != null)
        {
            parent.position = position;
            parent.rotation = rotation;
        }
        else
        {
            dialogueCameraObject.transform.position = position;
            dialogueCameraObject.transform.rotation = rotation;
        }

        dialogueCameraObject.SetActive(true);
        Debug.Log($"[DialogueTrigger] Dialogue Camera '{dialogueCameraObject.name}' ENABLED.");

        Camera cam = dialogueCameraObject.GetComponent<Camera>();
        if (cam != null) cam.enabled = true;

        Debug.Log("[DialogueTrigger] === SWITCH CAMERA COMPLETE ===");
    }

    private void RestoreCameraState()
    {
        if (!switchCamera) return;

        Debug.Log("[DialogueTrigger] === RESTORE CAMERA START ===");

        if (dialogueCameraObject != null)
        {
            dialogueCameraObject.SetActive(false);
            Debug.Log($"[DialogueTrigger] Dialogue Camera '{dialogueCameraObject.name}' DISABLED.");
        }

        if (mainCameraObject != null)
        {
            mainCameraObject.SetActive(true);
            mainCameraObject.transform.position = _originalMainCamPos;
            mainCameraObject.transform.rotation = _originalMainCamRot;
            Debug.Log($"[DialogueTrigger] Main Camera restored.");
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.gameObject.SetActive(true);
                mainCam.transform.position = _originalMainCamPos;
                mainCam.transform.rotation = _originalMainCamRot;
            }
        }

        Debug.Log("[DialogueTrigger] === RESTORE CAMERA COMPLETE ===");
    }

    // ─── DIALOGUE FLOW ─────────────────────────────────────────────

    public void PlayDialogueAuto()
    {
        if (_isPlaying || _isDialogueTriggered) return;

        if (string.IsNullOrEmpty(triggerID))
        {
            Debug.LogWarning("[DialogueTrigger] triggerID rỗng.");
            return;
        }

        // ✅ Xác định hướng tương tác (có hỗ trợ override)
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

        int entryIndex = System.Array.IndexOf(dialogueEntries, entry);
        if (entry.playOnce && _playedEntryIndices.Contains(entryIndex))
            return;

        StopPlayerImmediately();
        _isDialogueTriggered = true;

        if (entry.playOnce)
            _playedEntryIndices.Add(entryIndex);

        _currentEntry = entry;

        if (_npcInteraction == null && interactionPrompt != null)
            interactionPrompt.SetActive(false);

        // ✅ Nếu có black screen, set flag true trước khi fade
        if (useBlackScreen)
        {
            DialogueBubbleUI.SetDialogueActive(true);
            StartCoroutine(PlayWithBlackScreenTransition(entry.lines));
        }
        else
        {
            // Nếu không có black screen, set flag true luôn để ẩn UI ngay
            DialogueBubbleUI.SetDialogueActive(true);
            StartDialogue(entry.lines);
        }
    }

    private IEnumerator PlayWithBlackScreenTransition(DialogueLineData[] lines)
    {
        Vector3? teleportTarget = GetTeleportTarget();
        bool hasCamPoint = GetCameraPointTarget(out Vector3 camPos, out Quaternion camRot);

        yield return FadeController.Instance.FadeToBlack();

        // 🔥 Tìm lại camera controller mỗi lần dùng (có thể bị null nếu camera bị disable khi vào combat)
        if (_cameraController == null)
            _cameraController = FindObjectOfType<HSRCameraController>();

        if (_cameraController != null)
        {
            _cameraController.ResetYaw();
            Debug.Log("[DialogueTrigger] Camera reset về hướng gốc (yaw = 0).");
        }

        ApplyTeleportToPosition(teleportTarget);

        if (switchCamera && hasCamPoint)
        {
            SwitchToDialogueCameraAtPoint(camPos, camRot);
        }

        ApplyCharacterFlips();

        yield return new WaitForSeconds(blackScreenDelay);

        yield return FadeController.Instance.FadeFromBlack();

        StartDialogueInternal(lines);
    }

    private void StartDialogue(DialogueLineData[] lines)
    {
        Vector3? teleportTarget = GetTeleportTarget();
        bool hasCamPoint = GetCameraPointTarget(out Vector3 camPos, out Quaternion camRot);

        // 🔥 Tìm lại camera controller mỗi lần dùng (có thể bị null nếu camera bị disable khi vào combat)
        if (_cameraController == null)
            _cameraController = FindObjectOfType<HSRCameraController>();

        if (_cameraController != null)
        {
            _cameraController.ResetYaw();
        }

        ApplyTeleportToPosition(teleportTarget);

        if (switchCamera && hasCamPoint)
        {
            SwitchToDialogueCameraAtPoint(camPos, camRot);
        }

        ApplyCharacterFlips();
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

    // ─── COMPLETE ───────────────────────────────────────────────────

    private void OnDialogueComplete()
    {
        _isPlaying = false;
        _isDialogueTriggered = false;
        _questNotified = false;

        if (useBlackScreenOnEnd)
            StartCoroutine(EndDialogueWithFade());
        else
            EndDialogueImmediate();
    }

    private void EndDialogueImmediate()
    {
        DialogueBubbleUI.SetDialogueActive(false);

        RestoreCameraState();
        RestoreOriginalScales();
        UnlockPlayerMovement();
        NotifyQuestIfNeeded();
        InvokeNPCInteractionComplete();
        CheckAndHideIfNeeded();
    }

    private IEnumerator EndDialogueWithFade()
    {
        yield return FadeController.Instance.FadeToBlack();

        RestoreCameraState();
        RestoreOriginalScales();

        yield return new WaitForSeconds(blackScreenDelay);

        yield return FadeController.Instance.FadeFromBlack();

        DialogueBubbleUI.SetDialogueActive(false);

        UnlockPlayerMovement();
        NotifyQuestIfNeeded();
        InvokeNPCInteractionComplete();
        CheckAndHideIfNeeded();
    }

    private void NotifyQuestIfNeeded()
    {
        if (!_questNotified && QuestManager.Instance != null && !string.IsNullOrEmpty(triggerID))
        {
            _questNotified = true;
            QuestManager.Instance.OnDialogueEnded(triggerID);
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
            if (entry != null && (!entry.playOnce || !_playedEntryIndices.Contains(System.Array.IndexOf(dialogueEntries, entry))))
            {
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
            }
            else
            {
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
            }
        }
    }

    // ─── FLIP LOGIC ─────────────────────────────────────────────────

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

    // ─── TRIGGER EVENTS ─────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (_npcInteraction != null) return;

        var entry = GetAppropriateEntry();
        if (entry != null)
        {
            int index = System.Array.IndexOf(dialogueEntries, entry);
            if (!entry.playOnce || !_playedEntryIndices.Contains(index))
            {
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
            }
        }
        else
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
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

    // ─── PUBLIC ─────────────────────────────────────────────────────

    public bool IsPlaying()
    {
        return _isPlaying;
    }
}