using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

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

    // Danh sách các Quest đã hoàn thành (lưu questId)
    private List<string> _completedQuestIds = new List<string>();

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

    // Kiểm tra xem một quest (theo ID) đã hoàn thành chưa (từ lịch sử)
    public bool IsQuestCompleted(string questId)
    {
        return _completedQuestIds.Contains(questId);
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
                stepId      = original.steps[i].stepId,
                type        = original.steps[i].type,
                targetId    = original.steps[i].targetId,
                description = original.steps[i].description,
                isCompleted = false,
                requiredAmount = original.steps[i].requiredAmount,
                currentAmount = 0
            };
        }
        return clone;
    }

    public void StartQuest(QuestData template)
    {
        runtimeQuest = CloneQuest(template);
        currentStepIndex = 0;

        if (questUI != null)
        {
            questUI.Show();
            UpdateUI();
        }

        OnStepChanged?.Invoke(CurrentStep);
        Debug.Log($"[Quest] Started fresh quest: {runtimeQuest.questName}");
    }

    // ─── DIALOGUE ─────────────────────────────────────────────────────────────

    public void OnDialogueEnded(string triggerID)
    {
        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.type == QuestStepType.Talk && step.targetId == triggerID && !step.isCompleted)
        {
            Debug.Log($"[QuestManager] Dialogue ended with {triggerID} → completing talk step.");
            CompleteCurrentStep();
        }
    }

    // ─── COMBAT (KILL COUNT) ─────────────────────────────────────────────────

    public void OnEnemyDefeated(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogWarning("[QuestManager] OnEnemyDefeated called with null or empty enemyId.");
            return;
        }

        if (runtimeQuest == null)
        {
            Debug.LogWarning("[QuestManager] No active quest.");
            return;
        }

        if (currentStepIndex >= runtimeQuest.steps.Length)
        {
            Debug.LogWarning($"[QuestManager] Quest already completed. Can't process step.");
            return;
        }

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.type == QuestStepType.Kill && step.targetId == enemyId && !step.isCompleted)
        {
            step.currentAmount++;
            Debug.Log($"[QuestManager] Kill progress: {step.currentAmount}/{step.requiredAmount} for {enemyId}");

            UpdateUI();

            if (step.currentAmount >= step.requiredAmount)
            {
                Debug.Log($"[QuestManager] Kill step completed: {step.description}");
                CompleteCurrentStep();
            }
        }
        else
        {
            Debug.Log($"[QuestManager] Kill step mismatch: step.type={step.type}, step.targetId={step.targetId}, enemyId={enemyId}");
        }
    }

    // ─── BACKWARD COMPATIBILITY (old OnEnemyGroupDefeated) ─────────────────

    public void OnEnemyGroupDefeated(EnemyGroupData enemyGroup)
    {
        if (enemyGroup == null) return;
        OnEnemyGroupDefeated(enemyGroup.name);
    }

    public void OnEnemyGroupDefeated(string targetId)
    {
        if (string.IsNullOrEmpty(targetId))
        {
            Debug.LogWarning("[QuestManager] OnEnemyGroupDefeated called with null or empty targetId.");
            return;
        }

        // Nếu step hiện tại là Kill với requiredAmount > 1, thì không xử lý ở đây (dùng OnEnemyDefeated)
        if (runtimeQuest != null && currentStepIndex < runtimeQuest.steps.Length)
        {
            var currentStep = runtimeQuest.steps[currentStepIndex];
            if (currentStep.type == QuestStepType.Kill && currentStep.requiredAmount > 1)
            {
                Debug.Log("[QuestManager] OnEnemyGroupDefeated ignored because Kill step uses count.");
                return;
            }
        }

        // Fallback: Kill step với requiredAmount = 1 (kiểu cũ)
        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.type == QuestStepType.Kill && step.targetId == targetId && !step.isCompleted)
        {
            if (step.requiredAmount <= 1)
            {
                CompleteCurrentStep();
            }
        }
    }

    // ─── GATHER / ITEM PICKUP ────────────────────────────────────────────────

    public void OnItemPickedUp(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("[QuestManager] OnItemPickedUp called with null or empty itemId.");
            return;
        }

        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.type == QuestStepType.Gather && step.targetId == itemId && !step.isCompleted)
        {
            Debug.Log($"[QuestManager] Item '{itemId}' picked up → completing gather step.");
            CompleteCurrentStep();
        }
    }

    // ─── PUZZLE ──────────────────────────────────────────────────────────────

    public void OnPuzzleCompleted(string triggerID)
    {
        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        bool isPuzzleType = step.type == QuestStepType.SymbolSequence
                         || step.type == QuestStepType.RiddleGate
                         || step.type == QuestStepType.MemoryGrove
                         || step.type == QuestStepType.SlidePuzzle
                         || step.type == QuestStepType.SpirePuzzle
                         || step.type == QuestStepType.FlowPuzzle
                         || step.type == QuestStepType.Unblock;
        if (isPuzzleType && step.targetId == triggerID && !step.isCompleted)
        {
            Debug.Log($"[QuestManager] Puzzle completed: {step.description}");
            CompleteCurrentStep();
        }
    }

    // ─── EXPLORE / LOCATION ──────────────────────────────────────────────────

    public void OnLocationReached(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
        {
            Debug.LogWarning("[QuestManager] OnLocationReached called with null or empty locationId.");
            return;
        }

        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.type == QuestStepType.Explore && step.targetId == locationId && !step.isCompleted)
        {
            Debug.Log($"[QuestManager] Location reached: {locationId} → completing explore step.");
            CompleteCurrentStep();
        }
    }

    // ─── COMPLETE STEP ──────────────────────────────────────────────────────

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

            // Thêm vào danh sách quest đã hoàn thành
            if (!_completedQuestIds.Contains(runtimeQuest.questId))
            {
                _completedQuestIds.Add(runtimeQuest.questId);
            }

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
        else
        {
            // Hết chuỗi quest - thông báo để marker biết không còn quest active
            Debug.Log("[Quest] Quest chain completed. No more quests.");
            OnStepChanged?.Invoke(null);
        }
    }

    // ─── REWARDS ─────────────────────────────────────────────────────────────

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
                    break;

                default:
                    Debug.LogWarning($"[Quest Reward] Unknown reward type: {reward.rewardType}");
                    break;
            }
        }
    }

    // ─── UI ──────────────────────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (questUI != null && runtimeQuest != null && currentStepIndex < runtimeQuest.steps.Length)
        {
            var step = runtimeQuest.steps[currentStepIndex];
            string displayText = step.description;

            // Thay thế placeholder cho Kill step
            if (step.type == QuestStepType.Kill && step.requiredAmount > 1)
            {
                displayText = displayText.Replace("{current}", step.currentAmount.ToString())
                                         .Replace("{required}", step.requiredAmount.ToString());
            }

            questUI.SetObjective(displayText);
        }
    }

    // ─── RESET ──────────────────────────────────────────────────────────────

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

    // ─── DEBUG ──────────────────────────────────────────────────────────────

    [ContextMenu("Force Complete Current Step")]
    public void DebugForceCompleteStep()
    {
        if (CurrentStep != null && !CurrentStep.isCompleted)
        {
            Debug.Log($"[QuestManager] DEBUG: Force completing step {CurrentStep.stepId}");
            CompleteCurrentStep();
        }
        else
        {
            Debug.Log("[QuestManager] DEBUG: No step to complete or step already completed.");
        }
    }
}