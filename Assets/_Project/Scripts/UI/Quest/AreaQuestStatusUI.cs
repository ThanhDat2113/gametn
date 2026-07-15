using UnityEngine;
using TMPro;

/// <summary>
/// Gắn vào 1 GameObject riêng trong MỖI scene/khu vực (Map1, Map2, CombatScene...).
/// Hiển thị số nhiệm vụ đã hoàn thành / tổng số nhiệm vụ được cấu hình cho khu vực đó.
///
/// KHÔNG cần sửa gì ở QuestManager/QuestData/QuestStep — chỉ dựa vào
/// QuestManager.Instance.IsQuestCompleted(questId) đã có sẵn (kiểm tra theo lịch sử
/// quest đã hoàn thành), nên hoạt động được với cả quest đã xong từ trước hoặc quest
/// thuộc scene khác (không cần đang active).
///
/// SETUP:
///   1. Tạo 1 GameObject trong scene (vd "AreaQuestStatus"), gắn script này.
///   2. Điền areaDisplayName (vd "Khu rừng phía Bắc").
///   3. Kéo tất cả QuestData asset thuộc khu vực này vào questsInArea.
///   4. (Tuỳ chọn) Gán statusText nếu muốn tự hiển thị lên UI — để trống nếu bạn
///      chỉ cần đọc CompletedCount/TotalCount/CompletionRatio từ script khác.
/// </summary>
public class AreaQuestStatusUI : MonoBehaviour
{
    [Header("Khu vực")]
    [Tooltip("Tên hiển thị của khu vực này (vd: 'Khu rừng phía Bắc', 'Thị trấn Silvane'...).")]
    [SerializeField] private string areaDisplayName = "Khu vực";

    [Tooltip("Danh sách QuestData thuộc khu vực này. Kéo các quest asset liên quan vào đây — " +
             "quest có thể đã hoàn thành từ trước hoặc chưa từng active, không quan trọng.")]
    [SerializeField] private QuestData[] questsInArea;

    [Header("UI (tuỳ chọn)")]
    [Tooltip("Để trống nếu không cần tự hiển thị text — script khác vẫn đọc được " +
             "CompletedCount/TotalCount/CompletionRatio qua public property.")]
    [SerializeField] private TMP_Text statusText;

    [Tooltip("Format hiển thị. {area}=tên khu vực, {done}=số đã xong, {total}=tổng số, " +
             "{percent}=phần trăm làm tròn (vd 60).")]
    [SerializeField] private string displayFormat = "{area}: {done}/{total} nhiệm vụ hoàn thành";

    /// <summary>Số quest đã hoàn thành trong danh sách questsInArea (tính lại mỗi lần Refresh()).</summary>
    public int CompletedCount { get; private set; }

    /// <summary>Tổng số quest được cấu hình cho khu vực này.</summary>
    public int TotalCount => questsInArea?.Length ?? 0;

    /// <summary>Tỉ lệ hoàn thành từ 0..1. Trả về 0 nếu chưa cấu hình quest nào.</summary>
    public float CompletionRatio => TotalCount > 0 ? (float)CompletedCount / TotalCount : 0f;

    /// <summary>True nếu tất cả quest trong khu vực đã hoàn thành (và có ít nhất 1 quest được cấu hình).</summary>
    public bool IsAreaFullyCompleted => TotalCount > 0 && CompletedCount >= TotalCount;

    private void Start()
    {
        Refresh();

        if (QuestManager.Instance != null)
        {
            // Bất kỳ quest nào hoàn thành (không chỉ quest của khu vực này) đều có thể
            // ảnh hưởng tới _completedQuestIds, nên refresh lại mỗi lần có quest xong
            // là đơn giản và chắc chắn nhất — tránh phải đoán quest nào thuộc khu vực nào.
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
            QuestManager.Instance.OnQuestCompleted.RemoveListener(OnQuestCompleted);
    }

    private void OnQuestCompleted(QuestData _) => Refresh();

    /// <summary>Tính lại CompletedCount/TotalCount và cập nhật statusText nếu có gán.</summary>
    public void Refresh()
    {
        CompletedCount = 0;

        if (questsInArea != null && QuestManager.Instance != null)
        {
            foreach (var quest in questsInArea)
            {
                if (quest == null) continue;
                if (QuestManager.Instance.IsQuestCompleted(quest.questId))
                    CompletedCount++;
            }
        }

        if (statusText != null)
        {
            int percent = TotalCount > 0 ? Mathf.RoundToInt(CompletionRatio * 100f) : 0;
            statusText.text = displayFormat
                .Replace("{area}", areaDisplayName)
                .Replace("{done}", CompletedCount.ToString())
                .Replace("{total}", TotalCount.ToString())
                .Replace("{percent}", percent.ToString());
        }
    }
}
