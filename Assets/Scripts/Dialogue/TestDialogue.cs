using UnityEngine;

public class TestDialogue : MonoBehaviour
{
    [Header("Test Settings")]
    public KeyCode testKey = KeyCode.T;
    public DialogueEvent testDialogue;  // Kéo file .asset vào đây
    
    void Update()
    {
        if (Input.GetKeyDown(testKey) && testDialogue != null)
        {
            ExtendedDialogueManager.Instance.PlayDialogue(testDialogue);
        }
    }
}