using UnityEngine;

public class AutoDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("Kéo DialogueEvent vào đây")]
    public DialogueEvent dialogue;
    
    [Header("Settings")]
    public bool triggerOnStart = true;
    public bool triggerOnEnter = true;
    public bool triggerOnce = true;
    public float delay = 0f;
    
    private bool hasTriggered = false;
    
    private void Start()
    {
        if (triggerOnStart && dialogue != null)
        {
            if (delay > 0)
                Invoke(nameof(PlayDialogue), delay);
            else
                PlayDialogue();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;
        if (triggerOnce && hasTriggered) return;
        if (!other.CompareTag("Player")) return;
        if (dialogue == null) return;
        
        hasTriggered = true;
        
        if (delay > 0)
            Invoke(nameof(PlayDialogue), delay);
        else
            PlayDialogue();
    }
    
    private void PlayDialogue()
    {
        ExtendedDialogueManager.Instance?.PlayDialogue(dialogue);
    }
    
    [ContextMenu("Reset Trigger")]
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}