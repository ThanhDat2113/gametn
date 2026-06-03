using System.Collections.Generic;
using UnityEngine;

public class CombatTestStarter : MonoBehaviour
{
    [Header("Test Formation (kéo data vào)")]
    public CharacterData[] testPlayerCharacters;
    public int[] testPlayerLevels = { 1, 1, 1, 1, 1 };
    public int[] testPlayerSlots = { 0, 1, 2, 3, 4 };
    public EnemyGroupData testEnemyGroup;

    [Header("Auto Start")]
    public bool autoStartOnAwake = false;
    public KeyCode startKey = KeyCode.T;

    private CombatManager combat;
    private bool started = false;

    void Awake()
    {
        combat = CombatManager.Instance;
        if (combat == null) Debug.LogError("[TestStarter] CombatManager not found!");

        // Nếu đã có đội hình từ Map → vô hiệu hóa hoàn toàn
        if (FormationDataStorage.PendingFormation != null)
        {
            Debug.Log("[TestStarter] PendingFormation detected from Map. Disabling test starter.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (autoStartOnAwake && !started && enabled)
            StartTestCombat();
    }

    void Update()
    {
        if (!started && enabled && Input.GetKeyDown(startKey))
            StartTestCombat();
    }

    void StartTestCombat()
    {
        if (started) return;
        if (testEnemyGroup == null || testPlayerCharacters.Length == 0)
        {
            Debug.LogError("[TestStarter] Missing test data!");
            return;
        }

        var playerSetup = new List<(CharacterData, int, int)>();
        for (int i = 0; i < testPlayerCharacters.Length; i++)
        {
            if (testPlayerCharacters[i] == null) continue;
            int lvl = (i < testPlayerLevels.Length) ? testPlayerLevels[i] : 1;
            int slot = (i < testPlayerSlots.Length) ? testPlayerSlots[i] : i;
            playerSetup.Add((testPlayerCharacters[i], lvl, slot));
        }

        var enemySetup = new List<(CharacterData, int, int)>();
        foreach (var e in testEnemyGroup.enemies)
            if (e?.data != null)
                enemySetup.Add((e.data, e.level, e.gridSlot));

        combat.StartCombat(playerSetup, enemySetup);
        started = true;
        enabled = false; // chỉ chạy một lần
    }
}