using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapDialogueController : MonoBehaviour
{
    public static MapDialogueController Instance { get; private set; }

    [Header("UI Prefab")]
    public GameObject dialogueBubblePrefab; // Panel có Text, NameText, Portrait (tuỳ chọn)
    public Transform bubbleParent;          // Canvas

    [Header("Input")]
    public KeyCode continueKey = KeyCode.Space;
    public bool clickToContinue = true;

    private GameObject currentBubble;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI nameText;
    private Queue<DialogueLine> lines = new Queue<DialogueLine>();
    private bool isPlaying = false;
    private Coroutine typingCoroutine;
    private string currentFullText;
    private System.Action onDialogueEnd;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (Input.GetKeyDown(continueKey) || (clickToContinue && Input.GetMouseButtonDown(0)))
        {
            Continue();
        }
    }

    public void StartDialogue(DialogueEvent dialogueEvent, Transform speaker = null, System.Action onEnd = null)
    {
        if (isPlaying) return;
        lines.Clear();
        foreach (var line in dialogueEvent.lines) lines.Enqueue(line);
        onDialogueEnd = onEnd;

        if (currentBubble == null)
            currentBubble = Instantiate(dialogueBubblePrefab, bubbleParent);
        if (speaker != null)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(speaker.position + Vector3.up * 1.5f);
            currentBubble.transform.position = screenPos;
        }
        else
        {
            currentBubble.transform.position = new Vector2(Screen.width / 2, 150);
        }

        dialogueText = currentBubble.GetComponentInChildren<TextMeshProUGUI>();
        nameText = currentBubble.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        isPlaying = true;
        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        var line = lines.Dequeue();
        if (nameText != null && line.character != null)
        {
            nameText.text = line.character.characterName;
            nameText.color = line.character.nameColor;
        }
        currentFullText = line.text;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line.text, line.textSpeed));
    }

    private IEnumerator TypeText(string text, float speed)
    {
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(speed);
        }
        typingCoroutine = null;
    }

    private void Continue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            dialogueText.text = currentFullText;
            return;
        }
        ShowNextLine();
    }

    private void EndDialogue()
    {
        isPlaying = false;
        if (currentBubble != null) Destroy(currentBubble);
        onDialogueEnd?.Invoke();
    }

    public bool IsPlaying() => isPlaying;
}