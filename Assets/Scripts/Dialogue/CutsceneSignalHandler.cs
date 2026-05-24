using UnityEngine;

public class CutsceneSignalHandler : MonoBehaviour
{
    public DialogueEvent dialogueEvent;
    public Transform speaker;

    public void ShowDialogue()
    {
        MapDialogueController.Instance.StartDialogue(dialogueEvent, speaker);
    }
}