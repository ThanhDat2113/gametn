using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "RPG/Dialogue/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string characterName;
    public Color nameColor = Color.white;
    public AudioClip defaultVoice;
    public CharacterPosition defaultPosition = CharacterPosition.Center;
    
    [Header("Portraits - Có thể thêm không giới hạn")]
    public List<PortraitEntry> portraits = new List<PortraitEntry>();
    
    private Dictionary<string, Sprite> portraitDictionary;
    
    public void Initialize()
    {
        portraitDictionary = new Dictionary<string, Sprite>();
        foreach (var entry in portraits)
        {
            if (!string.IsNullOrEmpty(entry.emotionKey))
            {
                portraitDictionary[entry.emotionKey] = entry.sprite;
            }
        }
    }
    
    public Sprite GetPortrait(string emotionKey)
    {
        if (portraitDictionary == null) Initialize();
        
        if (portraitDictionary.TryGetValue(emotionKey, out Sprite sprite))
            return sprite;
        
        // Fallback: trả về portrait đầu tiên nếu không tìm thấy
        if (portraits.Count > 0)
            return portraits[0].sprite;
        
        return null;
    }
    
    // Hàm tiện lợi cho các emotion phổ biến
    public Sprite GetPortrait(DialogueEmotion emotion)
    {
        string key = emotion.ToString().ToLower();
        return GetPortrait(key);
    }
}

[System.Serializable]
public class PortraitEntry
{
    [Tooltip("Tên key: normal, happy, angry, sad, surprised, cry, blush, etc.")]
    public string emotionKey = "normal";
    public Sprite sprite;
}