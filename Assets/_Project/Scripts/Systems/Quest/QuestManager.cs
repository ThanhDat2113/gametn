using UnityEngine;
using UnityEngine.Events;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Template (asset gốc, không bị thay đổi)")]
    public QuestData questTemplate;

    [Header("Quest Chain (tự động chạy tuần tự)")]
    public QuestData[] questChain;

    [Header("UI")]
    public QuestUI questUI;
    public QuestRewardUI rewardUI;

    public UnityEvent<QuestStep> OnStepCompleted;
    public UnityEvent<QuestData> OnQuestCompleted;
    public UnityEvent<QuestStep> OnStepChanged;

    private QuestData runtimeQuest;
    private int currentStepIndex = 0;
    private int currentChainIndex = 0;

    public QuestStep CurrentStep => (runtimeQuest != null && currentStepIndex < runtimeQuest.steps.Length)
        ? runtimeQuest.steps[currentStepIndex]
        : null;

    public QuestData CurrentQuest => runtimeQuest;
    public int CurrentStepIndex => currentStepIndex;

    public bool IsStepCompleted(int stepIndex)
    {
        if (runtimeQuest == null) return false;
        if (stepIndex < 0 || stepIndex >= runtimeQuest.steps.Length) return false;
        return runtimeQuest.steps[stepIndex].isCompleted;
    }

    public bool IsQuestCompleted()
    {
        if (runtimeQuest == null) return false;
        return currentStepIndex >= runtimeQuest.steps.Length;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (questChain != null && questChain.Length > 0)
        {
            StartQuest(questChain[0]);
        }
        else if (questTemplate != null)
        {
            StartQuest(questTemplate);
        }
        else
        {
            Debug.LogError("[QuestManager] No quest template or chain assigned!");
        }
    }

    private QuestData CloneQuest(QuestData original)
    {
        QuestData clone = ScriptableObject.CreateInstance<QuestData>();
        clone.questId = original.questId;
        clone.questName = original.questName;
        clone.isRepeatable = original.isRepeatable;
        clone.rewards = original.rewards;

        clone.steps = new QuestStep[original.steps.Length];
        for (int i = 0; i < original.steps.Length; i++)
        {
            clone.steps[i] = new QuestStep
            {
                stepId         = original.steps[i].stepId,
                type           = original.steps[i].type,
                targetId       = original.steps[i].targetId,
                requiredItem   = original.steps[i].requiredItem,
                requiredAmount = original.steps[i].requiredAmount,
                description    = original.steps[i].description,
                isCompleted    = false
            };
        }
        return clone;
    }

    public void StartQuest(QuestData template)
    {
        runtimeQuest = CloneQuest(template);
        currentStepIndex = 0;

        // ✅ Hiển thị lại UI trước khi set nội dung
        if (questUI != null)
        {
            questUI.Show();
            UpdateUI();
        }

        OnStepChanged?.Invoke(CurrentStep);
        Debug.Log($"[Quest] Started fresh quest: {runtimeQuest.questName}");
    }

    public void OnDialogueEnded(string triggerID)
    {
        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.isCompleted) return;

        // Step Talk: kiểm tra triggerID
        if (step.type == QuestStepType.Talk && step.targetId == triggerID)
        {
            CompleteCurrentStep();
            return;
        }

        // Step Gather: khi nói chuyện với NPC, kiểm tra inventory đã đủ vật phẩm chưa
        if (step.type == QuestStepType.Gather)
        {
            var item = step.requiredItem;
            int requiredAmount = step.requiredAmount;

            if (item == null)
            {
                Debug.LogError($"[QuestManager] Gather step '{step.stepId}' thiếu requiredItem! Gán ItemData trong Inspector.");
                return;
            }

            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(item, requiredAmount))
            {
                // Tiêu thụ vật phẩm khi nộp quest
                InventoryManager.Instance.RemoveItem(item, requiredAmount);
                Debug.Log($"[QuestManager] Đã nộp {requiredAmount}x {item.itemName} để hoàn thành step.");
                CompleteCurrentStep();
            }
            else
            {
                Debug.Log($"[QuestManager] Chưa đủ {item.itemName} để hoàn thành step. Cần: {requiredAmount}");
            }
        }
    }

    /// <summary>
    /// Gọi từ ChestReward khi người chơi mở rương.
    /// Nếu step hiện tại là Gather, kiểm tra inventory đã đủ vật phẩm chưa.
    /// </summary>
    public void OnChestOpened(string chestId)
    {
        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.isCompleted) return;

        // Nếu step là Gather, kiểm tra inventory ngay sau khi mở rương
        if (step.type == QuestStepType.Gather)
        {
            var item = step.requiredItem;
            int requiredAmount = step.requiredAmount;

            if (item == null) return;

            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(item, requiredAmount))
            {
                Debug.Log($"[QuestManager] Mở rương '{chestId}' — đã có đủ {item.itemName} x{requiredAmount}!");
                // Không auto-complete ở đây, chờ player nói chuyện với NPC để nộp
            }
        }

        Debug.Log($"[QuestManager] Chest opened: {chestId}");
    }

    public void OnEnemyGroupDefeated(EnemyGroupData enemyGroup)
    {
        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.isCompleted) return;

        // Step Kill: hoàn thành ngay khi tiêu diệt đúng enemy group
        if (step.type == QuestStepType.Kill && step.targetId == enemyGroup.name)
        {
            CompleteCurrentStep();
            return;
        }

        // Step Gather: sau khi đánh quái xong, item đã vào inventory, kiểm tra nếu đủ thì auto-complete
        if (step.type == QuestStepType.Gather)
        {
            var item = step.requiredItem;
            int requiredAmount = step.requiredAmount;

            if (item == null) return;

            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(item, requiredAmount))
            {
                // Tiêu thụ vật phẩm và hoàn thành step ngay (vật phẩm lấy từ enemy drop)
                InventoryManager.Instance.RemoveItem(item, requiredAmount);
                Debug.Log($"[QuestManager] Đã tiêu diệt {enemyGroup.name} và nhặt được {item.itemName} — auto-complete Gather step!");
                CompleteCurrentStep();
            }
        }
    }

    private void CompleteCurrentStep()
    {
        var step = runtimeQuest.steps[currentStepIndex];
        step.isCompleted = true;
        OnStepCompleted?.Invoke(step);
        Debug.Log($"[Quest] Step completed: {step.description}");

        currentStepIndex++;

        if (currentStepIndex >= runtimeQuest.steps.Length)
        {
            OnQuestCompleted?.Invoke(runtimeQuest);
            Debug.Log($"[Quest] Quest completed: {runtimeQuest.questName}");
            questUI?.Hide();

            CompleteQuestAndAdvance();
        }
        else
        {
            UpdateUI();
            OnStepChanged?.Invoke(CurrentStep);
        }
    }

    private void CompleteQuestAndAdvance()
    {
        bool hasNext = (questChain != null && currentChainIndex + 1 < questChain.Length);

        System.Action onRewardsClaimed = () =>
        {
            if (hasNext)
                StartNextQuest();
            else
                Debug.Log("[Quest] No more quests in chain.");
        };

        GiveRewards(runtimeQuest, onRewardsClaimed);
    }

    private void StartNextQuest()
    {
        currentChainIndex++;
        if (currentChainIndex < questChain.Length)
        {
            Debug.Log($"[Quest] Starting next quest: {questChain[currentChainIndex].questName}");
            StartQuest(questChain[currentChainIndex]);
        }
    }

    private void GiveRewards(QuestData quest, System.Action onRewardsClaimed = null)
    {
        if (rewardUI != null)
        {
            rewardUI.Show(quest.rewards, () =>
            {
                ApplyRewards(quest.rewards);
                onRewardsClaimed?.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("[QuestManager] Không tìm thấy QuestRewardUI — cấp phần thưởng trực tiếp.");
            ApplyRewards(quest.rewards);
            onRewardsClaimed?.Invoke();
        }
    }

    private void ApplyRewards(QuestReward[] rewards)
    {
        var formationMgr = FindFirstObjectByType<FormationManager>();

        foreach (var reward in rewards)
        {
            switch (reward.rewardType)
            {
                case QuestRewardType.NewCharacter:
                    if (reward.character != null && formationMgr != null)
                    {
                        formationMgr.UnlockCharacter(reward.character);

                        // Scale level nhân vật mới = level trung bình của party
                        int avgLevel = 1;
                        if (PlayerProgression.Instance != null)
                        {
                            avgLevel = PlayerProgression.Instance.GetAveragePartyLevel();
                            PlayerProgression.Instance.SetLevel(reward.character, avgLevel);
                        }

                        Debug.Log($"[Quest Reward] Mở khóa nhân vật: {reward.character.characterName} (Lv.{avgLevel})");
                    }
                    break;

                case QuestRewardType.Item:
                    if (reward.item != null && InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.AddItem(reward.item, reward.amount);
                        Debug.Log($"[Quest Reward] Thêm item: {reward.item.itemName} x{reward.amount}");
                    }
                    break;

                case QuestRewardType.Experience:
                    if (reward.amount > 0 && PlayerProgression.Instance != null)
                    {
                        PlayerProgression.Instance.AddPartyExperience(reward.amount);
                        Debug.Log($"[Quest Reward] Party nhận {reward.amount} kinh nghiệm!");
                    }
                    else
                    {
                        Debug.LogWarning($"[Quest Reward] Không thể cộng {reward.amount} EXP: PlayerProgression chưa được khởi tạo.");
                    }
                    break;
            }
        }
    }

    private void UpdateUI()
    {
        if (questUI != null && runtimeQuest != null && currentStepIndex < runtimeQuest.steps.Length)
            questUI.SetObjective(runtimeQuest.steps[currentStepIndex].description);
    }

    public void ResetQuest()
    {
        if (questChain != null && questChain.Length > 0)
        {
            currentChainIndex = 0;
            StartQuest(questChain[0]);
        }
        else if (questTemplate != null)
        {
            StartQuest(questTemplate);
        }
        else
        {
            Debug.LogWarning("[QuestManager] Cannot reset because no quest chain or template is set.");
        }
    }
}