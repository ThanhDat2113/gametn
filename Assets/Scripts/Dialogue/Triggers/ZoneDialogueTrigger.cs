using UnityEngine;

public class ZoneDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("Kéo DialogueEvent vào đây")]
    public DialogueEvent dialogueOnEnter;
    public DialogueEvent dialogueOnExit;
    
    [Header("Settings")]
    public bool triggerOnce = true;
    public float delay = 0f;
    
    private bool hasTriggeredEnter = false;
    private bool hasTriggeredExit = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggeredEnter) return;
        if (!other.CompareTag("Player")) return;
        if (dialogueOnEnter == null) return;
        
        hasTriggeredEnter = true;
        
        if (delay > 0)
            Invoke(nameof(PlayEnterDialogue), delay);
        else
            PlayEnterDialogue();
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (triggerOnce && hasTriggeredExit) return;
        if (!other.CompareTag("Player")) return;
        if (dialogueOnExit == null) return;
        
        hasTriggeredExit = true;
        
        if (delay > 0)
            Invoke(nameof(PlayExitDialogue), delay);
        else
            PlayExitDialogue();
    }
    
    private void PlayEnterDialogue()
    {
        ExtendedDialogueManager.Instance?.PlayDialogue(dialogueOnEnter);
    }
    
    private void PlayExitDialogue()
    {
        ExtendedDialogueManager.Instance?.PlayDialogue(dialogueOnExit);
    }
    
    [ContextMenu("Reset Trigger")]
    public void ResetTrigger()
    {
        hasTriggeredEnter = false;
        hasTriggeredExit = false;
    }
}