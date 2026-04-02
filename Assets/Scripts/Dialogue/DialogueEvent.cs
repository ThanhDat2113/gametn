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
    [Header("Character")]
    public DialogueCharacter character;
    public CharacterPosition position = CharacterPosition.Center;
    
    [Header("Text")]
    [TextArea(3, 5)]
    public string text;
    public DialogueEmotion emotion = DialogueEmotion.Normal;
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
    
    [Header("Other Characters (Hiện cùng lúc)")]
    public OtherCharacter[] otherCharacters;
}

[System.Serializable]
public class OtherCharacter
{
    public DialogueCharacter character;
    public CharacterPosition position;
    public DialogueEmotion emotion = DialogueEmotion.Normal;
}