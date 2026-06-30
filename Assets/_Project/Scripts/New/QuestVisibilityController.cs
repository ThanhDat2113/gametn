using UnityEngine;

/// <summary>
/// Gắn script này lên một GameObject CHA luôn active.
/// Nó sẽ điều khiển bật/tắt targetObject theo điều kiện quest.
///
/// CÁCH DÙNG:
///   1. Tạo một GameObject rỗng luôn active (VD: "VisibilityManager")
///   2. Gắn script này lên đó
///   3. Kéo GameObject cần ẩn/hiện vào targetObject
///   4. Set targetObject về INACTIVE trong scene (tắt thủ công trong Inspector)
///   5. defaultVisibility = false → target bắt đầu ẩn
/// </summary>
public class QuestVisibilityController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("GameObject cần điều khiển ẩn/hiện. Để inactive trong scene từ đầu nếu defaultVisibility = false.")]
    public GameObject targetObject;

    [Header("Visibility Config")]
    public QuestVisibilityConfig config;

    [Header("Default State")]
    [Tooltip("false = ẩn khi chưa đủ điều kiện. true = hiện khi chưa đủ điều kiện.")]
    public bool defaultVisibility = false;

    private void Start()
    {
        // Áp dụng trạng thái mặc định
        ApplyVisibility(defaultVisibility);

        if (config == null)
        {
            Debug.LogWarning($"[QuestVisibility] {gameObject.name} has no config.");
            return;
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStepCompleted.AddListener(OnQuestStepCompleted);
            QuestManager.Instance.OnQuestCompleted.AddListener(OnQuestCompleted);
        }
        else
        {
            Debug.LogWarning("[QuestVisibility] QuestManager not found.");
            return;
        }

        // Kiểm tra ngay — quest có thể đã đủ điều kiện từ đầu
        CheckVisibility();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStepCompleted.RemoveListener(OnQuestStepCompleted);
            QuestManager.Instance.OnQuestCompleted.RemoveListener(OnQuestCompleted);
        }
    }

    private void OnQuestStepCompleted(QuestStep step) => CheckVisibility();
    private void OnQuestCompleted(QuestData quest) => CheckVisibility();

    private void CheckVisibility()
    {
        if (config == null || targetObject == null) return;

        bool shouldShow = defaultVisibility;

        if (config.EvaluateConditions(QuestManager.Instance))
        {
            if (config.conditions.Count > 0)
                shouldShow = (config.conditions[0].action == QuestVisibilityConfig.ActionType.Show);
        }

        ApplyVisibility(shouldShow);
    }

    private void ApplyVisibility(bool visible)
    {
        if (targetObject == null) return;

        if (targetObject.activeSelf != visible)
        {
            targetObject.SetActive(visible);
            Debug.Log($"[QuestVisibility] {targetObject.name} → {(visible ? "SHOW" : "HIDE")}");
        }
    }
}