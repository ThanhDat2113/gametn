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

        // ✅ Quan trọng: Xóa session data cũ nếu nó không phải từ map (ví dụ data sót lại từ lần test trước)
        if (CombatSessionData.HasData && !CombatSessionData.IsFromMap)
        {
            Debug.Log("[TestStarter] Found stale CombatSessionData (not from map). Clearing it.");
            CombatSessionData.Clear();
        }

        // ✅ Nếu vẫn có data và đến từ map → tắt test starter (không can thiệp vào luồng game thật)
        if (CombatSessionData.IsFromMap)
        {
            Debug.Log("[TestStarter] CombatSessionData is from map. Disabling test starter.");
            enabled = false;
            return;
        }

        // Nếu autoStart được bật, log thông báo (sẽ chạy trong Start())
        if (autoStartOnAwake && enabled)
            Debug.Log("[TestStarter] Standalone mode. Auto-starting combat.");
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

    public bool HasTestData()
    {
        return testEnemyGroup != null && testPlayerCharacters != null && testPlayerCharacters.Length > 0;
    }

    void StartTestCombat()
    {
        if (started) return;
        if (!HasTestData())
        {
            Debug.LogError("[TestStarter] Missing test data!");
            return;
        }

        // Đảm bảo session data sạch trước khi test (phòng ngừa)
        if (CombatSessionData.HasData)
            CombatSessionData.Clear();

        var playerSetup = new List<(CharacterData, int, int)>();
        for (int i = 0; i < testPlayerCharacters.Length; i++)
        {
            if (testPlayerCharacters[i] == null) continue;
            int lvl  = (i < testPlayerLevels.Length) ? testPlayerLevels[i] : 1;
            int slot = (i < testPlayerSlots.Length)  ? testPlayerSlots[i]  : i;
            playerSetup.Add((testPlayerCharacters[i], lvl, slot));
        }

        var enemySetup = new List<(CharacterData, int, int)>();
        foreach (var e in testEnemyGroup.enemies)
            if (e?.data != null)
                enemySetup.Add((e.data, e.level, e.gridSlot));

        combat.StartCombat(playerSetup, enemySetup);
        started = true;
        enabled = false; // Tự disable sau khi đã start
    }
}