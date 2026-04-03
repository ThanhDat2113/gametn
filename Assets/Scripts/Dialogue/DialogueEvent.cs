using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "RPG/Dialogue/Event")]
public class DialogueEvent : ScriptableObject
{
    public DialogueLine[] lines;
    public DialogueEvent nextEvent;
    public bool autoStart = true;
}

[System.Serializable]
public class DialogueLine
{
    [Header("Main Character")]
    public DialogueCharacter character;
    public CharacterPosition position = CharacterPosition.Center;
    
    [Header("Emotion - Có thể nhập bất kỳ key nào")]
    public string emotionKey = "normal";  // Thay vì enum, dùng string
    
    [Header("Text")]
    [TextArea(3, 5)]
    public string text;
    public float textSpeed = 0.05f;
    
    [Header("Portrait Effects")]
    public PortraitEffect portraitEffect = PortraitEffect.None;
    public float effectDuration = 0.3f;
    
    [Header("Background")]
    public Sprite backgroundSprite;
    public BackgroundEffect backgroundEffect = BackgroundEffect.None;
    public float backgroundEffectDuration = 0.5f;
    
    [Header("Audio")]
    public AudioClip voiceClip;
    public AudioClip textSound;
    
    [Header("Other Characters")]
    public OtherCharacter[] otherCharacters;
}

[System.Serializable]
public class OtherCharacter
{
    public DialogueCharacter character;
    public CharacterPosition position;
    public string emotionKey = "normal";
}