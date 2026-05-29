using UnityEngine;
using System.Collections;
using System.Linq;

[System.Serializable]
public class DialogueEntry
{
    public string questId;          // ID của quest (so sánh với QuestManager.currentQuest.questId)
    public int requiredStepIndex;   // Yêu cầu step index hiện tại phải bằng giá trị này (-1: không cần)
    public bool requiredCompleted;  // Yêu cầu step đó đã hoàn thành hay chưa? (true=completed, false=not completed)
    public DialogueLineData[] lines;
    public bool playOnce = true;    // Ghi đè playOnce cho entry này
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Trigger Identity")]
    public string triggerID;

    [Header("Multiple Dialogue Entries (checked in order)")]
    public DialogueEntry[] dialogueEntries;

    // Fallback nếu không có entry nào phù hợp
    public DialogueLineData[] defaultLines;

    [Header("Visual")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt;
    public bool useBlackScreen = false;
    public bool useBlackScreenOnEnd = false;
    public float blackScreenDelay = 0.3f;

    [Header("Teleport / Camera (optional)")]
    public bool teleportPlayer = false;
    public Transform playerToTeleport;
    public Vector3 targetPlayerPosition;
    public bool setCameraPosition = false;
    public Transform targetCameraTransform;
    public Vector3 targetCameraPosition;
    public Vector3 targetCameraRotation;
    public bool switchCamera = false;
    public GameObject dialogueCameraObject;
    public GameObject mainCameraObject;

    private Vector3 _originalMainCamPos;
    private Quaternion _originalMainCamRot;
    private bool _playerInRange;
    private bool _hasPlayedForCurrentEntry = false; // Reset khi entry thay đổi
    private bool _isPlaying;
    private Camera _mainCamera;
    private MonoBehaviour _playerController;

    // Lưu entry đang được chọn để biết có nên reset _hasPlayedForCurrentEntry hay không
    private DialogueEntry _currentEntry;

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
            Debug.LogError("DialogueTrigger: Chưa gán dialogueCameraObject!");
        if (teleportPlayer && playerToTeleport == null)
            Debug.LogError("DialogueTrigger: Bật teleportPlayer nhưng chưa gán playerToTeleport!");
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
            Debug.Log($"[DialogueTrigger] No matching dialogue entry for current quest state.");
            return;
        }

        // Kiểm tra playOnce riêng cho entry này
        bool playOnceForEntry = entry.playOnce;
        if (playOnceForEntry && _hasPlayedForCurrentEntry) return;

        _hasPlayedForCurrentEntry = true;
        _currentEntry = entry;

        if (interactionPrompt != null) interactionPrompt.SetActive(false);

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

        // Duyệt theo thứ tự — entry đầu tiên thỏa mãn tất cả điều kiện sẽ được chọn
        foreach (var entry in dialogueEntries)
        {
            // ── Điều kiện 1: questId ──────────────────────────────
            // Nếu entry yêu cầu questId cụ thể, phải có quest đang chạy với đúng ID đó
            if (!string.IsNullOrEmpty(entry.questId))
            {
                if (currentQuest == null || currentQuest.questId != entry.questId)
                    continue;
            }

            // ── Điều kiện 2: requiredStepIndex & requiredCompleted ─
            // requiredStepIndex == -1  →  không quan tâm step nào
            if (entry.requiredStepIndex >= 0)
            {
                if (qm == null) continue;

                // Kiểm tra step đó có tồn tại không
                if (currentQuest == null || entry.requiredStepIndex >= currentQuest.steps.Length)
                    continue;

                bool stepCompleted = currentQuest.steps[entry.requiredStepIndex].isCompleted;

                // requiredCompleted == true  → cần step đó ĐÃ hoàn thành
                // requiredCompleted == false → cần step đó CHƯA hoàn thành (đang là step hiện tại)
                if (entry.requiredCompleted != stepCompleted)
                    continue;

                // Nếu yêu cầu step chưa hoàn thành (requiredCompleted=false),
                // step đó phải đúng là step HIỆN TẠI đang chờ
                if (!entry.requiredCompleted && qm.CurrentStepIndex != entry.requiredStepIndex)
                    continue;
            }

            // ── Tất cả điều kiện thỏa mãn ────────────────────────
            // Reset playOnce nếu đây là entry mới (entry vừa thay đổi)
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
        bool playOnceForEntry = entry.playOnce;
        if (playOnceForEntry && _hasPlayedForCurrentEntry) return;

        _hasPlayedForCurrentEntry = true;
        _currentEntry = entry;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (useBlackScreen)
            StartCoroutine(PlayWithBlackScreenTransition(entry.lines));
        else
            StartDialogue(entry.lines);
    }

    private IEnumerator PlayWithBlackScreenTransition(DialogueLineData[] lines)
    {
        yield return FadeController.Instance.FadeToBlack();
        ApplyTeleportAndCamera();
        if (switchCamera) SwitchToDialogueCamera();
        yield return new WaitForSeconds(blackScreenDelay);
        yield return FadeController.Instance.FadeFromBlack();
        StartDialogue(lines);
    }

    private IEnumerator EndWithBlackScreen()
    {
        yield return FadeController.Instance.FadeToBlack();
        if (switchCamera)
        {
            if (dialogueCameraObject != null) dialogueCameraObject.SetActive(false);
            if (mainCameraObject != null)
            {
                mainCameraObject.SetActive(true);
                if (_mainCamera != null)
                    _mainCamera.transform.SetPositionAndRotation(_originalMainCamPos, _originalMainCamRot);
                else
                    mainCameraObject.transform.SetPositionAndRotation(_originalMainCamPos, _originalMainCamRot);
            }
        }
        yield return new WaitForSeconds(blackScreenDelay);
        yield return FadeController.Instance.FadeFromBlack();
    }

    private void ApplyTeleportAndCamera()
    {
        if (teleportPlayer && playerToTeleport != null)
        {
            if (_playerController != null) _playerController.enabled = false;
            var cc = playerToTeleport.GetComponent<CharacterController>();
            bool ccWasEnabled = false;
            if (cc != null && cc.enabled) { ccWasEnabled = true; cc.enabled = false; }
            var rb = playerToTeleport.GetComponent<Rigidbody>();
            bool rbWasKinematic = false;
            if (rb != null)
            {
                rbWasKinematic = rb.isKinematic;
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            playerToTeleport.position = targetPlayerPosition;
            if (cc != null && ccWasEnabled) { cc.enabled = true; cc.Move(Vector3.zero); }
            if (rb != null) rb.isKinematic = rbWasKinematic;
            if (_playerController != null) _playerController.enabled = true;
        }
        if (setCameraPosition && _mainCamera != null)
        {
            if (targetCameraTransform != null)
                _mainCamera.transform.SetPositionAndRotation(targetCameraTransform.position, targetCameraTransform.rotation);
            else
                _mainCamera.transform.SetPositionAndRotation(targetCameraPosition, Quaternion.Euler(targetCameraRotation));
        }
    }

    private void SwitchToDialogueCamera()
    {
        if (mainCameraObject != null) mainCameraObject.SetActive(false);
        if (dialogueCameraObject != null) dialogueCameraObject.SetActive(true);
    }

    private void StartDialogue(DialogueLineData[] lines)
    {
        _isPlaying = true;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (sequential)
            DialogueBubbleUI.Instance.ShowSequential(lines, transform, OnDialogueComplete);
        else
            DialogueBubbleUI.Instance.Show(lines[0], transform, OnDialogueComplete);
    }

    private bool sequential = true; // Giữ sequential mặc định true, có thể thêm vào DialogueEntry nếu muốn

    private void OnDialogueComplete()
    {
        _isPlaying = false;
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(triggerID))
            QuestManager.Instance.OnDialogueEnded(triggerID);

        // Hiện lại prompt nếu vẫn trong vùng và entry vẫn còn hiệu lực (chưa hoàn thành vĩnh viễn)
        if (_playerInRange)
        {
            var entry = GetAppropriateEntry();
            if (entry != null && (!entry.playOnce || !_hasPlayedForCurrentEntry))
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(true);
            }
        }

        if (useBlackScreenOnEnd)
        {
            StartCoroutine(EndWithBlackScreen());
            return;
        }

        if (switchCamera)
        {
            if (dialogueCameraObject != null) dialogueCameraObject.SetActive(false);
            if (mainCameraObject != null)
            {
                mainCameraObject.SetActive(true);
                if (_mainCamera != null)
                    _mainCamera.transform.SetPositionAndRotation(_originalMainCamPos, _originalMainCamRot);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            var entry = GetAppropriateEntry();
            if (entry != null && (!entry.playOnce || !_hasPlayedForCurrentEntry) && !_isPlaying)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
    }
}