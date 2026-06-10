using UnityEngine;
using System.Collections;
using System.Linq;

[System.Serializable]
public class DialogueEntry
{
    public string questId;
    public int requiredStepIndex = -1;
    public bool requiredCompleted;
    public DialogueLineData[] lines;
    public bool playOnce = true;
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
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt;

    [Header("Black Screen")]
    public bool useBlackScreen = false;
    public bool useBlackScreenOnEnd = false;
    public float blackScreenDelay = 0.3f;

    [Header("Teleport")]
    public bool teleportPlayer = false;
    public Transform playerToTeleport;
    public Vector3 targetPlayerPosition;

    [Header("Dialogue Camera")]
    public bool switchCamera = false;

    public GameObject mainCameraObject;
    public GameObject dialogueCameraObject;

    // Empty GameObject dùng để đánh dấu vị trí camera
    public Transform dialogueCameraPoint;

    private Vector3 _originalMainCamPos;
    private Quaternion _originalMainCamRot;

    private bool _playerInRange;
    private bool _hasPlayedForCurrentEntry;
    private bool _isPlaying;

    private Camera _mainCamera;
    private MonoBehaviour _playerController;

    private DialogueEntry _currentEntry;

    private bool sequential = true;

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

        if (switchCamera && dialogueCameraObject == null)
            Debug.LogError("DialogueTrigger: Dialogue Camera chưa được gán!");

        if (switchCamera && dialogueCameraPoint == null)
            Debug.LogError("DialogueTrigger: Dialogue Camera Point chưa được gán!");

        if (teleportPlayer && playerToTeleport == null)
            Debug.LogError("DialogueTrigger: Player chưa được gán!");

        if (playerToTeleport != null)
            _playerController = playerToTeleport.GetComponent<MonoBehaviour>();
    }

    void Update()
    {
        if (!_playerInRange) return;

        if (!Input.GetKeyDown(interactKey)) return;

        var entry = GetAppropriateEntry();

        if (entry == null)
        {
            Debug.Log("[DialogueTrigger] Không có dialogue phù hợp.");
            return;
        }

        if (entry.playOnce && _hasPlayedForCurrentEntry)
            return;

        _hasPlayedForCurrentEntry = true;
        _currentEntry = entry;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (useBlackScreen)
            StartCoroutine(PlayWithBlackScreenTransition(entry.lines));
        else
            StartDialogue(entry.lines);
    }

    private DialogueEntry GetAppropriateEntry()
    {
        if (dialogueEntries == null || dialogueEntries.Length == 0)
            return null;

        var qm = QuestManager.Instance;
        var currentQuest = qm?.CurrentQuest;

        foreach (var entry in dialogueEntries)
        {
            // Quest ID check
            if (!string.IsNullOrEmpty(entry.questId))
            {
                if (currentQuest == null)
                    continue;

                if (currentQuest.questId != entry.questId)
                    continue;
            }

            // Step check
            if (entry.requiredStepIndex >= 0)
            {
                if (qm == null || currentQuest == null)
                    continue;

                if (entry.requiredStepIndex >= currentQuest.steps.Length)
                    continue;

                bool stepCompleted =
                    currentQuest.steps[entry.requiredStepIndex].isCompleted;

                if (entry.requiredCompleted != stepCompleted)
                    continue;

                // Nếu step chưa completed
                // thì phải là current step
                if (!entry.requiredCompleted)
                {
                    if (qm.CurrentStepIndex != entry.requiredStepIndex)
                        continue;
                }
            }

            // Reset playOnce nếu đổi entry
            if (_currentEntry != entry)
            {
                _hasPlayedForCurrentEntry = false;
                _currentEntry = entry;
            }

            return entry;
        }

        return null;
    }

    public void PlayDialogueAuto()
    {
        if (_isPlaying) return;

        var entry = GetAppropriateEntry();

        if (entry == null) return;

        if (entry.playOnce && _hasPlayedForCurrentEntry)
            return;

        _hasPlayedForCurrentEntry = true;
        _currentEntry = entry;

        if (interactionPrompt != null)
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

        if (switchCamera)
            SwitchToDialogueCamera();

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
        if (!teleportPlayer || playerToTeleport == null)
            return;

        if (_playerController != null)
            _playerController.enabled = false;

        CharacterController cc =
            playerToTeleport.GetComponent<CharacterController>();

        bool ccWasEnabled = false;

        if (cc != null && cc.enabled)
        {
            ccWasEnabled = true;
            cc.enabled = false;
        }

        Rigidbody rb = playerToTeleport.GetComponent<Rigidbody>();

        bool rbWasKinematic = false;

        if (rb != null)
        {
            rbWasKinematic = rb.isKinematic;

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerToTeleport.position = targetPlayerPosition;

        if (cc != null && ccWasEnabled)
        {
            cc.enabled = true;
            cc.Move(Vector3.zero);
        }

        if (rb != null)
            rb.isKinematic = rbWasKinematic;

        if (_playerController != null)
            _playerController.enabled = true;
    }

    private void SwitchToDialogueCamera()
    {
        if (mainCameraObject != null)
        {
            _originalMainCamPos = mainCameraObject.transform.position;
            _originalMainCamRot = mainCameraObject.transform.rotation;

            mainCameraObject.SetActive(false);
        }

        if (dialogueCameraObject != null)
        {
            Transform rig = dialogueCameraObject.transform.parent;

            if (rig == null)
            {
                Debug.LogError("Dialogue Camera cần có parent rig!");
                return;
            }

            if (dialogueCameraPoint != null)
            {
                rig.SetPositionAndRotation(
                    dialogueCameraPoint.position,
                    dialogueCameraPoint.rotation
                );
            }

            dialogueCameraObject.SetActive(true);
        }
    }

    private void RestoreCameraState()
    {
        if (!switchCamera) return;

        if (dialogueCameraObject != null)
            dialogueCameraObject.SetActive(false);

        if (mainCameraObject != null)
        {
            mainCameraObject.SetActive(true);

            mainCameraObject.transform.SetPositionAndRotation(
                _originalMainCamPos,
                _originalMainCamRot
            );
        }
    }

    private void StartDialogue(DialogueLineData[] lines)
    {
        ApplyTeleport();

        if (switchCamera)
            SwitchToDialogueCamera();

        StartDialogueInternal(lines);
    }

    private void StartDialogueInternal(DialogueLineData[] lines)
    {
        _isPlaying = true;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (sequential)
        {
            DialogueBubbleUI.Instance.ShowSequential(
                lines,
                transform,
                OnDialogueComplete
            );
        }
        else
        {
            DialogueBubbleUI.Instance.Show(
                lines[0],
                transform,
                OnDialogueComplete
            );
        }
    }

    private void OnDialogueComplete()
    {
        _isPlaying = false;

        if (QuestManager.Instance != null &&
            !string.IsNullOrEmpty(triggerID))
        {
            QuestManager.Instance.OnDialogueEnded(triggerID);
        }

        if (useBlackScreenOnEnd)
        {
            StartCoroutine(EndWithBlackScreen());
        }
        else
        {
            RestoreCameraState();
        }

        if (_playerInRange)
        {
            var entry = GetAppropriateEntry();

            if (entry != null &&
                (!entry.playOnce || !_hasPlayedForCurrentEntry))
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

        var entry = GetAppropriateEntry();

        if (entry != null &&
            (!entry.playOnce || !_hasPlayedForCurrentEntry) &&
            !_isPlaying)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInRange = false;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
}