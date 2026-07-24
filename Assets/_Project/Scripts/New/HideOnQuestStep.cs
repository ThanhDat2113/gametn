using UnityEngine;

/// <summary>
/// Ẩn GameObject khi một step nhiệm vụ cụ thể được hoàn thành.
/// Đơn giản, không điều kiện phức tạp.
/// </summary>
public class HideOnQuestStep : MonoBehaviour
{
    [Header("Quest Step")]
    [Tooltip("ID của quest cần theo dõi (để trống nếu là quest hiện tại)")]
    public string questId;

    [Tooltip("Index của step (bắt đầu từ 0)")]
    public int stepIndex;

    [Header("Target")]
    [Tooltip("GameObject cần ẩn. Để trống sẽ ẩn chính GameObject này.")]
    public GameObject targetObject;

    private void Start()
    {
        if (targetObject == null)
            targetObject = gameObject;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);
            // Kiểm tra nếu step đã hoàn thành từ trước (khi scene load sau)
            CheckIfAlreadyCompleted();
        }
        else
        {
            Debug.LogWarning("[HideOnQuestStep] QuestManager.Instance is null.");
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnStepCompleted.RemoveListener(OnStepCompleted);
    }

    private void OnStepCompleted(QuestStep step)
    {
        if (IsMatchingStep(step))
            HideObject();
    }

    private void CheckIfAlreadyCompleted()
    {
        var currentQuest = QuestManager.Instance.CurrentQuest;
        if (currentQuest == null) return;

        if (!string.IsNullOrEmpty(questId) && currentQuest.questId != questId)
            return;

        if (stepIndex < 0 || stepIndex >= currentQuest.steps.Length)
            return;

        if (currentQuest.steps[stepIndex].isCompleted)
            HideObject();
    }

    private bool IsMatchingStep(QuestStep step)
    {
        var currentQuest = QuestManager.Instance.CurrentQuest;
        if (currentQuest == null) return false;

        if (!string.IsNullOrEmpty(questId) && currentQuest.questId != questId)
            return false;

        for (int i = 0; i < currentQuest.steps.Length; i++)
        {
            if (currentQuest.steps[i] == step && i == stepIndex)
                return true;
        }
        return false;
    }

    private void HideObject()
    {
        if (targetObject != null && targetObject.activeSelf)
        {
            targetObject.SetActive(false);
            Debug.Log($"[HideOnQuestStep] Ẩn {targetObject.name} (step {stepIndex} hoàn thành)");
        }
    }
}