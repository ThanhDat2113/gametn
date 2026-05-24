using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Cutscene/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string characterName;
    public Color nameColor = Color.white;

    [Header("Portraits")]
    public List<PortraitEntry> portraits = new List<PortraitEntry>();

    private Dictionary<string, Sprite> _portraitDict;

    public Sprite GetPortrait(string emotionKey)
    {
        if (_portraitDict == null)
        {
            _portraitDict = new Dictionary<string, Sprite>();
            foreach (var entry in portraits)
                if (!string.IsNullOrEmpty(entry.emotionKey))
                    _portraitDict[entry.emotionKey] = entry.sprite;
        }
        if (_portraitDict.TryGetValue(emotionKey, out Sprite s)) return s;
        return portraits.Count > 0 ? portraits[0].sprite : null;
    }

    public Sprite GetPortrait(DialogueEmotion emotion) =>
        GetPortrait(emotion.ToString().ToLower());
}

[System.Serializable]
public class PortraitEntry
{
    public string emotionKey = "normal";
    public Sprite sprite;
}

public enum DialogueEmotion
{
    Normal, Happy, Angry, Sad, Surprised, Cry, Blush
}