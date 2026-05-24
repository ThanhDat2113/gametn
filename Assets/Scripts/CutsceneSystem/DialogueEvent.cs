using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Cutscene/Dialogue Event")]
public class DialogueEvent : ScriptableObject
{
    public DialogueLine[] lines;
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    public DialogueEmotion emotion = DialogueEmotion.Normal;
    [TextArea(3, 5)]
    public string text;
    public float textSpeed = 0.04f;
    public AudioClip voiceClip;
}