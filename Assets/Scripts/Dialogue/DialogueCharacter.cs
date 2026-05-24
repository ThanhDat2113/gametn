using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "RPG/Dialogue/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string characterName;
    public Color nameColor = Color.white;
    
    [Header("Portraits")]
    public List<PortraitEntry> portraits = new List<PortraitEntry>();
    
    private Dictionary<string, Sprite> portraitDictionary;
    
    public Sprite GetPortrait(string emotionKey)
    {
        if (portraitDictionary == null)
        {
            portraitDictionary = new Dictionary<string, Sprite>();
            foreach (var entry in portraits)
                if (!string.IsNullOrEmpty(entry.emotionKey))
                    portraitDictionary[entry.emotionKey] = entry.sprite;
        }
        
        if (portraitDictionary.TryGetValue(emotionKey, out Sprite sprite))
            return sprite;
        if (portraits.Count > 0) return portraits[0].sprite;
        return null;
    }
    
    public Sprite GetPortrait(DialogueEmotion emotion) => GetPortrait(emotion.ToString().ToLower());
}

[System.Serializable]
public class PortraitEntry
{
    public string emotionKey = "normal";
    public Sprite sprite;
}