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
        // Nếu có CombatSessionData (từ map hoặc retry), TestStarter tự disable
        // để không can thiệp vào combat đang chạy
        if (CombatSessionData.HasData)
        {
            Debug.Log("[TestStarter] Session data detected. Disabling TestStarter.");
            enabled = false;
            return;
        }

        combat = CombatManager.Instance;
        if (combat == null) Debug.LogError("[TestStarter] CombatManager not found!");
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

        // Tạo FormationData từ testPlayerCharacters
        var slotList = new List<FormationSlot>();
        for (int i = 0; i < testPlayerCharacters.Length; i++)
        {
            if (testPlayerCharacters[i] == null) continue;
            slotList.Add(new FormationSlot
            {
                data = testPlayerCharacters[i],
                level = (i < testPlayerLevels.Length) ? testPlayerLevels[i] : 1,
                gridSlot = (i < testPlayerSlots.Length) ? testPlayerSlots[i] : i
            });
        }
        var formation = new FormationData { slots = slotList.ToArray() };

        // Truyền trực tiếp testEnemyGroup gốc để giữ nguyên backgroundImage,
        // bgmClip, introStinger, victoryFanfare... thay vì tạo EnemyGroupData mới.
        combat.StartCombat(formation, testEnemyGroup);
        started = true;
        enabled = false;
    }
}