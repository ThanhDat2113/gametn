using UnityEngine;
using TMPro;

/// <summary>
/// Gắn vào 1 GameObject riêng trong MỖI scene/khu vực (Map1, Map2, CombatScene...).
/// Hiển thị % hoàn thành GỘP của TẤT CẢ quest thuộc khu vực đó, tính theo:
///   (tổng số step đã hoàn thành của mọi quest) / (tổng số step của mọi quest)
///
/// Với mỗi quest trong danh sách:
///   - Nếu quest đã hoàn thành hẳn (IsQuestCompleted) → tính đủ 100% số step của quest đó.
///   - Nếu quest đang là quest chạy hiện tại (CurrentQuest) → tính theo CurrentStepIndex.
///   - Nếu chưa từng chạy tới (chưa completed, không phải current) → tính 0 step đã xong.
///
/// KHÔNG cần sửa gì ở QuestManager/QuestData/QuestStep — chỉ đọc:
///   - QuestManager.Instance.IsQuestCompleted(questId)
///   - QuestManager.Instance.CurrentQuest / CurrentStepIndex
///
/// SETUP:
///   1. Tạo 1 GameObject trong scene (vd "AreaQuestStatus"), gắn script này.
///   2. Điền areaDisplayName (vd "Phương Bắc").
///   3. Kéo TẤT CẢ QuestData thuộc khu vực này vào questsInArea.
///   4. (Tuỳ chọn) Gán statusText nếu Text hiển thị CÙNG SCENE. Nếu Text nằm ở
///      Persistent Scene (khác scene), để trống và dùng AreaQuestStatusDisplay.Instance
///      (xem file AreaQuestStatusDisplay.cs).
/// </summary>
public class AreaQuestStatusUI : MonoBehaviour
{
    [Header("Khu vực")]
    [Tooltip("Tên hiển thị của khu vực này (vd: 'Phương Bắc', 'Thị trấn Silvane'...).")]
    [SerializeField] private string areaDisplayName = "Khu vực";

    [Tooltip("Tất cả QuestData thuộc khu vực này. % hoàn thành = tổng step đã xong / tổng step " +
             "của TẤT CẢ quest trong danh sách này gộp lại.")]
    [SerializeField] private QuestData[] questsInArea;

    [Header("UI (tuỳ chọn)")]
    [Tooltip("CHỈ dùng nếu Text hiển thị nằm CÙNG SCENE với script này. Nếu Canvas hiển thị " +
             "nằm ở Persistent Scene (không kéo tay cross-scene được), để trống field này và " +
             "gắn AreaQuestStatusDisplay lên Canvas đó — script sẽ tự gọi qua Instance.")]
    [SerializeField] private TMP_Text statusText;

    [Tooltip("Format hiển thị. {area}=tên khu vực, {percent}=% làm tròn, {done}=tổng step đã xong, " +
             "{total}=tổng step tất cả quest.")]
    [SerializeField] private string displayFormat = "{area}: {percent}% nhiệm vụ hoàn thành";

    /// <summary>Tổng số step đã hoàn thành, gộp tất cả quest trong questsInArea.</summary>
    public int CompletedSteps { get; private set; }

    /// <summary>Tổng số step, gộp tất cả quest trong questsInArea.</summary>
    public int TotalSteps { get; private set; }

    /// <summary>Tỉ lệ hoàn thành từ 0..1. Trả về 0 nếu chưa cấu hình quest nào hoặc quest không có step.</summary>
    public float CompletionRatio => TotalSteps > 0 ? (float)CompletedSteps / TotalSteps : 0f;

    /// <summary>True nếu tất cả quest trong khu vực đã hoàn thành hẳn (và có ít nhất 1 step được cấu hình).</summary>
    public bool IsAreaFullyCompleted => TotalSteps > 0 && CompletedSteps >= TotalSteps;

    private void Start()
    {
        Refresh();

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStepChanged.AddListener(OnStepChanged);
            QuestManager.Instance.OnStepCompleted.AddListener(OnStepCompleted);
            QuestManager.Instance.OnQuestCompleted.AddListener(OnQuestCompleted);
        }
        else
        {
            Debug.LogWarning("[AreaQuestStatusUI] QuestManager.Instance chưa sẵn sàng lúc Start().");
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnStepChanged.RemoveListener(OnStepChanged);
            QuestManager.Instance.OnStepCompleted.RemoveListener(OnStepCompleted);
            QuestManager.Instance.OnQuestCompleted.RemoveListener(OnQuestCompleted);
        }
    }

    private void OnStepChanged(QuestStep _) => Refresh();
    private void OnStepCompleted(QuestStep _) => Refresh();
    private void OnQuestCompleted(QuestData _) => Refresh();

    /// <summary>Tính lại CompletedSteps/TotalSteps gộp tất cả quest trong khu vực, rồi cập nhật hiển thị.</summary>
    public void Refresh()
    {
        CompletedSteps = 0;
        TotalSteps = 0;

        if (questsInArea != null && QuestManager.Instance != null)
        {
            var current = QuestManager.Instance.CurrentQuest;

            foreach (var quest in questsInArea)
            {
                if (quest == null || quest.steps == null) continue;

                int stepCount = quest.steps.Length;
                TotalSteps += stepCount;

                if (QuestManager.Instance.IsQuestCompleted(quest.questId))
                {
                    // Quest này đã xong hẳn — tính đủ số step của nó.
                    CompletedSteps += stepCount;
                }
                else if (current != null && current.questId == quest.questId)
                {
                    // Quest này đang chạy dở — CurrentStepIndex là số step đã xong của nó.
                    CompletedSteps += Mathf.Clamp(QuestManager.Instance.CurrentStepIndex, 0, stepCount);
                }
                // Chưa từng chạy tới (chưa completed, không phải current) → cộng 0 vào CompletedSteps.
            }
        }

        ApplyDisplay();
    }

    private void ApplyDisplay()
    {
        int percent = Mathf.RoundToInt(CompletionRatio * 100f);
        string formatted = displayFormat
            .Replace("{area}", areaDisplayName)
            .Replace("{percent}", percent.ToString())
            .Replace("{done}", CompletedSteps.ToString())
            .Replace("{total}", TotalSteps.ToString());

        // Ưu tiên gọi qua singleton ở Persistent Scene (Canvas khác scene, không kéo
        // tay được). Nếu không có, fallback dùng statusText gán trực tiếp (cùng scene).
        if (AreaQuestStatusDisplay.Instance != null)
            AreaQuestStatusDisplay.Instance.SetText(formatted);
        else if (statusText != null)
            statusText.text = formatted;
    }
}