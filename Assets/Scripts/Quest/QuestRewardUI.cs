using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bảng popup hiện ra khi player hoàn thành quest.
///
/// Hierarchy trong Unity:
///
///   QuestRewardPanel  (GameObject — kéo vào field "panel")
///   └── Window  (Image — khung bảng)
///       ├── TitleText       (TMP — "HOÀN THÀNH NHIỆM VỤ")   → kéo vào titleText
///       ├── RewardLabel     (TMP — "Phần thưởng")            → kéo vào rewardLabelText
///       ├── RewardNameText  (TMP — tên phần thưởng)          → kéo vào rewardNameText
///       └── ConfirmButton   (Button)                         → kéo vào confirmButton
///           └── Text        (TMP — "Xác nhận")
/// </summary>
public class QuestRewardUI : MonoBehaviour
{
    [Header("Panel gốc (ẩn/hiện toàn bộ)")]
    public GameObject panel;

    [Header("Text")]
    [Tooltip("Dòng lớn trên cùng — 'HOÀN THÀNH NHIỆM VỤ'")]
    public TextMeshProUGUI titleText;

    [Tooltip("Dòng nhỏ hơn — 'Phần thưởng'")]
    public TextMeshProUGUI rewardLabelText;

    [Tooltip("Dòng hiển thị tên phần thưởng")]
    public TextMeshProUGUI rewardNameText;

    [Header("Nút xác nhận")]
    public Button confirmButton;

    // Callback thực sự cấp thưởng — gọi sau khi player bấm xác nhận
    private Action _onConfirm;

    // ── Lifecycle ─────────────────────────────────────────────
    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    // ── Public API ────────────────────────────────────────────

    /// <summary>
    /// Hiện bảng phần thưởng.
    /// Nếu rewards rỗng, gọi onConfirm thẳng không hiện bảng.
    /// </summary>
    public void Show(QuestReward[] rewards, Action onConfirm)
    {
        if (rewards == null || rewards.Length == 0)
        {
            onConfirm?.Invoke();
            return;
        }

        _onConfirm = onConfirm;

        // Điền text
        if (titleText != null)
            titleText.text = "HOÀN THÀNH NHIỆM VỤ";

        if (rewardLabelText != null)
            rewardLabelText.text = "Phần thưởng";

        if (rewardNameText != null)
            rewardNameText.text = BuildRewardNames(rewards);

        // Hiện panel
        if (panel != null) panel.SetActive(true);
    }

    // ── Internal ──────────────────────────────────────────────

    /// <summary>
    /// Ghép tên tất cả phần thưởng thành 1 chuỗi.
    /// Nếu có nhiều phần thưởng, mỗi cái trên 1 dòng.
    /// </summary>
    private string BuildRewardNames(QuestReward[] rewards)
    {
        if (rewards.Length == 1)
            return rewards[0].DisplayName();

        var sb = new StringBuilder();
        foreach (var r in rewards)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append(r.DisplayName());
        }
        return sb.ToString();
    }

    private void OnConfirmClicked()
    {
        if (panel != null) panel.SetActive(false);

        var callback = _onConfirm;
        _onConfirm = null;
        callback?.Invoke();   // Thực sự cấp thưởng tại đây
    }
}