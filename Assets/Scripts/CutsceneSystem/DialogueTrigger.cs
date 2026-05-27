using UnityEngine;
using System.Collections;

public class DialogueTrigger : MonoBehaviour
{
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

    [Header("Combat After Dialogue")]
    public bool startCombatAfterDialogue = false;
    public EnemyGroupData enemyGroup;
    public string combatSceneName = "CombatScene";

    private Vector3 _originalMainCamPos;
    private Quaternion _originalMainCamRot;
    private bool _mainCamWasActive;

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
            _mainCamWasActive = mainCameraObject.activeSelf;
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
        if (_playerInRange && Input.GetKeyDown(interactKey))
        {
            if (playOnce && _hasPlayed) return;
            _hasPlayed = true;
            if (useBlackScreen)
                StartCoroutine(PlayWithBlackScreenTransition());
            else
                StartDialogue();
        }
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

        if (startCombatAfterDialogue)
            StartCombat();
    }

    private void StartCombat()
    {
        Debug.Log("[DialogueTrigger] Starting combat after dialogue.");

        // Nếu PendingFormation chưa được set (người chơi chưa nhấn T),
        // thử lấy từ FormationManager đang có trong scene.
        if (FormationDataStorage.PendingFormation == null)
        {
            var formationManager = FindFirstObjectByType<FormationManager>();
            if (formationManager != null)
            {
                formationManager.SaveFormation();
                Debug.Log("[DialogueTrigger] Lấy đội hình từ FormationManager.");
            }
            else
            {
                Debug.LogWarning("[DialogueTrigger] Không tìm thấy FormationManager trong scene.");
            }
        }

        var formation = FormationDataStorage.PendingFormation;
        if (formation == null)
        {
            Debug.LogError("[DialogueTrigger] Không có đội hình để bắt đầu combat! Hãy xếp đội hình trước.");
            return;
        }

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.StartCombat(formation, enemyGroup);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(combatSceneName);
        }
    }

    private void ApplyTeleportAndCamera()
    {
        if (teleportPlayer && playerToTeleport != null)
        {
            if (_playerController != null) _playerController.enabled = false;

            var cc = playerToTeleport.GetComponent<CharacterController>();
            bool ccWasEnabled = false;
            if (cc != null && cc.enabled)
            {
                ccWasEnabled = true;
                cc.enabled = false;
            }

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

            if (cc != null && ccWasEnabled)
            {
                cc.enabled = true;
                cc.Move(Vector3.zero);
            }
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
        if (sequential)
            DialogueBubbleUI.Instance.ShowSequential(lines, transform, OnDialogueComplete);
        else
            DialogueBubbleUI.Instance.Show(lines[0], transform, OnDialogueComplete);
    }

    private void OnDialogueComplete()
    {
        _isPlaying = false;
        if (useBlackScreenOnEnd)
            StartCoroutine(EndWithBlackScreen());
        else if (switchCamera)
        {
            if (dialogueCameraObject != null) dialogueCameraObject.SetActive(false);
            if (mainCameraObject != null)
            {
                mainCameraObject.SetActive(true);
                if (_mainCamera != null)
                    _mainCamera.transform.SetPositionAndRotation(_originalMainCamPos, _originalMainCamRot);
            }
        }
        else if (startCombatAfterDialogue)
        {
            StartCombat();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            if (interactionPrompt) interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            if (interactionPrompt) interactionPrompt.SetActive(false);
        }
    }
}