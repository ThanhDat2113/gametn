using UnityEngine;
using System.Collections;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Trigger Identity")]
    public string triggerID;

    public DialogueLineData[] lines;
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt;
    public bool playOnce = true;
    public bool sequential = true;
    public bool useBlackScreen = false;
    public bool useBlackScreenOnEnd = false;
    public float blackScreenDelay = 0.3f;

    [Header("Teleport Settings (during black screen)")]
    public bool teleportPlayer = false;
    public Transform playerToTeleport;
    public Vector3 targetPlayerPosition;
    public bool setCameraPosition = false;
    public Transform targetCameraTransform;
    public Vector3 targetCameraPosition;
    public Vector3 targetCameraRotation;

    [Header("Camera Switching (during black screen)")]
    public bool switchCamera = false;
    public GameObject dialogueCameraObject;
    public GameObject mainCameraObject;

    private Vector3 _originalMainCamPos;
    private Quaternion _originalMainCamRot;

    private bool _playerInRange;
    private bool _hasPlayed;
    private bool _isPlaying;
    private Camera _mainCamera;
    private MonoBehaviour _playerController;

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
        if (playOnce && _hasPlayed) return;

        _hasPlayed = true;
        // Ẩn prompt ngay khi nhấn nút
        if (interactionPrompt != null) interactionPrompt.SetActive(false);

        if (useBlackScreen)
            StartCoroutine(PlayWithBlackScreenTransition());
        else
            StartDialogue();
    }

    public void PlayDialogueAuto()
    {
        if (_isPlaying) return;
        _hasPlayed = true;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);

        if (useBlackScreen)
            StartCoroutine(PlayWithBlackScreenTransition());
        else
            StartDialogue();
    }

    private IEnumerator PlayWithBlackScreenTransition()
    {
        yield return FadeController.Instance.FadeToBlack();
        ApplyTeleportAndCamera();
        if (switchCamera) SwitchToDialogueCamera();
        yield return new WaitForSeconds(blackScreenDelay);
        yield return FadeController.Instance.FadeFromBlack();
        StartDialogue();
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

    private void StartDialogue()
    {
        _isPlaying = true;
        // Đảm bảo prompt đã bị ẩn (có thể đã ẩn từ trước)
        if (interactionPrompt != null) interactionPrompt.SetActive(false);

        if (sequential)
            DialogueBubbleUI.Instance.ShowSequential(lines, transform, OnDialogueComplete);
        else
            DialogueBubbleUI.Instance.Show(lines[0], transform, OnDialogueComplete);
    }

    private void OnDialogueComplete()
    {
        _isPlaying = false;

        // Báo cho QuestManager
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(triggerID))
            QuestManager.Instance.OnDialogueEnded(triggerID);

        // Quyết định hiện lại prompt hay không
        if (_playerInRange)
        {
            // Nếu playOnce = true và đã chơi rồi => không hiện lại
            // Ngược lại, nếu có thể chơi lại (playOnce = false) thì hiện lại
            if (!playOnce || !_hasPlayed)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(true);
            }
            // Nếu playOnce && _hasPlayed thì giữ nguyên ẩn (không hiện)
        }
        else
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
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
            // Chỉ hiện prompt nếu chưa từng chơi (hoặc cho phép chơi lại)
            if ((!playOnce || !_hasPlayed) && !_isPlaying)
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