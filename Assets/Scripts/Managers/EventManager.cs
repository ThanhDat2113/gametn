using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }
    
    private Dictionary<EventType, List<Action<GameEvent>>> eventListeners = new Dictionary<EventType, List<Action<GameEvent>>>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void RegisterListener(EventType eventType, Action<GameEvent> listener)
    {
        if (!eventListeners.ContainsKey(eventType))
            eventListeners[eventType] = new List<Action<GameEvent>>();
        
        eventListeners[eventType].Add(listener);
    }
    
    public void UnregisterListener(EventType eventType, Action<GameEvent> listener)
    {
        if (eventListeners.ContainsKey(eventType))
            eventListeners[eventType].Remove(listener);
    }
    
    public void TriggerEvent(GameEvent gameEvent)
    {
        if (eventListeners.ContainsKey(gameEvent.eventType))
        {
            foreach (var listener in eventListeners[gameEvent.eventType])
            {
                listener?.Invoke(gameEvent);
            }
        }
        
        Debug.Log($"[EventManager] Triggered: {gameEvent.eventType} - {gameEvent.description}");
    }
}