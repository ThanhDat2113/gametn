using UnityEngine;
using UnityEngine.Events;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Template (asset gốc, không bị thay đổi)")]
    public QuestData questTemplate;

    [Header("UI")]
    public QuestUI questUI;

    public UnityEvent<QuestStep> OnStepCompleted;
    public UnityEvent<QuestData> OnQuestCompleted;

    private QuestData runtimeQuest;   // bản sao runtime
    private int currentStepIndex = 0;

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
        if (questTemplate == null)
        {
            Debug.LogError("[QuestManager] No quest template assigned!");
            return;
        }
        // Bắt đầu quest mới hoàn toàn (reset)
        StartQuest(questTemplate);
    }

    // Tạo bản sao hoàn chỉnh của QuestData (không dùng Instantiate để tránh giữ tham chiếu)
    private QuestData CloneQuest(QuestData original)
    {
        QuestData clone = ScriptableObject.CreateInstance<QuestData>();
        clone.questId = original.questId;
        clone.questName = original.questName;
        clone.isRepeatable = original.isRepeatable;

        // Clone từng step (quan trọng: reset isCompleted)
        clone.steps = new QuestStep[original.steps.Length];
        for (int i = 0; i < original.steps.Length; i++)
        {
            clone.steps[i] = new QuestStep
            {
                stepId = original.steps[i].stepId,
                type = original.steps[i].type,
                targetId = original.steps[i].targetId,
                description = original.steps[i].description,
                isCompleted = false   // Luôn reset
            };
        }
        return clone;
    }

    public void StartQuest(QuestData template)
    {
        runtimeQuest = CloneQuest(template);
        currentStepIndex = 0;
        UpdateUI();
        Debug.Log($"[Quest] Started fresh quest: {runtimeQuest.questName}");
    }

    public void OnDialogueEnded(string triggerID)
    {
        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.type == QuestStepType.Talk && step.targetId == triggerID && !step.isCompleted)
        {
            CompleteCurrentStep();
        }
    }

    public void OnEnemyGroupDefeated(EnemyGroupData enemyGroup)
    {
        if (runtimeQuest == null) return;
        if (currentStepIndex >= runtimeQuest.steps.Length) return;

        var step = runtimeQuest.steps[currentStepIndex];
        if (step.type == QuestStepType.Kill && step.targetId == enemyGroup.name && !step.isCompleted)
        {
            CompleteCurrentStep();
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
        }
        else
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (questUI != null)
            questUI.SetObjective(runtimeQuest.steps[currentStepIndex].description);
    }

    // Gọi khi muốn reset thủ công (ví dụ từ MainMenu)
    public void ResetQuest()
    {
        if (questTemplate != null)
            StartQuest(questTemplate);
    }
}