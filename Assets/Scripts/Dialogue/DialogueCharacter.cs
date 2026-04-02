using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "RPG/Dialogue/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string characterName;
    public Sprite defaultPortrait;
    public Sprite happyPortrait;
    public Sprite angryPortrait;
    public Sprite sadPortrait;
    public Sprite surprisedPortrait;
    public Color nameColor = Color.white;
    public AudioClip defaultVoice;
    
    // Vị trí mặc định cho nhân vật
    public CharacterPosition defaultPosition = CharacterPosition.Center;
    
    public Sprite GetPortrait(DialogueEmotion emotion)
    {
        return emotion switch
        {
            DialogueEmotion.Happy => happyPortrait ?? defaultPortrait,
            DialogueEmotion.Angry => angryPortrait ?? defaultPortrait,
            DialogueEmotion.Sad => sadPortrait ?? defaultPortrait,
            DialogueEmotion.Surprised => surprisedPortrait ?? defaultPortrait,
            _ => defaultPortrait
        };
    }
}

public enum CharacterPosition
{
    Left,
    Center,
    Right
}