using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Dạng tutorial: TextStep = hướng dẫn text từng bước (dạng cũ),
/// ImageSlideshow = xem ảnh hướng dẫn với nút Prev/Next.
/// </summary>
public enum TutorialMode { TextStep, ImageSlideshow }

/// <summary>Cách 1 bước tutorial tiến sang bước kế tiếp.</summary>
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
    CombatVictory,     // CombatManager.OnVictory — thắng trận (dùng cho bước tổng kết cuối tutorial)
    UIClicked          // Người chơi click vào highlightTarget của bước này
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

    [Tooltip("Tuỳ chọn: RectTransform của nút/UI cần highlight ở bước này (vd nút End Turn, " +
             "thanh AP...). Để trống nếu bước này không cần highlight gì, HOẶC nếu target là UI được " +
             "tạo động lúc runtime (xem highlightTargetName bên dưới) — trong trường hợp đó KHÔNG THỂ " +
             "gán trực tiếp ở đây vì object chưa tồn tại lúc thiết kế scene.\n" +
             "Nếu requiredAction = UIClicked, đây chính là vùng người chơi phải bấm vào để qua bước.")]
    public RectTransform highlightTarget;

    [Tooltip("CHỈ dùng khi highlightTarget để trống — dành cho UI được tạo động lúc runtime và " +
             "KHÔNG thể kéo-thả sẵn trong Inspector (vd nút chọn nhân vật do SpawnUnitViews tạo ra, " +
             "hoặc nút skill chỉ xuất hiện sau khi người chơi chọn 1 nhân vật). " +
             "Điền tên (hoặc 1 phần tên) của GameObject cần tìm — script sẽ tự dò trong scene mỗi khi " +
             "bước này đang active, và tự gắn lại nếu UI đó bị destroy/tạo lại (vd danh sách nút skill " +
             "refresh lại khi đổi nhân vật đang chọn). Không phân biệt hoa/thường, chỉ cần chứa chuỗi này.")]
    public string highlightTargetName;
}

/// <summary>
/// Một trang ảnh hướng dẫn trong dạng ImageSlideshow.
/// </summary>
[System.Serializable]
public class ImageSlide
{
    [Tooltip("Ảnh hướng dẫn hiển thị ở trang này.")]
    public Sprite image;

    [TextArea(1, 3)]
    [Tooltip("Chú thích phía dưới ảnh (tuỳ chọn, để trống nếu không cần).")]
    public string caption;
}

 ///Danh sách bước (TutorialStep)
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
/// VỀ UIClicked:
///   Đặt requiredAction = UIClicked + highlightTarget = RectTransform muốn người chơi bấm vào.
///   highlightTarget phải có Raycast Target = true (Image mặc định là true).
///   highlightOverlay nếu có phải để Raycast Target = false để click xuyên qua được.
/// </summary>
public class CombatTutorialManager : MonoBehaviour
{
    public static CombatTutorialManager Instance { get; private set; }

    [Header("Tutorial Mode")]
    [Tooltip("TextStep: dạng cũ — hướng dẫn text từng bước với highlight UI.\n" +
             "ImageSlideshow: xem ảnh hướng dẫn, có nút Prev/Next để lật trang.")]
    [SerializeField] private TutorialMode tutorialMode = TutorialMode.TextStep;

    [Header("Steps")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Image Slideshow")]
    [Tooltip("Danh sách ảnh hướng dẫn — chỉ dùng khi tutorialMode = ImageSlideshow.")]
    [SerializeField] private List<ImageSlide> slides = new List<ImageSlide>();
    [Tooltip("Panel chứa UI slideshow (ẩn/hiện độc lập với tutorialPanel của TextStep).")]
    [SerializeField] private GameObject slideshowPanel;
    [Tooltip("Image component hiển thị ảnh slide.")]
    [SerializeField] private Image slideImage;
    [Tooltip("Text chú thích bên dưới ảnh (TMP). Để trống nếu không cần caption.")]
    [SerializeField] private TMP_Text slideCaptionText;
    [Tooltip("Nút xem trang TRƯỚC (← Prev).")]
    [SerializeField] private Button prevButton;
    [Tooltip("Nút xem trang SAU (→ Next) — cũng là nút đóng ở trang cuối.")]
    [SerializeField] private Button nextSlideButton;
    [Tooltip("Text hiển thị số trang, vd '2 / 5'. Để trống nếu không cần.")]
    [SerializeField] private TMP_Text slidePageText;

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
    [Tooltip("Overlay làm tối màn hình xung quanh, chỉ bật khi bước hiện tại có highlightTarget.\n" +
             "Đặt Raycast Target = false trên Image của overlay để click xuyên qua được.")]
    [SerializeField] private GameObject highlightOverlay;
    [Tooltip("Khung viền di chuyển tới vị trí highlightTarget của từng bước.")]
    [SerializeField] private RectTransform highlightRing;

    private int _currentStepIndex = -1;
    private bool _isRunning = false;
    private ClickDetector _activeClickDetector;
    private Coroutine _dynamicTargetCoroutine;
    private GameObject _clickDetectorHost;
    private GameObject _nonUIClickTarget;

    // ── Slideshow state ───────────────────────────────────────────────────────
    private int _currentSlideIndex = 0;

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

    private Coroutine _subscribeCoroutine;

    private void Start()
    {
        if (nextButton != null)      nextButton.onClick.AddListener(OnNextButtonClicked);
        if (prevButton != null)      prevButton.onClick.AddListener(OnPrevSlide);
        if (nextSlideButton != null) nextSlideButton.onClick.AddListener(OnNextSlide);
        if (tutorialPanel != null)   tutorialPanel.SetActive(false);
        if (slideshowPanel != null)  slideshowPanel.SetActive(false);
        ClearHighlight();

        _subscribeCoroutine = StartCoroutine(SubscribeToCombatManagerRoutine());
    }

    private void Update()
    {
        if (_nonUIClickTarget == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // Bỏ qua nếu chuột đang trên UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null) return;

        // Thử 2D raycast trước (sprite/SpriteRenderer), rồi 3D
        var ray = cam.ScreenPointToRay(Input.mousePosition);

        var hit2D = Physics2D.GetRayIntersection(ray);
        if (hit2D.collider != null)
        {
            if (IsTargetOrChild(hit2D.collider.gameObject, _nonUIClickTarget))
            {
                OnHighlightTargetClicked();
                return;
            }
        }

        if (Physics.Raycast(ray, out RaycastHit hit3D))
        {
            if (IsTargetOrChild(hit3D.collider.gameObject, _nonUIClickTarget))
            {
                OnHighlightTargetClicked();
            }
        }
    }

    private bool IsTargetOrChild(GameObject hit, GameObject target)
    {
        var t = hit.transform;
        while (t != null)
        {
            if (t.gameObject == target) return true;
            t = t.parent;
        }
        return false;
    }

    /// <summary>
    /// Chờ CombatManager chuyển sang phase PlayerTurn — thời điểm intro camera
    /// (fade-in + pan + zoom-out) đã hoàn tất — rồi mới PlayTutorial().
    /// Không cần thêm event hay sửa bất kỳ script nào khác.
    /// </summary>
    private IEnumerator SubscribeToCombatManagerRoutine()
    {
        // Chờ CombatManager.Instance sẵn sàng
        while (CombatManager.Instance == null)
            yield return null;

        var cm = CombatManager.Instance;

        // Nếu phase chưa đến PlayerTurn → chờ
        while (cm.CurrentPhase != CombatPhase.PlayerTurn)
            yield return null;

        Debug.Log("[CombatTutorialManager] PlayerTurn bắt đầu — hiện tutorial.");
        OnCombatStarted();

        _subscribeCoroutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (CombatManager.Instance != null)
            CombatManager.Instance.OnCombatStarted -= OnCombatStarted;
        UnsubscribeCombatEvents();
        RemoveClickDetector();
        StopDynamicTargetSearch();
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
        if (tutorialMode == TutorialMode.ImageSlideshow)
        {
            PlaySlideshow();
            return;
        }

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

    // ── ImageSlideshow ────────────────────────────────────────────────────────

    private void PlaySlideshow()
    {
        if (slides == null || slides.Count == 0)
        {
            Debug.LogWarning("[CombatTutorialManager] Chưa cấu hình Slides nào trong Inspector.");
            return;
        }

        _isRunning = true;
        _currentSlideIndex = 0;
        if (slideshowPanel != null) slideshowPanel.SetActive(true);
        ShowSlide(_currentSlideIndex);
    }

    private void ShowSlide(int index)
    {
        if (slides == null || index < 0 || index >= slides.Count) return;

        var slide = slides[index];

        if (slideImage != null)        slideImage.sprite = slide.image;
        if (slideCaptionText != null)  slideCaptionText.text = slide.caption;
        if (slidePageText != null)     slidePageText.text = $"{index + 1} / {slides.Count}";

        // Nút Prev: ẩn ở trang đầu
        if (prevButton != null) prevButton.gameObject.SetActive(index > 0);

        // Nút Next: đổi label ở trang cuối thành "Đóng" (tuỳ chỉnh label qua Inspector)
        bool isLast = index >= slides.Count - 1;
        if (nextSlideButton != null)
        {
            var label = nextSlideButton.GetComponentInChildren<TMP_Text>();
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
            StopSlideshow();
            return;
        }

        _currentSlideIndex++;
        ShowSlide(_currentSlideIndex);
    }

    private void StopSlideshow()
    {
        _isRunning = false;
        if (slideshowPanel != null) slideshowPanel.SetActive(false);
        MarkTutorialSeen();
    }

    /// <summary>Dừng tutorial giữa chừng (vd người chơi bấm "Bỏ qua").</summary>
    public void StopTutorial()
    {
        _isRunning = false;
        RemoveClickDetector();
        StopDynamicTargetSearch();
        UnsubscribeCombatEvents();
        if (tutorialPanel != null)  tutorialPanel.SetActive(false);
        if (slideshowPanel != null) slideshowPanel.SetActive(false);
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
        if (messageText != null)  messageText.text = step.message;

        bool showNextButton = step.advanceMode == TutorialAdvanceMode.NextButton
                            || step.advanceMode == TutorialAdvanceMode.Both;
        if (nextButton != null) nextButton.gameObject.SetActive(showNextButton);

        RemoveClickDetector();
        StopDynamicTargetSearch();

        if (step.highlightTarget != null)
        {
            // Target tĩnh, đã gán sẵn trong Inspector (UI có sẵn trên scene) — như cũ.
            SetupHighlightAndClickDetector(step, step.highlightTarget);
        }
        else if (!string.IsNullOrEmpty(step.highlightTargetName))
        {
            // Target động (vd nút chọn nhân vật do SpawnUnitViews tạo ra lúc combat bắt đầu,
            // hoặc nút skill chỉ xuất hiện sau khi chọn nhân vật) — chưa chắc tồn tại ngay lúc
            // bước này bắt đầu, nên phải dò liên tục thay vì tìm 1 lần.
            ClearHighlight();
            _dynamicTargetCoroutine = StartCoroutine(WatchForDynamicTarget(step));
        }
        else
        {
            ClearHighlight();
        }
    }

    private void SetupHighlightAndClickDetector(TutorialStep step, RectTransform target)
    {
        ApplyHighlight(target);
        AttachClickDetectorIfNeeded(step, target.gameObject);
    }

    // Overload cho 3D/2D GameObject không có RectTransform (vd UnitView sprite trong BattleField)
    private void SetupHighlightAndClickDetector(TutorialStep step, GameObject target)
    {
        // Không apply highlight ring vì target không phải UI
        bool needsClickDetect = step.requiredAction == TutorialActionType.UIClicked
                             && (step.advanceMode == TutorialAdvanceMode.WaitForAction
                              || step.advanceMode == TutorialAdvanceMode.Both);
        if (needsClickDetect)
            _nonUIClickTarget = target;
    }

    private void AttachClickDetectorIfNeeded(TutorialStep step, GameObject host)
    {
        bool needsClickDetect = step.requiredAction == TutorialActionType.UIClicked
                             && (step.advanceMode == TutorialAdvanceMode.WaitForAction
                              || step.advanceMode == TutorialAdvanceMode.Both);
        if (!needsClickDetect) return;

        // Đảm bảo có Physics2DRaycaster / PhysicsRaycaster trên camera nếu là 3D object
        _clickDetectorHost = host;
        _activeClickDetector = host.GetComponent<ClickDetector>()
                            ?? host.AddComponent<ClickDetector>();
        _activeClickDetector.OnClicked = OnHighlightTargetClicked;
    }

    /// <summary>
    /// Liên tục dò tìm target động (nút chọn nhân vật / nút skill...) trong suốt thời gian
    /// bước tutorial này đang active. Cần lặp thay vì tìm 1 lần vì:
    ///   - Nhân vật (UnitView) chỉ được Instantiate sau khi combat bắt đầu (SpawnUnitViews),
    ///     có thể muộn hơn lúc tutorial bắt đầu hiện bước này.
    ///   - Nút skill thường chỉ xuất hiện SAU KHI người chơi chọn 1 nhân vật, và danh sách nút
    ///     skill có thể bị Destroy & tạo lại mỗi lần đổi nhân vật đang chọn.
    /// Khi target biến mất (bị Destroy), vòng lặp tự tìm lại và gắn ClickDetector mới.
    /// </summary>
    private IEnumerator WatchForDynamicTarget(TutorialStep step)
    {
        var wait = new WaitForSeconds(0.15f);
        GameObject currentTarget = null;

        while (true)
        {
            if (currentTarget == null)
            {
                var found = FindDynamicTarget(step.highlightTargetName);
                if (found != null)
                {
                    currentTarget = found;
                    RemoveClickDetector();

                    var rt = found.GetComponent<RectTransform>();
                    if (rt != null)
                        SetupHighlightAndClickDetector(step, rt);       // UI element
                    else
                        SetupHighlightAndClickDetector(step, found);    // 3D/2D sprite
                }
                else
                {
                    ClearHighlight();
                }
            }
            else if (currentTarget == null || !currentTarget.activeInHierarchy)
            {
                // Target cũ đã bị destroy hoặc ẩn đi (vd đổi nhân vật khác) — reset để tìm lại.
                currentTarget = null;
                RemoveClickDetector();
                ClearHighlight();
            }

            yield return wait;
        }
    }

    /// <summary>
    /// Tìm GameObject đang active trong scene có tên chứa targetName (không phân biệt hoa/thường).
    /// Tìm cả UI (RectTransform) lẫn 3D/2D GameObject (vd UnitView sprite trong BattleField).
    /// Lưu ý: đặt tên GameObject đủ riêng biệt để tránh trùng khớp nhầm với object khác.
    /// </summary>
    private GameObject FindDynamicTarget(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        // Tìm UI trước (RectTransform) — ưu tiên cao hơn vì tên thường rõ ràng hơn
        var allRects = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        foreach (var rt in allRects)
        {
            if (rt != null && rt.gameObject.activeInHierarchy &&
                rt.name.IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return rt.gameObject;
            }
        }

        // Tìm 3D/2D GameObject (vd Eugeo(Clone), Saber(Clone)...)
        var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go != null && go.activeInHierarchy &&
                go.name.IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return go;
            }
        }

        return null;
    }

    private void StopDynamicTargetSearch()
    {
        if (_dynamicTargetCoroutine != null)
        {
            StopCoroutine(_dynamicTargetCoroutine);
            _dynamicTargetCoroutine = null;
        }
    }

    private void OnHighlightTargetClicked()
    {
        TryAdvanceOnAction(TutorialActionType.UIClicked);
    }

    private void RemoveClickDetector()
    {
        if (_activeClickDetector != null)
        {
            _activeClickDetector.OnClicked = null;
            Destroy(_activeClickDetector);
            _activeClickDetector = null;
        }
        _clickDetectorHost = null;
        _nonUIClickTarget = null;
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
    private void OnTurnEnded_TutorialHook()                    => TryAdvanceOnAction(TutorialActionType.TurnEnded);
    private void OnAPChanged_TutorialHook(int _)               => TryAdvanceOnAction(TutorialActionType.APChanged);
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