using UnityEngine;

public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("Kéo DialogueEvent vào đây")]
    public DialogueEvent dialogue;
    
    [Header("Interaction")]
    public string interactionKey = "e";
    public GameObject interactionPrompt;
    public bool canRepeat = true;
    
    [Header("Auto Play")]
    public bool autoPlayOnApproach = false;
    public float autoPlayDelay = 0f;
    
    private bool playerInRange = false;
    private bool hasPlayed = false;
    
    private void Update()
    {
        if (!canRepeat && hasPlayed) return;
        
        if (playerInRange && Input.GetKeyDown(interactionKey) && dialogue != null)
        {
            PlayDialogue();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInRange = true;
        
        if (interactionPrompt != null)
            interactionPrompt.SetActive(true);
        
        if (autoPlayOnApproach && !hasPlayed)
        {
            if (autoPlayDelay > 0)
                Invoke(nameof(PlayDialogue), autoPlayDelay);
            else
                PlayDialogue();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInRange = false;
        
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
    
    private void PlayDialogue()
    {
        if (dialogue == null) return;
        
        hasPlayed = true;
        ExtendedDialogueManager.Instance?.PlayDialogue(dialogue);
    }
    
    [ContextMenu("Reset NPC")]
    public void ResetNPC()
    {
        hasPlayed = false;
    }
}