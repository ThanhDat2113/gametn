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

    // Static flag để các UI khác kiểm tra
    public static bool IsDialogueActive { get; private set; }

    // Phương thức để set flag từ bên ngoài (DialogueTrigger)
    public static void SetDialogueActive(bool active)
    {
        IsDialogueActive = active;
        Debug.Log($"[DialogueBubbleUI] SetDialogueActive = {active}");
    }

    [Header("Bubble Prefabs")]
    public GameObject npcBubblePrefab;
    public GameObject playerBubblePrefab;

    [Header("Settings")]
    public Vector2 defaultOffset = new Vector2(0, 1.5f);
    public KeyCode continueKey = KeyCode.Space;
    public bool clickToContinue = true;

    [Header("Skip")]
    [Tooltip("Nhấn phím này để skip toàn bộ sequence hội thoại hiện tại")]
    public KeyCode skipKey = KeyCode.Backspace;

    [Header("Directional Settings")]
    [Tooltip("Khoảng cách dịch ngang thêm (world units) khi bubble ở bên phải/trái")]
    public float horizontalOffset = 1.5f;

    [Header("Typing Effect")]
    public float typingSpeed = 0.03f;
    public bool typingSoundEnabled = true;

    private GameObject _currentBubbleInstance;
    private TextMeshProUGUI _speakerText;
    private TextMeshProUGUI _contentText;
    private bool _isShowing;
    private System.Action _onHide;
    private System.Action _sequenceCompleteCallback;
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
        IsDialogueActive = false;
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

        if (Input.GetKeyDown(skipKey))
            SkipAll();
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

    public void Show(DialogueLineData line, Transform npcTarget, Transform playerTarget,
                     InteractionSide? side = null, System.Action onHide = null)
    {
        if (_isShowing) Hide();

        // KHÔNG set flag ở đây – DialogueTrigger đã set trước
        _onHide = onHide;
        _currentFullText = line.text;

        GameObject selectedPrefab;
        Transform target;
        bool isSwapped = false;

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

        if (target == null)
        {
            Debug.LogWarning("[DialogueBubbleUI] Target is null, using fallback.");
            target = (line.isPlayerLine) ? playerTarget : npcTarget;
            if (target == null)
            {
                Debug.LogError("[DialogueBubbleUI] Both targets are null! Cannot display bubble.");
                return;
            }
        }

        if (selectedPrefab == null)
        {
            Debug.LogError("[DialogueBubbleUI] Missing prefab!");
            return;
        }

        _currentBubbleInstance = Instantiate(selectedPrefab, transform);
        _currentBubbleInstance.name = $"Bubble_{(line.isPlayerLine ? "Player" : "NPC")}";

        _speakerText = GetComponentInChildren<TextMeshProUGUI>(_currentBubbleInstance, "SpeakerName");
        if (_speakerText == null)
        {
            var allTexts = _currentBubbleInstance.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (allTexts.Length >= 2)
            {
                _speakerText = allTexts[0];
                _contentText = allTexts[1];
            }
            else if (allTexts.Length == 1)
            {
                _contentText = allTexts[0];
            }
        }
        else
        {
            _contentText = GetComponentInChildren<TextMeshProUGUI>(_currentBubbleInstance, "Content");
        }

        if (_speakerText != null)
            _speakerText.text = line.speakerName;
        else
            Debug.LogWarning("[DialogueBubbleUI] SpeakerName TextMeshProUGUI not found!");

        if (_contentText != null)
            _contentText.text = "";
        else
            Debug.LogWarning("[DialogueBubbleUI] Content TextMeshProUGUI not found!");

        Vector2 offset;
        if (isSwapped && line.offsetRight != Vector2.zero)
            offset = line.offsetRight;
        else
            offset = line.offset == Vector2.zero ? defaultOffset : line.offset;

        if (side.HasValue)
        {
            float dir = (side.Value == InteractionSide.Right) ? 1f : -1f;
            offset = new Vector2(offset.x + dir * horizontalOffset, offset.y);
        }

        _currentBubbleInstance.transform.position = target.position + (Vector3)offset;
        _currentBubbleInstance.SetActive(true);
        _isShowing = true;

        if (typingSpeed > 0f)
            _typingCoroutine = StartCoroutine(TypeText(line.text));
        else if (_contentText != null)
            _contentText.text = line.text;
    }

    public void ShowSequential(DialogueLineData[] lines, Transform npcTarget, Transform playerTarget,
                               System.Action onComplete = null, int startIndex = 0, InteractionSide? side = null)
    {
        if (startIndex == 0)
        {
            // KHÔNG set flag ở đây
            _sequenceCompleteCallback = onComplete;
        }

        if (startIndex >= lines.Length)
        {
            _sequenceCompleteCallback = null;
            Hide();
            onComplete?.Invoke();
            return;
        }

        Show(lines[startIndex], npcTarget, playerTarget, side,
             () => ShowSequential(lines, npcTarget, playerTarget, onComplete, startIndex + 1, side));
    }

    public void SkipAll()
    {
        if (!_isShowing) return;

        CompleteTyping();

        if (_currentBubbleInstance != null)
        {
            Destroy(_currentBubbleInstance);
            _currentBubbleInstance = null;
        }

        _isShowing = false;
        // KHÔNG set flag ở đây
        _onHide = null;

        var finalCallback = _sequenceCompleteCallback;
        _sequenceCompleteCallback = null;

        AudioManager.Instance?.PlayUIDialogueAdvance();
        finalCallback?.Invoke();
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
        // KHÔNG set flag ở đây
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