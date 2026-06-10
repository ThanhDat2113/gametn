using UnityEngine;
using System.Collections;

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

            combat.OnVictory += () => StartCoroutine(HandleVictory());
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
        Debug.Log("[CombatSceneStarter] Victory - Xóa enemy và cập nhật quest.");
        if (CombatAudioManager.Instance != null)
            CombatAudioManager.Instance.StopBGM();

        if (LastTouchedEnemy != null)
        {
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