using UnityEngine;
using TMPro;

/// <summary>
/// Đếm thời gian chơi trong session hiện tại (reset khi tắt game).
/// Hiển thị dạng HH:MM:SS trên bất kỳ TextMeshProUGUI nào được gán.
///
/// SETUP:
///   1. Tạo GameObject mới (vd "PlaytimeTracker") ở scene PersistentScene hoặc DontDestroyOnLoad.
///   2. Add component này vào.
///   3. Gán displayText (TextMeshProUGUI) từ HUD hoặc Pause Menu — hoặc cả hai (xem bên dưới).
///
/// GẮN NHIỀU TEXT CÙNG LÚC:
///   Component cho phép gán một mảng extraTexts[] nếu muốn hiện đồng thời
///   ở HUD lẫn màn hình pause mà không cần script thứ hai.
///
/// DỪNG ĐẾM:
///   Gọi PlaytimeTracker.Instance.SetPaused(true) khi mở pause menu nếu muốn dừng đồng hồ.
///   Mặc định đồng hồ chạy liên tục kể cả khi pause menu mở.
/// </summary>
public class PlaytimeTracker : MonoBehaviour
{
    public static PlaytimeTracker Instance { get; private set; }

    [Header("Hiển thị")]
    [Tooltip("Text chính (vd trên HUD).")]
    [SerializeField] private TextMeshProUGUI displayText;

    [Tooltip("Các text phụ (vd trên màn hình Pause). Tuỳ chọn — để trống nếu không cần.")]
    [SerializeField] private TextMeshProUGUI[] extraTexts;

    [Header("Tuỳ chỉnh")]
    [Tooltip("Dừng đếm khi game bị pause (Time.timeScale = 0).")]
    [SerializeField] private bool pauseWithTimeScale = false;

    // Thời gian thực đã trôi qua (giây) kể từ khi bắt đầu session.
    private float _elapsed;
    private bool _manualPause;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        bool shouldPause = _manualPause
                        || (pauseWithTimeScale && Time.timeScale == 0f);

        if (!shouldPause)
            _elapsed += Time.unscaledDeltaTime;

        // Cập nhật UI mỗi frame — nhẹ vì chỉ ghi string khi giây thay đổi
        UpdateDisplay();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Đặt tạm dừng/tiếp tục đồng hồ thủ công.</summary>
    public void SetPaused(bool paused) => _manualPause = paused;

    /// <summary>Reset đồng hồ về 0 (dùng khi bắt đầu ván mới trong cùng session).</summary>
    public void Reset() => _elapsed = 0f;

    /// <summary>Thời gian hiện tại dạng chuỗi HH:MM:SS.</summary>
    public string GetFormattedTime() => FormatTime(_elapsed);

    /// <summary>Tổng giây đã trôi qua.</summary>
    public float GetElapsedSeconds() => _elapsed;

    // ── Internal ──────────────────────────────────────────────────────────────

    private int _lastDisplayedSecond = -1;

    private void UpdateDisplay()
    {
        int currentSecond = Mathf.FloorToInt(_elapsed);
        if (currentSecond == _lastDisplayedSecond) return; // không rebuild string mỗi frame
        _lastDisplayedSecond = currentSecond;

        string formatted = FormatTime(_elapsed);

        if (displayText != null)
            displayText.text = formatted;

        if (extraTexts != null)
            foreach (var t in extraTexts)
                if (t != null) t.text = formatted;
    }

    private static string FormatTime(float totalSeconds)
    {
        int total = Mathf.FloorToInt(totalSeconds);
        int hours   = total / 3600;
        int minutes = (total % 3600) / 60;
        int seconds = total % 60;
        return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
    }
}
