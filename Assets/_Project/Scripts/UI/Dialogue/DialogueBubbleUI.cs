using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueBubbleUI : MonoBehaviour
{
    public static DialogueBubbleUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject bubbleRoot;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI contentText;
    public Image background;

    [Header("Settings")]
    public Vector2 defaultOffset = new Vector2(0, 1.5f);
    public KeyCode continueKey = KeyCode.Space;
    public bool clickToContinue = true;

    [Header("Typing Effect")]
    [Tooltip("Tốc độ hiện chữ (giây mỗi ký tự). 0 = hiện ngay lập tức.")]
    public float typingSpeed = 0.03f;
    [Tooltip("Bật/tắt âm thanh gõ chữ")]
    public bool typingSoundEnabled = true;

    private Camera _cam;
    private bool _isShowing;
    private System.Action _onHide;
    private System.Action _onComplete;
    private Coroutine _typingCoroutine;
    private bool _isTyping;

    public bool IsShowing => _isShowing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        bubbleRoot.SetActive(false);
    }

    void Update()
    {
        if (!_isShowing) return;

        // Quan trọng: KHÔNG để việc xoay bubble theo camera làm gãy việc bắt input.
        // Nếu Camera.main null/đổi camera (VD: cutscene chuyển sang cutsceneCamera
        // không gắn tag MainCamera), trước đây dòng xoay sẽ ném NullReferenceException
        // ngay tại Update() khiến đoạn check click/space phía dưới KHÔNG BAO GIỜ chạy.
        UpdateBillboard();

        bool keyPressed = Input.GetKeyDown(continueKey) || (clickToContinue && Input.GetMouseButtonDown(0));
        if (keyPressed)
        {
            // Nếu đang typing → hiện ngay toàn bộ text thay vì skip dòng
            if (_isTyping)
            {
                CompleteTyping();
            }
            else
            {
                Hide();
            }
        }
    }

    private void UpdateBillboard()
    {
        // Tự refetch nếu cache bị mất, thay vì chỉ lấy 1 lần duy nhất ở Awake.
        if (_cam == null)
            _cam = Camera.main;

        if (_cam == null)
            return; // không có camera hợp lệ -> bỏ qua xoay, không phá input

        bubbleRoot.transform.rotation = _cam.transform.rotation;
    }

    public void Show(DialogueLineData line, Transform target, System.Action onHide = null)
    {
        if (_isShowing) Hide();
        Vector2 offset = line.offset == Vector2.zero ? defaultOffset : line.offset;
        bubbleRoot.transform.position = target.position + (Vector3)offset;
        speakerText.text = line.speakerName;
        bubbleRoot.SetActive(true);
        _isShowing = true;
        _onHide = onHide;

        // Bắt đầu typing effect (hoặc hiện ngay nếu typingSpeed = 0)
        if (typingSpeed > 0f)
        {
            _typingCoroutine = StartCoroutine(TypeText(line.text));
        }
        else
        {
            contentText.text = line.text;
        }
    }

    public void ShowSequential(DialogueLineData[] lines, Transform target, System.Action onComplete = null, int startIndex = 0)
    {
        if (startIndex >= lines.Length)
        {
            Hide();
            onComplete?.Invoke();
            return;
        }
        Show(lines[startIndex], target, () => ShowSequential(lines, target, onComplete, startIndex + 1));
    }

    private IEnumerator TypeText(string fullText)
    {
        _isTyping = true;
        contentText.text = "";

        foreach (char c in fullText)
        {
            contentText.text += c;

            // Phát âm thanh gõ chữ (có cooldown bên trong AudioManager)
            if (typingSoundEnabled)
            {
                AudioManager.Instance?.PlayUITyping();
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;
        _typingCoroutine = null;
    }

    /// <summary>
    /// Hiện ngay toàn bộ text khi đang typing (click/space giữa chừng).
    /// </summary>
    private void CompleteTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        _isTyping = false;

        // Dừng typing sound ngay lập tức
        AudioManager.Instance?.StopUITyping();
    }

    private void Hide()
    {
        if (!_isShowing) return;
        _isShowing = false;

        // Dừng typing + phát advance sound
        CompleteTyping();
        AudioManager.Instance?.PlayUIDialogueAdvance();

        bubbleRoot.SetActive(false);
        var callback = _onHide;
        _onHide = null;
        callback?.Invoke();
    }
}