using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtendedDialogueManager : MonoBehaviour
{
    public static ExtendedDialogueManager Instance { get; private set; }
    
    [Header("References")]
    public DialogueBoxUI dialogueBox;
    
    [Header("Queue Settings")]
    public bool canQueueDialogues = true;
    public float queueDelay = 0.5f;
    
    private Queue<DialogueEvent> dialogueQueue = new Queue<DialogueEvent>();
    private bool isPlaying = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Play trực tiếp DialogueEvent
    public void PlayDialogue(DialogueEvent dialogueEvent, System.Action onComplete = null)
    {
        if (dialogueEvent == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        if (canQueueDialogues && isPlaying)
        {
            dialogueQueue.Enqueue(dialogueEvent);
            Debug.Log($"[DialogueManager] Queue: {dialogueEvent.name}");
        }
        else
        {
            StartCoroutine(PlayDialogueCoroutine(dialogueEvent, onComplete));
        }
    }
    
    private IEnumerator PlayDialogueCoroutine(DialogueEvent dialogueEvent, System.Action onComplete)
    {
        isPlaying = true;
        
        if (dialogueBox != null)
        {
            dialogueBox.Show();
            
            foreach (var line in dialogueEvent.lines)
            {
                bool lineComplete = false;
                dialogueBox.DisplayLine(line, () => lineComplete = true);
                yield return new WaitUntil(() => lineComplete);
            }
            
            dialogueBox.Hide();
            yield return new WaitForSeconds(0.1f);
        }
        
        onComplete?.Invoke();
        
        if (dialogueQueue.Count > 0)
        {
            yield return new WaitForSeconds(queueDelay);
            var next = dialogueQueue.Dequeue();
            StartCoroutine(PlayDialogueCoroutine(next, null));
        }
        else
        {
            isPlaying = false;
        }
    }
    
    public void ClearQueue()
    {
        dialogueQueue.Clear();
    }
    
    public bool IsPlaying() => isPlaying;
}