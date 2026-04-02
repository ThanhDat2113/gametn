using UnityEngine;

public class CombatDialogueTrigger : MonoBehaviour
{
    [Header("Dialogues")]
    [Tooltip("Kéo DialogueEvent vào đây")]
    public DialogueEvent onVictory;
    public DialogueEvent onDefeat;
    public DialogueEvent onBattleStart;
    
    [Header("Settings")]
    public bool triggerOnce = true;
    public float delay = 0f;
    
    private bool hasTriggeredVictory = false;
    private bool hasTriggeredDefeat = false;
    private bool hasTriggeredStart = false;
    
    private void OnEnable()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnVictory += OnCombatVictory;
            CombatManager.Instance.OnDefeat += OnCombatDefeat;
            CombatManager.Instance.OnCombatStarted += OnCombatStarted;
        }
    }
    
    private void OnDisable()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnVictory -= OnCombatVictory;
            CombatManager.Instance.OnDefeat -= OnCombatDefeat;
            CombatManager.Instance.OnCombatStarted -= OnCombatStarted;
        }
    }
    
    private void OnCombatVictory()
    {
        if (triggerOnce && hasTriggeredVictory) return;
        if (onVictory == null) return;
        
        hasTriggeredVictory = true;
        
        if (delay > 0)
            Invoke(nameof(PlayVictoryDialogue), delay);
        else
            PlayVictoryDialogue();
    }
    
    private void OnCombatDefeat()
    {
        if (triggerOnce && hasTriggeredDefeat) return;
        if (onDefeat == null) return;
        
        hasTriggeredDefeat = true;
        
        if (delay > 0)
            Invoke(nameof(PlayDefeatDialogue), delay);
        else
            PlayDefeatDialogue();
    }
    
    private void OnCombatStarted()
    {
        if (triggerOnce && hasTriggeredStart) return;
        if (onBattleStart == null) return;
        
        hasTriggeredStart = true;
        
        if (delay > 0)
            Invoke(nameof(PlayStartDialogue), delay);
        else
            PlayStartDialogue();
    }
    
    private void PlayVictoryDialogue()
    {
        ExtendedDialogueManager.Instance?.PlayDialogue(onVictory);
    }
    
    private void PlayDefeatDialogue()
    {
        ExtendedDialogueManager.Instance?.PlayDialogue(onDefeat);
    }
    
    private void PlayStartDialogue()
    {
        ExtendedDialogueManager.Instance?.PlayDialogue(onBattleStart);
    }
    
    [ContextMenu("Reset Triggers")]
    public void ResetTriggers()
    {
        hasTriggeredVictory = false;
        hasTriggeredDefeat = false;
        hasTriggeredStart = false;
    }
}