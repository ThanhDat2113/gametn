using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum InteractionSide
{
    Left,
    Right
}

public class DialogueBubbleUI : MonoBehaviour
{
    public static DialogueBubbleUI Instance { get; private set; }

    [Header("Bubble Prefabs")]
    public GameObject npcBubblePrefab;
    public GameObject playerBubblePrefab;

    [Header("Settings")]
    public Vector2 defaultOffset = new Vector2(0, 1.5f);
    public KeyCode continueKey = KeyCode.Space;
    public bool clickToContinue = true;

    [Header("Directional Settings")]
    [Tooltip("Khoảng cách dịch ngang thêm (world units) khi bubble ở bên phải/trái (chỉ áp dụng nếu không dùng offsetRight)")]
    public float horizontalOffset = 1.5f;

    [Header("Typing Effect")]
    public float typingSpeed = 0.03f;
    public bool typingSoundEnabled = true;

    private GameObject _currentBubbleInstance;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _contentText;
    private bool _isShowing;
    private System.Action _onHide;
    private Coroutine _typingCoroutine;
    private bool _isTyping;
    private string _currentFullText;

    public bool IsShowing => _isShowing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (!_isShowing || _currentBubbleInstance == null) return;
        UpdateBillboard();

        bool keyPressed = Input.GetKeyDown(continueKey) || (clickToContinue && Input.GetMouseButtonDown(0));
        if (keyPressed)
        {
            if (_isTyping)
                CompleteTyping();
            else
                Hide();
        }
    }

    private void UpdateBillboard()
    {
        if (_currentBubbleInstance != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                _currentBubbleInstance.transform.rotation = cam.transform.rotation;
        }
    }

    /// <summary>
    /// Hiển thị một dòng hội thoại với bubble phù hợp.
    /// </summary>
    public void Show(DialogueLineData line, Transform npcTarget, Transform playerTarget, InteractionSide? side = null, System.Action onHide = null)
    {
        if (_isShowing) Hide();

        _onHide = onHide;
        _currentFullText = line.text;

        // Xác định prefab và target
        GameObject selectedPrefab;
        Transform target;
        bool isSwapped = false; // true nếu swap bubble (tương tác phải)

        if (side == InteractionSide.Right)
        {
            isSwapped = true;
            if (line.isPlayerLine)
            {
                selectedPrefab = npcBubblePrefab;
                target = playerTarget;
            }
            else
            {
                selectedPrefab = playerBubblePrefab;
                target = npcTarget;
            }
        }
        else
        {
            if (line.isPlayerLine)
            {
                selectedPrefab = playerBubblePrefab;
                target = playerTarget;
            }
            else
            {
                selectedPrefab = npcBubblePrefab;
                target = npcTarget;
            }
        }

        if (selectedPrefab == null)
        {
            Debug.LogError("[DialogueBubbleUI] Missing prefab!");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[DialogueBubbleUI] Target is null, cannot display bubble.");
            return;
        }

        // Tạo instance
        _currentBubbleInstance = Instantiate(selectedPrefab, transform);
        _currentBubbleInstance.name = $"Bubble_{(line.isPlayerLine ? "Player" : "NPC")}";

        // Lấy components
        _speakerText = GetComponentInChildren<TextMeshProUGUI>(_currentBubbleInstance, "SpeakerName");
        _contentText = GetComponentInChildren<TextMeshProUGUI>(_currentBubbleInstance, "Content");

        // Set text
        if (_speakerText != null) _speakerText.text = line.speakerName;
        if (_contentText != null) _contentText.text = "";

        // --- Xác định offset ---
        Vector2 offset;

        // Nếu đang swap (tương tác phải) và có offsetRight riêng
        if (isSwapped && line.offsetRight != Vector2.zero)
        {
            // Dùng offsetRight trực tiếp, KHÔNG cộng thêm horizontalOffset
            offset = line.offsetRight;
        }
        else
        {
            // Dùng offset mặc định hoặc offset từ line, sau đó cộng thêm horizontalOffset nếu có side
            offset = line.offset == Vector2.zero ? defaultOffset : line.offset;

            // Thêm horizontalOffset cho cả hai trường hợp (trừ khi đã dùng offsetRight)
            if (side.HasValue)
            {
                float dir = (side.Value == InteractionSide.Right) ? 1f : -1f;
                offset = new Vector2(offset.x + dir * horizontalOffset, offset.y);
            }
        }

        // Đặt vị trí
        _currentBubbleInstance.transform.position = target.position + (Vector3)offset;
        _currentBubbleInstance.SetActive(true);
        _isShowing = true;

        // Bắt đầu typing
        if (typingSpeed > 0f)
        {
            _typingCoroutine = StartCoroutine(TypeText(line.text));
        }
        else
        {
            if (_contentText != null) _contentText.text = line.text;
        }
    }

    public void ShowSequential(DialogueLineData[] lines, Transform npcTarget, Transform playerTarget, System.Action onComplete = null, int startIndex = 0, InteractionSide? side = null)
    {
        if (startIndex >= lines.Length)
        {
            Hide();
            onComplete?.Invoke();
            return;
        }
        Show(lines[startIndex], npcTarget, playerTarget, side, () => ShowSequential(lines, npcTarget, playerTarget, onComplete, startIndex + 1, side));
    }

    private IEnumerator TypeText(string fullText)
    {
        _isTyping = true;
        if (_contentText != null) _contentText.text = "";

        foreach (char c in fullText)
        {
            if (_contentText != null) _contentText.text += c;
            if (typingSoundEnabled)
                AudioManager.Instance?.PlayUITyping();
            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;
        _typingCoroutine = null;
    }

    private void CompleteTyping()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        if (!string.IsNullOrEmpty(_currentFullText) && _contentText != null)
            _contentText.text = _currentFullText;
        _isTyping = false;
        AudioManager.Instance?.StopUITyping();
    }

    private void Hide()
    {
        if (!_isShowing) return;
        _isShowing = false;
        CompleteTyping();
        AudioManager.Instance?.PlayUIDialogueAdvance();

        if (_currentBubbleInstance != null)
        {
            Destroy(_currentBubbleInstance);
            _currentBubbleInstance = null;
        }

        var callback = _onHide;
        _onHide = null;
        callback?.Invoke();
    }

    private T GetComponentInChildren<T>(GameObject root, string name) where T : Component
    {
        if (root == null) return null;
        foreach (T comp in root.GetComponentsInChildren<T>(true))
        {
            if (comp.gameObject.name == name)
                return comp;
        }
        return null;
    }
}