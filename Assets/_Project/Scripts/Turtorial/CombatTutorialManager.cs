using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cách bước tutorial này tiến sang bước kế tiếp.
/// </summary>
public enum TutorialAdvanceMode
{
    /// <summary>Chỉ cần bấm nút "Tiếp theo" — không bắt buộc thao tác thật trong combat.</summary>
    NextButton,

    /// <summary>Bắt buộc người chơi thực hiện đúng hành động (requiredAction) mới qua bước.
    /// Nút "Tiếp theo" bị ẩn ở bước này.</summary>
    WaitForAction,

    /// <summary>Hiện cả nút "Tiếp theo" LẪN tự động qua bước nếu hành động xảy ra trước.</summary>
    Both
}

/// <summary>
/// Hành động thật trong combat mà 1 bước tutorial có thể chờ để tự tiến bước.
/// Chỉ áp dụng khi advanceMode = WaitForAction hoặc Both.
/// </summary>
public enum TutorialActionType
{
    None,
    ActionSubmitted,   // CombatManager.OnActionResolved — người chơi đã chọn unit+skill+target và ra đòn
    TurnEnded,         // CombatManager.OnPlayerTurnEnd — người chơi bấm "Kết thúc lượt"
    APChanged,         // CombatManager.OnAPChanged — AP thay đổi (dùng cho bước dạy về hệ thống AP)
    CombatVictory      // CombatManager.OnVictory — thắng trận (dùng cho bước tổng kết cuối tutorial)
}

[System.Serializable]
public class TutorialStep
{
    [TextArea(2, 4)]
    [Tooltip("Nội dung hiển thị cho bước này.")]
    public string message;

    [Tooltip("Cách tiến sang bước kế — xem tooltip từng giá trị trong enum.")]
    public TutorialAdvanceMode advanceMode = TutorialAdvanceMode.NextButton;

    [Tooltip("CHỈ dùng khi advanceMode = WaitForAction hoặc Both. Hành động thật cần chờ để tự qua bước.")]
    public TutorialActionType requiredAction = TutorialActionType.None;

    [Tooltip("Tuỳ chọn: RectTransform của nút/UI cần highlight ở bước này (vd nút skill, nút End Turn, " +
             "thanh AP...). Để trống nếu bước này không cần highlight gì.")]
    public RectTransform highlightTarget;
}

/// <summary>
/// Quản lý tutorial hướng dẫn cách đánh nhau trong combat. Danh sách bước (TutorialStep)
/// cấu hình hoàn toàn trong Inspector — không cần sửa code để thêm/bớt/đổi thứ tự bước.
///
/// 2 CÁCH DÙNG:
///   1. Tự động chạy 1 lần ở trận combat đầu tiên (autoPlayOnFirstCombat = true) —
///      trạng thái "đã xem" lưu qua PlayerPrefs theo tutorialId, không hiện lại các lần sau.
///   2. Gọi PlayTutorial() từ 1 nút Help trong combat UI để xem lại bất kỳ lúc nào.
///
/// SETUP:
///   1. Gắn script này vào 1 GameObject trong CombatScene (cùng chỗ CombatManager cũng được).
///   2. Gán tutorialPanel, messageText, nextButton (UI hiển thị bước tutorial).
///   3. (Tuỳ chọn) Gán highlightOverlay/highlightRing nếu muốn khoanh vùng UI đang được dạy.
///   4. Điền danh sách "Steps" — mỗi bước 1 message + advanceMode + (tuỳ) requiredAction/highlightTarget.
///   5. Gắn nút Help trong combat UI, OnClick → CombatTutorialManager.Instance.PlayTutorial().
///
/// LƯU Ý: CombatManager hiện tại chỉ có event ở mức "hành động đã ra đòn" (OnActionResolved),
/// không có event riêng cho "đã chọn unit" / "đã chọn skill" / "đã chọn target" từng bước nhỏ
/// (những lựa chọn đó gộp chung vào SubmitPlayerAction). Nên bước dạy "chọn nhân vật + skill +
/// mục tiêu" nên dùng advanceMode = NextButton (không ép buộc từng thao tác nhỏ), hoặc dùng
/// requiredAction = ActionSubmitted để chờ tới khi người chơi ra đòn xong hết cả 3 bước đó.
/// Nếu bạn có script UI chọn unit/skill/target riêng (vd CombatPlanningUI) và muốn tutorial ép
/// buộc TỪNG bước nhỏ đó, gửi mình script đó để thêm hook chính xác hơn.
/// </summary>
public class CombatTutorialManager : MonoBehaviour
{
    public static CombatTutorialManager Instance { get; private set; }

    [Header("Steps")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Trigger tự động")]
    [Tooltip("Tự động chạy tutorial 1 lần khi combat bắt đầu, nếu người chơi chưa từng xem qua.")]
    [SerializeField] private bool autoPlayOnFirstCombat = true;

    [Tooltip("ID lưu trạng thái 'đã xem' qua PlayerPrefs — đổi ID này nếu muốn tách nhiều tutorial khác nhau.")]
    [SerializeField] private string tutorialId = "combat_basics";

    [Header("UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button nextButton;

    [Header("Highlight (tuỳ chọn)")]
    [Tooltip("Overlay làm tối màn hình xung quanh, chỉ bật khi bước hiện tại có highlightTarget.")]
    [SerializeField] private GameObject highlightOverlay;
    [Tooltip("Khung viền di chuyển tới vị trí highlightTarget của từng bước.")]
    [SerializeField] private RectTransform highlightRing;

    private int _currentStepIndex = -1;
    private bool _isRunning = false;

    public bool IsRunning => _isRunning;

    // ── Yêu cầu ép chạy tutorial từ bên ngoài (vd TutorialEnemyTrigger khi chạm quái) ────

    private static bool _forcedRequestPending = false;
    private static bool _forcedIgnoreSeenFlag = false;

    /// <summary>
    /// Gọi hàm này TRƯỚC khi combat bắt đầu (vd lúc chạm quái trên map) để ép
    /// CombatTutorialManager chạy tutorial ngay khi combat khởi động, bất kể
    /// autoPlayOnFirstCombat hay trạng thái "đã xem" hiện tại.
    /// </summary>
    /// <param name="ignoreAlreadySeen">true: luôn chạy dù đã xem rồi. false: chỉ chạy nếu chưa từng xem.</param>
    public static void RequestForcedTutorial(bool ignoreAlreadySeen)
    {
        _forcedRequestPending = true;
        _forcedIgnoreSeenFlag = ignoreAlreadySeen;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNextButtonClicked);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        ClearHighlight();

        if (CombatManager.Instance != null)
            CombatManager.Instance.OnCombatStarted += OnCombatStarted;
        else
            Debug.LogWarning("[CombatTutorialManager] CombatManager.Instance chưa sẵn sàng lúc Start().");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (CombatManager.Instance != null)
            CombatManager.Instance.OnCombatStarted -= OnCombatStarted;
        UnsubscribeCombatEvents();
    }

    private void OnCombatStarted()
    {
        if (_forcedRequestPending)
        {
            bool ignoreSeen = _forcedIgnoreSeenFlag;
            _forcedRequestPending = false;

            if (ignoreSeen || !HasSeenTutorial())
                PlayTutorial();
            return;
        }

        if (autoPlayOnFirstCombat && !HasSeenTutorial())
            PlayTutorial();
    }

    private bool HasSeenTutorial() => PlayerPrefs.GetInt(PrefsKey, 0) == 1;
    private void MarkTutorialSeen() => PlayerPrefs.SetInt(PrefsKey, 1);
    private string PrefsKey => "Tutorial_" + tutorialId;

    /// <summary>Gọi hàm này từ nút Help trong combat UI để xem lại tutorial bất kỳ lúc nào,
    /// kể cả sau khi đã đánh dấu "đã xem".</summary>
    public void PlayTutorial()
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("[CombatTutorialManager] Chưa cấu hình Steps nào trong Inspector.");
            return;
        }

        _isRunning = true;
        _currentStepIndex = -1;
        SubscribeCombatEvents();
        AdvanceStep();
    }

    /// <summary>Dừng tutorial giữa chừng (vd người chơi bấm "Bỏ qua").</summary>
    public void StopTutorial()
    {
        _isRunning = false;
        UnsubscribeCombatEvents();
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        ClearHighlight();
    }

    private void AdvanceStep()
    {
        _currentStepIndex++;
        if (_currentStepIndex >= steps.Count)
        {
            MarkTutorialSeen();
            StopTutorial();
            return;
        }

        ShowStep(steps[_currentStepIndex]);
    }

    private void ShowStep(TutorialStep step)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (messageText != null) messageText.text = step.message;

        bool showNextButton = step.advanceMode == TutorialAdvanceMode.NextButton
                            || step.advanceMode == TutorialAdvanceMode.Both;
        if (nextButton != null) nextButton.gameObject.SetActive(showNextButton);

        ApplyHighlight(step.highlightTarget);
    }

    private void OnNextButtonClicked()
    {
        if (!_isRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count) return;

        var step = steps[_currentStepIndex];
        if (step.advanceMode == TutorialAdvanceMode.NextButton || step.advanceMode == TutorialAdvanceMode.Both)
            AdvanceStep();
    }

    // ── Kết nối với hành động thật trong combat ──────────────────────────

    private void SubscribeCombatEvents()
    {
        var cm = CombatManager.Instance;
        if (cm == null) return;
        cm.OnActionResolved += OnActionResolved_TutorialHook;
        cm.OnPlayerTurnEnd  += OnTurnEnded_TutorialHook;
        cm.OnAPChanged      += OnAPChanged_TutorialHook;
        cm.OnVictory        += OnVictory_TutorialHook;
    }

    private void UnsubscribeCombatEvents()
    {
        var cm = CombatManager.Instance;
        if (cm == null) return;
        cm.OnActionResolved -= OnActionResolved_TutorialHook;
        cm.OnPlayerTurnEnd  -= OnTurnEnded_TutorialHook;
        cm.OnAPChanged      -= OnAPChanged_TutorialHook;
        cm.OnVictory        -= OnVictory_TutorialHook;
    }

    private void TryAdvanceOnAction(TutorialActionType actionType)
    {
        if (!_isRunning || _currentStepIndex < 0 || _currentStepIndex >= steps.Count) return;

        var step = steps[_currentStepIndex];
        if (step.requiredAction != actionType) return;

        if (step.advanceMode == TutorialAdvanceMode.WaitForAction || step.advanceMode == TutorialAdvanceMode.Both)
            AdvanceStep();
    }

    private void OnActionResolved_TutorialHook(ActionResult _) => TryAdvanceOnAction(TutorialActionType.ActionSubmitted);
    private void OnTurnEnded_TutorialHook() => TryAdvanceOnAction(TutorialActionType.TurnEnded);
    private void OnAPChanged_TutorialHook(int _) => TryAdvanceOnAction(TutorialActionType.APChanged);
    private void OnVictory_TutorialHook(Dictionary<CharacterData, int> _) => TryAdvanceOnAction(TutorialActionType.CombatVictory);

    // ── Highlight ─────────────────────────────────────────────────────────

    private void ApplyHighlight(RectTransform target)
    {
        if (highlightOverlay != null) highlightOverlay.SetActive(target != null);

        if (highlightRing == null) return;

        if (target == null)
        {
            highlightRing.gameObject.SetActive(false);
            return;
        }

        highlightRing.gameObject.SetActive(true);
        highlightRing.position = target.position;
        highlightRing.sizeDelta = target.sizeDelta;
    }

    private void ClearHighlight()
    {
        if (highlightOverlay != null) highlightOverlay.SetActive(false);
        if (highlightRing != null) highlightRing.gameObject.SetActive(false);
    }
}
