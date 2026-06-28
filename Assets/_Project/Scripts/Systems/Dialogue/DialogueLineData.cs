using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueLine", menuName = "Dialogue/Line")]
public class DialogueLineData : ScriptableObject
{
    public string speakerName = "";
    [TextArea(2, 4)] public string text;
    public Vector2 offset = Vector2.zero;

    [Tooltip("Offset đặc biệt khi tương tác từ bên phải (sẽ swap bubble)")]
    public Vector2 offsetRight = Vector2.zero;

    [Tooltip("True nếu là lời của Player, False nếu là NPC")]
    public bool isPlayerLine = false;
}