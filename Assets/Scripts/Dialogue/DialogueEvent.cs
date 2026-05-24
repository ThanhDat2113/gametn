using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "RPG/Dialogue/Event")]
public class DialogueEvent : ScriptableObject
{
    public DialogueLine[] lines;
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    public string emotionKey = "normal";
    [TextArea(3, 5)]
    public string text;
    public float textSpeed = 0.05f;
    public AudioClip voiceClip; // tùy chọn
}