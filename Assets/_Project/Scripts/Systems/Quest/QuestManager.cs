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
                stepId      = original.steps[i].stepId,
                type        = original.steps[i].type,
                targetId    = original.steps[i].targetId,
                description = original.steps[i].description,
                isCompleted = false
            };
        }
        return clone;
    }

    public void StartQuest(QuestData template)
    {
        runtimeQuest = CloneQuest(template);
        currentStepIndex = 0;

        // Hiển thị lại UI trước khi set nội dung
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

    // ─── COMBAT ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ MapEnemy khi đánh bại một enemy group.
    /// </summary>
    public void OnEnemyGroupDefeated(EnemyGroupData enemyGroup)
    {
        if (enemyGroup == null) return;
        OnEnemyGroupDefeated(enemyGroup.name);
    }

    /// <summary>
    /// Gọi từ CombatSceneStarter khi chiến thắng combat với một targetId cụ thể.
    /// targetId thường là triggerID của NPC (khớp với quest step targetId).
    /// </summary>
    public void OnEnemyGroupDefeated(string targetId)
    {
        if (string.IsNullOrEmpty(targetId))
        {
            Debug.LogWarning("[QuestManager] OnEnemyGroupDefeated called with null or empty targetId.");
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
        if (step.type == QuestStepType.Kill && step.targetId == targetId && !step.isCompleted)
        {
            Debug.Log($"[QuestManager] Kill step completed: {step.description}");
            CompleteCurrentStep();
        }
        else
        {
            Debug.Log($"[QuestManager] Kill step mismatch: step.type={step.type}, step.targetId={step.targetId}, targetId={targetId}");
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
                         || step.type == QuestStepType.FlowPuzzle;
        if (isPuzzleType && step.targetId == triggerID && !step.isCompleted)
        {
            Debug.Log($"[QuestManager] Puzzle completed: {step.description}");
            CompleteCurrentStep();
        }
    }

    // ─── EXPLORE / LOCATION ──────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ LocationTrigger khi player đến vị trí target.
    /// </summary>
    public void OnLocationReached(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
        {
            Debug.LogWarning("[QuestManager] OnLocationReached called with null or empty locationId.");
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
        if (step.type == QuestStepType.Explore && step.targetId == locationId && !step.isCompleted)
        {
            Debug.Log($"[QuestManager] Location reached: {locationId} → completing explore step.");
            CompleteCurrentStep();
        }
        else
        {
            Debug.Log($"[QuestManager] Explore step mismatch: step.type={step.type}, step.targetId={step.targetId}, locationId={locationId}");
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
            questUI.SetObjective(runtimeQuest.steps[currentStepIndex].description);
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