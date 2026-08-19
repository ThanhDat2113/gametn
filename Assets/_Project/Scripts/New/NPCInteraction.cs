using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("Identity")]
    public string triggerID;

    [Header("Dialogue")]
    public DialogueTrigger dialogueTrigger;

    [Header("Combat")]
    public EnemyGroupData enemyGroupForCombat;

    [Header("Interaction Prompts (Fallback)")]
    public GameObject talkPrompt;
    public GameObject combatPrompt;

    [Header("Settings")]
    public KeyCode interactKey = KeyCode.E;
    public bool autoStartCombatOnTouch = false;

    private bool isPlayerInRange = false;
    private bool isProcessing = false;
    private GameObject currentCustomPrompt = null;

    private void Update()
    {
        if (isProcessing) return;

        // 🔥 SAFETY NET: OnTriggerExit có thể bị bỏ lỡ khi CharacterController của player
        // bị disable/enable trong lúc dialogue (Unity không gọi trigger events chính xác).
        // Kiểm tra lại khoảng cách thực tế để không thể kích hoạt dialogue từ xa.
        if (isPlayerInRange && !IsPlayerActuallyInRange())
        {
            isPlayerInRange = false;
            HideAllPrompts();
            return;
        }

        if (!isPlayerInRange) return;

        // Chặn tương tác trong khi dialogue đang chạy
        if (dialogueTrigger != null && dialogueTrigger.IsPlaying()) return;

        if (autoStartCombatOnTouch)
        {
            TryInteract();
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    public void TryInteract()
    {
        if (isProcessing) return;
        if (!isPlayerInRange) return;

        // 🔥 Xác nhận player thật sự ở trong trigger trước khi cho tương tác
        if (!IsPlayerActuallyInRange())
        {
            isPlayerInRange = false;
            HideAllPrompts();
            return;
        }

        isProcessing = true;

        // Ẩn prompt ngay khi bắt đầu tương tác
        HideAllPrompts();

        QuestStep currentStep = QuestManager.Instance?.CurrentStep;
        if (currentStep == null)
        {
            if (dialogueTrigger != null)
                dialogueTrigger.PlayDialogueAuto();
            else
                Debug.Log("[NPCInteraction] Không có quest và không có dialogue.");
            isProcessing = false;
            return;
        }

        // Talk step
        if (currentStep.type == QuestStepType.Talk && currentStep.targetId == triggerID)
        {
            if (dialogueTrigger != null)
                dialogueTrigger.PlayDialogueAuto();
            else
                QuestManager.Instance?.OnDialogueEnded(triggerID);
            isProcessing = false;
            return;
        }

        // Kill step -> combat
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

        // Step khác -> dialogue mặc định
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

        CombatSessionData.Set(FormationDataStorage.PendingFormation, enemyGroupForCombat, fromMap: true, questTargetId: triggerID);

        PlayerManager.Instance?.StopPlayer();

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

        SceneLoaderManager.LoadCombatScene();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = true;
        // 🔥 Chỉ khi player vào trigger mới hiển thị prompt
        UpdatePromptVisibility();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = false;
        HideAllPrompts();
    }

    /// <summary>
    /// Lấy trigger collider dùng cho tương tác NPC.
    /// Ưu tiên collider trên chính GameObject, sau đó tìm trong children.
    /// </summary>
    private Collider GetTriggerCollider()
    {
        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider != null && ownCollider.isTrigger)
            return ownCollider;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
            if (c != null && c.isTrigger) return c;

        return ownCollider;
    }

    /// <summary>
    /// Kiểm tra player có thực sự nằm trong trigger collider hay không.
    /// Dùng làm safety net khi OnTriggerExit bị bỏ lỡ (ví dụ: CharacterController bị
    /// disable/enable trong lúc dialogue khiến Unity không gọi trigger events chính xác).
    /// </summary>
    private bool IsPlayerActuallyInRange()
    {
        Collider triggerCollider = GetTriggerCollider();
        if (triggerCollider == null || !triggerCollider.enabled)
            return false;

        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return false;

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider != null && playerCollider.enabled)
            return triggerCollider.bounds.Intersects(playerCollider.bounds);

        // Fallback: nếu player collider đang bị disable (ví dụ trong lúc dialogue),
        // kiểm tra vị trí player có nằm trong bounds của trigger không.
        return triggerCollider.bounds.Contains(player.transform.position);
    }

    /// <summary>
    /// Cập nhật prompt dựa trên step hiện tại.
    /// Chỉ được gọi khi player thực sự vào trigger.
    /// </summary>
    private void UpdatePromptVisibility()
    {
        // Nếu player không ở gần, không hiển thị
        if (!isPlayerInRange)
        {
            HideAllPrompts();
            return;
        }

        // Nếu đang chơi dialogue, không hiển thị prompt
        if (dialogueTrigger != null && dialogueTrigger.IsPlaying())
        {
            HideAllPrompts();
            return;
        }

        HideAllPrompts();

        // Kiểm tra customPrompt từ entry
        GameObject customPrompt = dialogueTrigger != null ? dialogueTrigger.GetCurrentPrompt() : null;
        if (customPrompt != null)
        {
            customPrompt.SetActive(true);
            currentCustomPrompt = customPrompt;
            return;
        }

        // Fallback
        QuestStep currentStep = QuestManager.Instance?.CurrentStep;
        if (currentStep == null)
        {
            if (talkPrompt != null) talkPrompt.SetActive(true);
            return;
        }

        // Kiểm tra targetId có khớp không
        if (currentStep.targetId != triggerID) return;

        // Hiển thị prompt tương ứng với loại step
        if (currentStep.type == QuestStepType.Talk)
        {
            if (talkPrompt != null) talkPrompt.SetActive(true);
        }
        else if (currentStep.type == QuestStepType.Kill)
        {
            if (combatPrompt != null) combatPrompt.SetActive(true);
        }
        else
        {
            if (talkPrompt != null) talkPrompt.SetActive(true);
        }
    }

    /// <summary>
    /// Gọi từ DialogueTrigger khi dialogue kết thúc.
    /// 🔥 KHÔNG tự động hiển thị prompt nữa – để player rời khỏi và quay lại trigger.
    /// </summary>
    public void OnDialogueComplete()
    {
        // Không làm gì với prompt – prompt sẽ chỉ hiển thị khi OnTriggerEnter xảy ra
        // Bạn có thể thêm logic khác ở đây nếu cần (reset trạng thái, v.v.)
        Debug.Log("[NPCInteraction] Dialogue complete. Prompt will only show on next trigger enter.");
    }

    private void HideAllPrompts()
    {
        if (talkPrompt != null) talkPrompt.SetActive(false);
        if (combatPrompt != null) combatPrompt.SetActive(false);
        if (currentCustomPrompt != null)
        {
            currentCustomPrompt.SetActive(false);
            currentCustomPrompt = null;
        }
    }

    private void OnDisable()
    {
        isProcessing = false;
        HideAllPrompts();
    }
}

// Extension method để kiểm tra trạng thái playing của DialogueTrigger
public static class DialogueTriggerExtensions
{
    public static bool IsPlaying(this DialogueTrigger trigger)
    {
        if (trigger == null) return false;
        var field = typeof(DialogueTrigger).GetField("_isPlaying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null && (bool)field.GetValue(trigger);
    }
}