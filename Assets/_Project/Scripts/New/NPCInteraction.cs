using UnityEngine;
using System.Collections;

public class NPCInteraction : MonoBehaviour
{
    [Header("Identity")]
    public string triggerID; // Khớp với QuestStep.targetId

    [Header("Dialogue")]
    public DialogueTrigger dialogueTrigger; // Gán nếu NPC có hội thoại

    [Header("Combat")]
    public EnemyGroupData enemyGroupForCombat; // Nhóm kẻ địch khi combat

    [Header("Visual")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt;

    [Header("Settings")]
    public bool autoStartCombatOnTouch = false; // Nếu true, chạm là vào combat luôn (không cần E)

    private bool isPlayerInRange = false;
    private bool isProcessing = false;

    private void Update()
    {
        if (!isPlayerInRange || isProcessing) return;

        // Nếu auto combat khi chạm, thì không cần nhấn E
        if (autoStartCombatOnTouch)
        {
            TryInteract();
            return;
        }

        // Nếu không auto, cần nhấn phím tương tác
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    public void TryInteract()
    {
        if (isProcessing) return;
        isProcessing = true;

        // Lấy step hiện tại
        QuestStep currentStep = QuestManager.Instance?.CurrentStep;
        if (currentStep == null)
        {
            // Không có quest -> mở dialogue mặc định (nếu có)
            if (dialogueTrigger != null)
                dialogueTrigger.PlayDialogueAuto();
            else
                Debug.Log("[NPCInteraction] Không có quest và không có dialogue.");
            isProcessing = false;
            return;
        }

        // Kiểm tra nếu step hiện tại là Talk và targetId khớp
        if (currentStep.type == QuestStepType.Talk && currentStep.targetId == triggerID)
        {
            if (dialogueTrigger != null)
            {
                dialogueTrigger.PlayDialogueAuto();
                // DialogueTrigger sẽ tự gọi OnDialogueEnded khi xong
            }
            else
            {
                // Nếu không có dialogueTrigger, vẫn có thể hoàn thành step (nếu muốn)
                QuestManager.Instance?.OnDialogueEnded(triggerID);
            }
            isProcessing = false;
            return;
        }

        // Kiểm tra nếu step hiện tại là Kill và targetId khớp -> bắt đầu combat
        if (currentStep.type == QuestStepType.Kill && currentStep.targetId == triggerID)
        {
            if (enemyGroupForCombat == null)
            {
                Debug.LogError($"[NPCInteraction] {triggerID} cần combat nhưng chưa gán EnemyGroupData!");
                isProcessing = false;
                return;
            }

            StartCombatWithNPC();
            isProcessing = false;
            return;
        }

        // Nếu step không khớp, có thể mở dialogue mặc định (nếu có)
        if (dialogueTrigger != null)
            dialogueTrigger.PlayDialogueAuto();
        else
            Debug.Log($"[NPCInteraction] Step hiện tại không phù hợp với {triggerID}.");

        isProcessing = false;
    }

    private void StartCombatWithNPC()
    {
        var formationManager = FindFirstObjectByType<FormationManager>();
        if (formationManager == null)
        {
            Debug.LogError("[NPCInteraction] Không tìm thấy FormationManager!");
            return;
        }
        formationManager.SaveFormation();

        // Lưu session data với questTargetId = triggerID
        CombatSessionData.Set(FormationDataStorage.PendingFormation, enemyGroupForCombat, fromMap: true, questTargetId: triggerID);

        // Đăng ký không có MapEnemy, nên không cần RegisterLastEnemy
        // Bắt đầu transition
        StartCoroutine(StartCombatTransition());
    }

    private IEnumerator StartCombatTransition()
    {
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

        // Ẩn MapRoot và PersistentContainer (giống MapEnemy)
        var mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            SceneLoaderManager.MapRoot = mapRoot;
            mapRoot.SetActive(false);
        }

        var persistentContainer = GameObject.Find("PersistentContainer");
        if (persistentContainer != null)
        {
            SceneLoaderManager.PersistentContainer = persistentContainer;
            persistentContainer.SetActive(false);
        }

        // Tạm dừng player (nếu cần)
        PlayerManager.Instance?.StopPlayer();

        SceneLoaderManager.LoadCombatScene();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }

    // Nếu muốn reset trạng thái khi bị disable
    private void OnDisable()
    {
        isProcessing = false;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
}