using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tutorial dạng ảnh hướng dẫn (slideshow) hiển thị trên PersistentScene — hoàn toàn độc lập
/// với CombatTutorialManager, KHÔNG cần vào CombatScene mới chạy được.
///
/// Dùng lại class ImageSlide đã khai báo public trong CombatTutorialManager.cs (không cần
/// định nghĩa lại vì cùng assembly).
///
/// HÀNH VI:
///   • Lần đầu tiên vào game (chưa từng xem qua) → tự động hiện 1 lần.
///   • Các lần sau → ẩn mặc định. Người chơi bấm nút Help trên HUD (helpButton) để MỞ.
///     Bấm lại helpButton lần nữa (hoặc nút trong panel) để ĐÓNG.
///
/// SETUP:
///   1. Gắn script này vào 1 GameObject trong PersistentScene (script tự DontDestroyOnLoad).
///   2. Gán slideshowPanel, slideImage, prevButton, nextButton, slideCaptionText, slidePageText.
///   3. Gán helpButton — nút "?" đặt cố định trên HUD, bấm để mở/đóng tutorial bất kỳ lúc nào.
///   4. Điền danh sách Slides trong Inspector.
/// </summary>
public class MapTutorialManager : MonoBehaviour
{
    public static MapTutorialManager Instance { get; private set; }

    [Header("Trigger tự động")]
    [Tooltip("Tự động hiện tutorial 1 lần đầu tiên khi vào game, nếu người chơi chưa từng xem.")]
    [SerializeField] private bool autoPlayFirstTime = true;

    [Tooltip("ID lưu trạng thái 'đã xem' qua PlayerPrefs — đổi ID này nếu muốn tách nhiều " +
             "tutorial map khác nhau (vd 'map_basics', 'map_shop', ...).")]
    [SerializeField] private string tutorialId = "map_basics";

    [Header("Slides")]
    [Tooltip("Danh sách ảnh hướng dẫn, xem theo thứ tự từ trên xuống.")]
    [SerializeField] private List<ImageSlide> slides = new List<ImageSlide>();

    [Header("UI")]
    [Tooltip("Panel chứa toàn bộ UI slideshow — bật/tắt để hiện/ẩn tutorial.")]
    [SerializeField] private GameObject slideshowPanel;
    [SerializeField] private Image slideImage;
    [Tooltip("Text chú thích bên dưới ảnh (TMP). Để trống nếu không cần caption.")]
    [SerializeField] private TMP_Text slideCaptionText;
    [Tooltip("Text hiển thị số trang, vd '2 / 5'. Để trống nếu không cần.")]
    [SerializeField] private TMP_Text slidePageText;
    [Tooltip("Nút xem trang TRƯỚC (← Prev). Tự ẩn ở trang đầu.")]
    [SerializeField] private Button prevButton;
    [Tooltip("Nút xem trang SAU (→ Next) — tự đổi label thành 'Đóng' ở trang cuối.")]
    [SerializeField] private Button nextButton;

    [Header("Nút mở lại từ HUD")]
    [Tooltip("Nút cố định trên HUD (icon '?') — bấm để MỞ tutorial (từ slide đầu) khi đang đóng, " +
             "hoặc ĐÓNG tutorial khi đang mở. Có thể để trống nếu bạn tự gọi OpenTutorial()/" +
             "ToggleTutorial() từ script khác.")]
    [SerializeField] private Button helpButton;

    private int _currentSlideIndex = 0;
    private bool _isOpen = false;

    public bool IsOpen => _isOpen;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(OnPrevSlide);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextSlide);
        if (helpButton != null) helpButton.onClick.AddListener(ToggleTutorial);
        if (slideshowPanel != null) slideshowPanel.SetActive(false);

        if (autoPlayFirstTime && !HasSeenTutorial())
        {
            OpenTutorial();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Mở tutorial từ slide đầu tiên. Gọi từ helpButton hoặc từ script khác nếu cần.</summary>
    public void OpenTutorial()
    {
        if (slides == null || slides.Count == 0)
        {
            Debug.LogWarning("[MapTutorialManager] Chưa cấu hình Slides nào trong Inspector.");
            return;
        }

        _isOpen = true;
        _currentSlideIndex = 0;
        if (slideshowPanel != null) slideshowPanel.SetActive(true);
        ShowSlide(_currentSlideIndex);
    }

    /// <summary>Đóng tutorial giữa chừng, kể cả khi chưa xem hết slide.</summary>
    public void CloseTutorial()
    {
        _isOpen = false;
        if (slideshowPanel != null) slideshowPanel.SetActive(false);

        // Đánh dấu đã xem ngay khi đóng (kể cả đóng sớm) — chỉ auto-play lần đầu tiên
        // duy nhất, những lần sau đều là do người chơi chủ động bấm helpButton.
        MarkTutorialSeen();
    }

    /// <summary>Nút Help trên HUD gọi hàm này — mở nếu đang đóng, đóng nếu đang mở.</summary>
    public void ToggleTutorial()
    {
        if (_isOpen) CloseTutorial();
        else OpenTutorial();
    }

    // ── Slideshow ─────────────────────────────────────────────────────────────

    private void ShowSlide(int index)
    {
        if (slides == null || index < 0 || index >= slides.Count) return;

        var slide = slides[index];

        if (slideImage != null)       slideImage.sprite = slide.image;
        if (slideCaptionText != null) slideCaptionText.text = slide.caption;
        if (slidePageText != null)    slidePageText.text = $"{index + 1} / {slides.Count}";

        // Nút Prev: ẩn ở trang đầu
        if (prevButton != null) prevButton.gameObject.SetActive(index > 0);

        // Nút Next: đổi label ở trang cuối thành "Đóng"
        bool isLast = index >= slides.Count - 1;
        if (nextButton != null)
        {
            var label = nextButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = isLast ? "Đóng" : "Tiếp >";
        }
    }

    private void OnPrevSlide()
    {
        if (_currentSlideIndex <= 0) return;
        _currentSlideIndex--;
        ShowSlide(_currentSlideIndex);
    }

    private void OnNextSlide()
    {
        if (slides == null) return;

        if (_currentSlideIndex >= slides.Count - 1)
        {
            // Trang cuối → đóng slideshow
            CloseTutorial();
            return;
        }

        _currentSlideIndex++;
        ShowSlide(_currentSlideIndex);
    }

    // ── PlayerPrefs ───────────────────────────────────────────────────────────

    private string PrefsKey => "MapTutorial_" + tutorialId;

    private bool HasSeenTutorial() => PlayerPrefs.GetInt(PrefsKey, 0) == 1;

    private void MarkTutorialSeen()
    {
        PlayerPrefs.SetInt(PrefsKey, 1);
        PlayerPrefs.Save();
    }
}
