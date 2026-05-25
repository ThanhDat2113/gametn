using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Cutscene/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string characterName;
    public Color nameColor = Color.white;
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
        return _portraitDict.TryGetValue(emotionKey, out Sprite s)
            ? s
            : (portraits.Count > 0 ? portraits[0].sprite : null);
    }
}

[System.Serializable]
public class PortraitEntry
{
    public string emotionKey = "normal";
    public Sprite sprite;
}

public enum DialogueEmotion { Normal, Happy, Angry, Sad, Surprised, Cry, Blush }
