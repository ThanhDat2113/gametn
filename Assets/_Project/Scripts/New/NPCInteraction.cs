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

    [Header("Interaction Range")]
    [Tooltip("Ban kinh toi da (units) de player co the tuong tac. De 0 se tu tinh tu trigger collider.")]
    public float interactionRange = 0f;

    private bool isPlayerInRange = false;
    private bool isProcessing = false;
    private GameObject currentCustomPrompt = null;

    // Vung tuong tac tu dong tinh o Awake
    private float _effectiveInteractionRange = 2f;
    private Vector3 _interactionCenterOffset;

    private void Awake()
    {
        CalculateInteractionRange();
    }

    private void CalculateInteractionRange()
    {
        if (interactionRange > 0f)
        {
            _effectiveInteractionRange = interactionRange;
            _interactionCenterOffset = Vector3.zero;
            return;
        }

        float maxRadius = 1.5f; // fallback toi thieu
        Vector3 center = transform.position;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col == null || !col.isTrigger) continue;

            // Chỉ quan tâm chiều ngang (XZ) vì game top-down.
            // KHÔNG nhân 1.2x và không dùng magnitude để tránh phóng đại
            // phạm vi tương tác so với collider thực tế.
            float colRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
            if (colRadius > maxRadius)
            {
                maxRadius = colRadius;
                center = col.bounds.center;
            }
        }

        _effectiveInteractionRange = maxRadius;
        _interactionCenterOffset = center - transform.position;
    }

    private Vector3 InteractionCenter => transform.position + _interactionCenterOffset;

    /// <summary>
    /// Kiểm tra player có thực sự nằm trong vùng tương tác (chạm trigger collider) hay không.
    /// Sửa bug: dialogue bị kích hoạt trước khi player chạm collider do khoảng cách phóng đại.
    /// </summary>
    private bool IsPlayerInActualRange()
    {
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null) return false;

        // Nếu user cố ý đặt interactionRange thủ công, tôn trọng khoảng cách đó
        if (interactionRange > 0f)
        {
            float dist = Vector3.Distance(player.transform.position, InteractionCenter);
            return dist <= interactionRange;
        }

        // Lấy bán kính player để check chính xác khi player chạm mép trigger
        float playerRadius = 0.1f;
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) playerRadius = Mathf.Max(playerRadius, cc.radius);
        CapsuleCollider playerCapsule = player.GetComponent<CapsuleCollider>();
        if (playerCapsule != null) playerRadius = Mathf.Max(playerRadius, playerCapsule.radius);

        Vector3 playerPos = player.transform.position;

        // Kiểm tra từng trigger collider trên NPC (và con của nó) bằng ClosestPoint.
        // Chính xác theo hình dạng collider thực tế, không phụ thuộc vào physics settings.
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col == null || !col.isTrigger || !col.enabled || !col.gameObject.activeInHierarchy) continue;

            // Nếu player đã ở trong bounds, chắc chắn chạm collider
            if (col.bounds.Contains(playerPos)) return true;

            // Tính điểm gần nhất trên collider so với vị trí player.
            // Nếu player ở bên ngoài, đây là điểm trên bề mặt collider gần player nhất.
            Vector3 closestPointOnCollider = col.ClosestPoint(playerPos);
            float distanceToCollider = Vector3.Distance(playerPos, closestPointOnCollider);

            // Player được coi là "chạm collider" khi khoảng cách đến bề mặt
            // <= bán kính của player (cộng thêm 0.05f dung sai).
            if (distanceToCollider <= playerRadius + 0.05f)
                return true;
        }

        return false;
    }

    private void Update()
    {
        // Kiem tra lai khoang cach thuc te moi frame.
        // Fix bug: khi teleport player, OnTriggerExit co the khong duoc goi,
        // khien isPlayerInRange bi "ket" = true va player co the nhan E tu xa.
        SyncPlayerRangeState();

        if (!isPlayerInRange || isProcessing) return;

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

    /// <summary>
    /// Dong bo trang thai player trong vung theo khoang cach thuc te.
    /// Bo sung cho OnTriggerEnter/Exit khi physics khong goi dung trigger.
    /// </summary>
    private void SyncPlayerRangeState()
    {
        GameObject player = PlayerManager.Instance?.GetPlayer();
        if (player == null)
        {
            if (isPlayerInRange)
            {
                isPlayerInRange = false;
                HideAllPrompts();
            }
            return;
        }

        bool actuallyInRange = IsPlayerInActualRange();

        if (actuallyInRange != isPlayerInRange)
        {
            isPlayerInRange = actuallyInRange;

            if (isPlayerInRange)
                UpdatePromptVisibility();
            else
                HideAllPrompts();
        }
    }

    public void TryInteract()
    {
        if (isProcessing) return;

        // SAFETY: Kiểm tra player có thực sự chạm trigger collider trước khi tương tác.
        // Ngăn kích hoạt dialogue từ xa khi player chưa chạm collider.
        if (!IsPlayerInActualRange())
        {
            isPlayerInRange = false;
            HideAllPrompts();
            Debug.LogWarning($"[NPCInteraction] {gameObject.name}: Player chưa chạm trigger collider, từ chối tương tác.");
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