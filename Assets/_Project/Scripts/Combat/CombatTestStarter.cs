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

        // ✅ Chỉ clear session data nếu KHÔNG phải từ map
        // (tránh trường hợp data từ map trước đó còn sót lại, khiến CombatSceneStarter
        //  chạy combat cũ thay vì dùng test data của người dùng)
        if (CombatSessionData.HasData && !CombatSessionData.IsFromMap)
        {
            Debug.Log("[TestStarter] Found stale CombatSessionData (not from map). Clearing it for test run.");
            CombatSessionData.Clear();
        }

        // Nếu autoStart được bật, log thông báo (sẽ chạy trong Start())
        if (autoStartOnAwake)
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