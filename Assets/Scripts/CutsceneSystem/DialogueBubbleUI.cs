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

    private Camera _cam;
    private bool _isShowing;
    private System.Action _onHide;
    private System.Action _onComplete;

    public bool IsShowing => _isShowing;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
        _cam = Camera.main;
        bubbleRoot.SetActive(false);
    }

    void Update()
    {
        if (!_isShowing) return;
        bubbleRoot.transform.rotation = _cam.transform.rotation;
        bool keyPressed = Input.GetKeyDown(continueKey) || (clickToContinue && Input.GetMouseButtonDown(0));
        if (keyPressed) Hide();
    }

    public void Show(DialogueLineData line, Transform target, System.Action onHide = null)
    {
        if (_isShowing) Hide();
        Vector2 offset = line.offset == Vector2.zero ? defaultOffset : line.offset;
        bubbleRoot.transform.position = target.position + (Vector3)offset;
        speakerText.text = line.speakerName;
        contentText.text = line.text;
        bubbleRoot.SetActive(true);
        _isShowing = true;
        _onHide = onHide;
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

    private void Hide()
    {
        if (!_isShowing) return;
        _isShowing = false;
        bubbleRoot.SetActive(false);
        var callback = _onHide;
        _onHide = null;
        callback?.Invoke();
    }
}