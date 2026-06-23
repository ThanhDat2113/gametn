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
    public GameObject dialogueCameraObject; // có thể để trống, script sẽ tự tìm
    public Transform dialogueCameraPoint;

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

        // ---- TỰ ĐỘNG TÌM DIALOGUE CAMERA NẾU CHƯA GÁN ----
        if (switchCamera)
        {
            FindDialogueCamera(); // tìm ngay khi Start
            if (dialogueCameraObject == null)
            {
                Debug.LogError("DialogueTrigger: Không tìm thấy DialogueCamera trong scene! Hãy đảm bảo có GameObject với component DialogueCamera.");
            }
        }

        if (switchCamera && dialogueCameraPoint == null)
        {
            Debug.LogWarning("DialogueTrigger: Dialogue Camera Point chưa được gán, camera sẽ giữ vị trí hiện tại.");
        }

        _npcInteraction = GetComponent<NPCInteraction>();
    }

    /// <summary>
    /// Tự động tìm DialogueCamera trong scene bằng component DialogueCamera.
    /// Tìm cả GameObject đang inactive (FindObjectsInactive.Include).
    /// </summary>
    private void FindDialogueCamera()
    {
        if (dialogueCameraObject != null)
            return;

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
        if (dialogueEntries == null || dialogueEntries.Length == 0)
            return null;

        var qm = QuestManager.Instance;
        var currentQuest = qm?.CurrentQuest;

        foreach (var entry in dialogueEntries)
        {
            if (!string.IsNullOrEmpty(entry.questId))
            {
                if (currentQuest == null)
                    continue;
                if (currentQuest.questId != entry.questId)
                    continue;
            }

            if (entry.requiredStepIndex >= 0)
            {
                if (qm == null || currentQuest == null)
                    continue;
                if (entry.requiredStepIndex >= currentQuest.steps.Length)
                    continue;

                bool stepCompleted = currentQuest.steps[entry.requiredStepIndex].isCompleted;
                if (entry.requiredCompleted != stepCompleted)
                    continue;

                if (!entry.requiredCompleted)
                {
                    if (qm.CurrentStepIndex != entry.requiredStepIndex)
                        continue;
                }
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

    public void PlayDialogueAuto()
    {
        if (_isPlaying) return;
        if (string.IsNullOrEmpty(triggerID))
        {
            Debug.LogWarning("[DialogueTrigger] triggerID rỗng, không thể chơi dialogue.");
            return;
        }

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
        if (!teleportPlayer) return;

        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null)
        {
            Debug.LogWarning("[DialogueTrigger] Không tìm thấy player để teleport.");
            return;
        }

        MonoBehaviour controller = player.GetComponent<MonoBehaviour>();
        if (controller != null) controller.enabled = false;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            player.transform.position = targetPlayerPosition;
            cc.enabled = true;
            cc.Move(Vector3.zero);
        }
        else
        {
            player.transform.position = targetPlayerPosition;
        }

        if (controller != null) controller.enabled = true;
    }

    private void SwitchToDialogueCamera()
    {
        // Tìm camera mỗi khi chuyển (phòng trường hợp scene chưa load kịp ở Start)
        FindDialogueCamera();

        if (dialogueCameraObject == null)
        {
            Debug.LogError("DialogueTrigger: Không thể chuyển sang DialogueCamera vì không tìm thấy!");
            return;
        }

        if (mainCameraObject != null)
        {
            _originalMainCamPos = mainCameraObject.transform.position;
            _originalMainCamRot = mainCameraObject.transform.rotation;
            mainCameraObject.SetActive(false);
        }

        Transform rig = dialogueCameraObject.transform.parent;
        if (rig == null)
        {
            Debug.LogError("Dialogue Camera cần có parent rig!");
            return;
        }

        if (dialogueCameraPoint != null)
        {
            rig.SetPositionAndRotation(dialogueCameraPoint.position, dialogueCameraPoint.rotation);
        }

        dialogueCameraObject.SetActive(true);
    }

    private void RestoreCameraState()
    {
        if (!switchCamera) return;

        if (dialogueCameraObject != null)
            dialogueCameraObject.SetActive(false);

        if (mainCameraObject != null)
        {
            mainCameraObject.SetActive(true);
            mainCameraObject.transform.SetPositionAndRotation(_originalMainCamPos, _originalMainCamRot);
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

        if (_npcInteraction == null && interactionPrompt != null)
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

        if (QuestManager.Instance != null && !string.IsNullOrEmpty(triggerID))
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

        if (_npcInteraction != null)
            return;

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

        if (_npcInteraction != null)
            return;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
}