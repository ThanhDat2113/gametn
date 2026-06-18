using UnityEngine;

[CreateAssetMenu(fileName = "NewGameEvent", menuName = "RPG/GameEvent")]
public class GameEvent : ScriptableObject
{
    [TextArea]
    public string description;
    public EventType eventType;
    public string targetID;
    public int intValue;
    public string stringValue;
    public float floatValue;
    
    public void Execute()
    {
        EventManager.Instance?.TriggerEvent(this);
    }
}

public enum EventType
{
    None,
    SpawnEnemy,
    UnlockZone,
    GiveItem,
    SetFlag,
    StartCombat,
    Teleport,
    PlayCutscene,
    AddQuest,
    CompleteQuest
}