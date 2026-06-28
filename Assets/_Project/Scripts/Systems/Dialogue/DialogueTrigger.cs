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
    public GameObject mainCameraObject;
    public GameObject dialogueCameraObject;
    public Transform dialogueCameraPoint;

    [Header("Directional Dialogue")]
    public bool useDirectionalDialogue = false;
    public Transform leftPlayerPoint;
    public Transform rightPlayerPoint;
    public Transform leftCameraPoint;
    public Transform rightCameraPoint;

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

    void Start()
    {
        _mainCamera = Camera.main;
        if (mainCameraObject == null && _mainCamera != null)
            mainCameraObject = _mainCamera.gameObject;

        if (mainCameraObject != null)
        {
            _originalMainCamPos = mainCameraObject.transform.position;
            _originalMainCamRot = mainCameraObject.transform.rotation;
        }

        if (switchCamera)
        {
            FindDialogueCamera();
            if (dialogueCameraObject == null)
                Debug.LogError("DialogueTrigger: Không tìm thấy DialogueCamera!");
        }

        if (switchCamera && dialogueCameraPoint == null)
            Debug.LogWarning("DialogueTrigger: Dialogue Camera Point chưa được gán.");

        _npcInteraction = GetComponent<NPCInteraction>();
    }

    private void FindDialogueCamera()
    {
        if (dialogueCameraObject != null) return;
        DialogueCamera cam = FindFirstObjectByType<DialogueCamera>(FindObjectsInactive.Include);
        if (cam != null)
        {
            dialogueCameraObject = cam.gameObject;
            Debug.Log("[DialogueTrigger] Tự tìm thấy DialogueCamera.");
        }
        else
        {
            Debug.LogError("Không tìm thấy DialogueCamera trong scene.");
        }
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
        if (!useDirectionalDialogue) return InteractionSide.Left;
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return InteractionSide.Left;
        float diff = player.transform.position.x - transform.position.x;
        return diff < 0 ? InteractionSide.Left : InteractionSide.Right;
    }

    public void PlayDialogueAuto()
    {
        if (_isPlaying) return;
        if (string.IsNullOrEmpty(triggerID))
        {
            Debug.LogWarning("[DialogueTrigger] triggerID rỗng, không thể chơi dialogue.");
            return;
        }

        _currentSide = DetermineInteractionSide();
        var entry = GetAppropriateEntry();

        if (entry == null)
        {
            if (defaultLines != null && defaultLines.Length > 0)
            {
                Debug.Log("[DialogueTrigger] Không có entry phù hợp, dùng defaultLines.");
                StartDialogue(defaultLines);
                return;
            }
            Debug.Log("[DialogueTrigger] Không có dialogue phù hợp.");
            return;
        }

        if (entry.playOnce && _hasPlayedForCurrentEntry)
        {
            Debug.Log("[DialogueTrigger] Entry đã chơi 1 lần và playOnce=true.");
            return;
        }

        _hasPlayedForCurrentEntry = true;
        _currentEntry = entry;

        if (_npcInteraction == null && interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (useBlackScreen)
            StartCoroutine(PlayWithBlackScreenTransition(entry.lines));
        else
            StartDialogue(entry.lines);
    }

    private IEnumerator PlayWithBlackScreenTransition(DialogueLineData[] lines)
    {
        yield return FadeController.Instance.FadeToBlack();
        ApplyTeleport();
        if (switchCamera) SwitchToDialogueCamera();
        yield return new WaitForSeconds(blackScreenDelay);
        yield return FadeController.Instance.FadeFromBlack();
        StartDialogueInternal(lines);
    }

    private IEnumerator EndWithBlackScreen()
    {
        yield return FadeController.Instance.FadeToBlack();
        RestoreCameraState();
        yield return new WaitForSeconds(blackScreenDelay);
        yield return FadeController.Instance.FadeFromBlack();
    }

    private void ApplyTeleport()
    {
        if (!teleportPlayer) return;
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return;

        Vector3 targetPos = targetPlayerPosition;
        if (useDirectionalDialogue)
        {
            if (_currentSide == InteractionSide.Left && leftPlayerPoint != null)
                targetPos = leftPlayerPoint.position;
            else if (_currentSide == InteractionSide.Right && rightPlayerPoint != null)
                targetPos = rightPlayerPoint.position;
        }

        MonoBehaviour controller = player.GetComponent<MonoBehaviour>();
        if (controller != null) controller.enabled = false;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            player.transform.position = targetPos;
            cc.enabled = true;
            cc.Move(Vector3.zero);
        }
        else
        {
            player.transform.position = targetPos;
        }

        if (controller != null) controller.enabled = true;
    }

    private void SwitchToDialogueCamera()
    {
        FindDialogueCamera();
        if (dialogueCameraObject == null) return;

        if (mainCameraObject != null)
        {
            _originalMainCamPos = mainCameraObject.transform.position;
            _originalMainCamRot = mainCameraObject.transform.rotation;
            mainCameraObject.SetActive(false);
        }

        Transform rig = dialogueCameraObject.transform.parent;
        if (rig == null) return;

        Transform cameraPoint = dialogueCameraPoint;
        if (useDirectionalDialogue)
        {
            if (_currentSide == InteractionSide.Left && leftCameraPoint != null)
                cameraPoint = leftCameraPoint;
            else if (_currentSide == InteractionSide.Right && rightCameraPoint != null)
                cameraPoint = rightCameraPoint;
        }

        if (cameraPoint != null)
            rig.SetPositionAndRotation(cameraPoint.position, cameraPoint.rotation);

        dialogueCameraObject.SetActive(true);
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
    }

    private void StartDialogue(DialogueLineData[] lines)
    {
        if (_currentSide == InteractionSide.Left)
            _currentSide = DetermineInteractionSide();

        ApplyTeleport();
        if (switchCamera) SwitchToDialogueCamera();
        StartDialogueInternal(lines);
    }

    private void StartDialogueInternal(DialogueLineData[] lines)
    {
        _isPlaying = true;
        if (_npcInteraction == null && interactionPrompt != null)
            interactionPrompt.SetActive(false);

        Transform playerTarget = PlayerManager.Instance?.GetPlayer()?.transform;

        if (sequential)
        {
            DialogueBubbleUI.Instance.ShowSequential(
                lines,
                transform,          // npcTarget
                playerTarget,       // playerTarget
                OnDialogueComplete,
                0,
                useDirectionalDialogue ? _currentSide : (InteractionSide?)null
            );
        }
        else
        {
            DialogueBubbleUI.Instance.Show(
                lines[0],
                transform,
                playerTarget,
                useDirectionalDialogue ? _currentSide : (InteractionSide?)null,
                OnDialogueComplete
            );
        }
    }

    private void OnDialogueComplete()
    {
        _isPlaying = false;

        if (QuestManager.Instance != null && !string.IsNullOrEmpty(triggerID))
            QuestManager.Instance.OnDialogueEnded(triggerID);

        if (useBlackScreenOnEnd)
            StartCoroutine(EndWithBlackScreen());
        else
            RestoreCameraState();

        if (_npcInteraction != null)
            _npcInteraction.OnDialogueComplete();

        if (_npcInteraction == null && _playerInRange)
        {
            var entry = GetAppropriateEntry();
            if (entry != null && (!entry.playOnce || !_hasPlayedForCurrentEntry))
            {
                if (interactionPrompt != null)
                    interactionPrompt.SetActive(true);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (_npcInteraction != null) return;

        var entry = GetAppropriateEntry();
        if (entry != null && (!entry.playOnce || !_hasPlayedForCurrentEntry) && !_isPlaying)
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
}