using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton điều khiển hộp thoại JRPG.
/// Được gọi bởi DialogueBehaviour (Timeline) hoặc trực tiếp từ code.
/// </summary>
public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    public GameObject continueIndicator; // mũi tên nhấp nháy

    [Header("Input")]
    public KeyCode continueKey = KeyCode.Z;
    public bool clickToContinue = true;

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioSource textBlipSource;
    public AudioClip defaultBlip;

    // Internal state
    private Queue<DialogueLine> _lineQueue = new Queue<DialogueLine>();
    private Coroutine _typingCoroutine;
    private string _currentFullText;
    private bool _isPlaying;
    private bool _isTyping;
    private System.Action _onComplete;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (!_isPlaying) return;
        bool advance = Input.GetKeyDown(continueKey) ||
                       (clickToContinue && Input.GetMouseButtonDown(0));
        if (advance) HandleAdvance();
    }

    // ─────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────

    public void StartDialogue(DialogueEvent dialogueEvent, System.Action onComplete = null)
    {
        if (_isPlaying) return;

        _onComplete = onComplete;
        _lineQueue.Clear();
        foreach (var line in dialogueEvent.lines)
            _lineQueue.Enqueue(line);

        _isPlaying = true;
        dialoguePanel.SetActive(true);
        ShowNextLine();
    }

    public bool IsPlaying => _isPlaying;

    // ─────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────

    void ShowNextLine()
    {
        if (_lineQueue.Count == 0) { EndDialogue(); return; }

        var line = _lineQueue.Dequeue();

        // Name & portrait
        if (nameText != null)
        {
            nameText.text = line.character != null ? line.character.characterName : "";
            nameText.color = line.character != null ? line.character.nameColor : Color.white;
        }

        if (portraitImage != null && line.character != null)
        {
            var sprite = line.character.GetPortrait(line.emotion);
            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
        }

        // Voice clip
        if (voiceSource != null && line.voiceClip != null)
        {
            voiceSource.clip = line.voiceClip;
            voiceSource.Play();
        }

        // Typewriter
        _currentFullText = line.text;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeText(line.text, line.textSpeed));

        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    IEnumerator TypeText(string text, float speed)
    {
        _isTyping = true;
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            if (textBlipSource != null && defaultBlip != null && c != ' ')
                textBlipSource.PlayOneShot(defaultBlip, 0.3f);
            yield return new WaitForSeconds(speed);
        }
        _isTyping = false;
        _typingCoroutine = null;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    void HandleAdvance()
    {
        if (_isTyping)
        {
            // Skip typewriter
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
            _isTyping = false;
            dialogueText.text = _currentFullText;
            if (continueIndicator != null) continueIndicator.SetActive(true);
            return;
        }
        ShowNextLine();
    }

    void EndDialogue()
    {
        _isPlaying = false;
        dialoguePanel.SetActive(false);
        if (voiceSource != null) voiceSource.Stop();
        var cb = _onComplete;
        _onComplete = null; // xóa trước khi gọi để tránh lặp
        cb?.Invoke();
    }
}