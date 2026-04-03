using UnityEngine;

/// <summary>
/// Auto-starts the battle when scene loads
/// Attach to BattleManager GameObject
/// </summary>
public class AutoStartBattle : MonoBehaviour
{
    [SerializeField] private SimpleBattleSystem battleSystem;

    private void Start()
    {
        if (battleSystem == null)
            battleSystem = GetComponent<SimpleBattleSystem>();

        if (battleSystem == null)
        {
            Debug.LogError("❌ SimpleBattleSystem not found! Attach this script to BattleManager.");
            return;
        }

        Debug.Log("✅ Auto-starting battle in 1 second...");
        Invoke(nameof(StartCutscene), 1f);
    }

    private void StartCutscene()
    {
        battleSystem.StartBattle();
    }
}
