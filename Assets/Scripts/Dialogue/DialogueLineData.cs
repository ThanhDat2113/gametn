using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueLine", menuName = "Dialogue/Line")]
public class DialogueLineData : ScriptableObject
{
    public string speakerName = "";
    [TextArea(2, 4)] public string text;
    public Vector2 offset = Vector2.zero;
}