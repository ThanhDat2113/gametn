using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatSceneStarter : MonoBehaviour
{
    void Start()
    {
        // 👈 Kiểm tra test starter trước tiên
        var testStarter = GetComponent<CombatTestStarter>();
        bool hasValidTestStarter = testStarter != null && testStarter.enabled && testStarter.HasTestData();

        if (hasValidTestStarter && !CombatSessionData.IsFromMap)
        {
            // Chỉ ưu tiên test starter khi không phải từ map (tức đang chạy scene test trực tiếp)
            Debug.Log("[CombatSceneStarter] Test starter detected and not from map. Letting it handle combat.");
            return; // để test starter tự xử lý
        }

        // Nếu có dữ liệu từ map → chạy bình thường
        if (CombatSessionData.HasData)
        {
            var combat = CombatManager.Instance;
            if (combat == null)
            {
                Debug.LogError("[CombatSceneStarter] Không tìm thấy CombatManager trong CombatScene!");
                return;
            }

            Debug.Log($"[CombatSceneStarter] StartCombat from Map: formation={CombatSessionData.Formation?.slots?.Length} slots, enemy={CombatSessionData.EnemyGroup?.name}");

            combat.StartCombat(CombatSessionData.Formation, CombatSessionData.EnemyGroup);

            combat.OnVictory += (_) => StartCoroutine(HandleVictory());  // SỬA: lambda bỏ qua tham số
            combat.OnDefeat  += () => StartCoroutine(HandleDefeat());
        }
        else
        {
            // Không có session data và không có test starter hợp lệ
            if (!hasValidTestStarter)
                Debug.LogError("[CombatSceneStarter] Không có CombatSessionData và không có TestStarter hợp lệ!");
        }
    }

    private IEnumerator HandleVictory()
    {
        Debug.Log("[CombatSceneStarter] Victory - Xóa enemy, drop loot và cập nhật quest.");
        if (CombatAudioManager.Instance != null)
            CombatAudioManager.Instance.StopBGM();

        if (LastTouchedEnemy != null)
        {
            // 🎲 Roll loot từ enemy
            var loots = LastTouchedEnemy.GetLoot();
            if (loots.Count > 0 && InventoryManager.Instance != null)
            {
                Debug.Log($"[CombatSceneStarter] Enemy có {loots.Count} loot entries. Bắt đầu roll...");
                foreach (var entry in loots)
                {
                    if (entry.item == null) continue;

                    float roll = Random.value;
                    if (roll <= entry.dropRate)
                    {
                        int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                        if (amount > 0)
                        {
                            InventoryManager.Instance.AddItem(entry.item, amount);
                            Debug.Log($"[Drop] +{amount}x {entry.item.itemName} (rate: {entry.dropRate * 100}%, roll: {roll:F2})");
                        }
                    }
                    else
                    {
                        Debug.Log($"[Drop] {entry.item.itemName} không drop (rate: {entry.dropRate * 100}%, roll: {roll:F2})");
                    }
                }
            }

            // 📜 Cập nhật quest
            if (QuestManager.Instance != null && LastTouchedEnemy.enemyGroup != null)
                QuestManager.Instance.OnEnemyGroupDefeated(LastTouchedEnemy.enemyGroup);

            LastTouchedEnemy.MarkAsDefeated();
        }

        CombatSessionData.Clear();
        yield break;
    }

    private IEnumerator HandleDefeat()
    {
        Debug.Log("[CombatSceneStarter] Defeat - showing defeat panel.");
        if (CombatAudioManager.Instance != null)
            CombatAudioManager.Instance.StopBGM();

        DefeatPanel defeatPanel = FindFirstObjectByType<DefeatPanel>(FindObjectsInactive.Include);
        if (defeatPanel != null)
        {
            defeatPanel.Show();
        }
        else
        {
            Debug.LogError("[CombatSceneStarter] Không tìm thấy DefeatPanel trong scene!");
            yield return new WaitForSeconds(2f);
            SceneLoaderManager.UnloadCombatScene();
        }
        yield break;
    }

    private static MapEnemy LastTouchedEnemy;
    public static void RegisterLastEnemy(MapEnemy enemy) => LastTouchedEnemy = enemy;
}