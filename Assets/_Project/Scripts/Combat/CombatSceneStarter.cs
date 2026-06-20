using UnityEngine;
using System.Collections;

public class CombatSceneStarter : MonoBehaviour
{
    void Start()
    {
        // Nếu combat đã được start (bởi TestStarter), không can thiệp
        if (CombatManager.Instance != null && CombatManager.Instance.CurrentPhase != CombatPhase.None)
        {
            Debug.Log("[CombatSceneStarter] Combat already started. Skipping.");
            return;
        }

        // Kiểm tra test starter
        var testStarter = GetComponent<CombatTestStarter>();
        bool hasValidTestStarter = testStarter != null && testStarter.enabled && testStarter.HasTestData();

        if (hasValidTestStarter && !CombatSessionData.IsFromMap)
        {
            Debug.Log("[CombatSceneStarter] Test starter detected and not from map. Letting it handle combat.");
            return;
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

            combat.OnVictory += (_) => StartCoroutine(HandleVictory());
            combat.OnDefeat  += () => StartCoroutine(HandleDefeat());
        }
        else
        {
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
        SceneLoaderManager.UnloadCombatScene();
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
        // Không unload combat scene khi thua — DefeatPanel sẽ xử lý (retry/quit)
        // Nếu DefeatPanel không unload, player bị stuck. FIX: đợi panel rồi unload.
        yield return new WaitForSeconds(0.5f);
        // Chờ player nhấn Quit trên DefeatPanel — nếu không có, unload sau 5s
        float timer = 5f;
        while (timer > 0f)
        {
            if (FindFirstObjectByType<DefeatPanel>() == null)
                break; // Panel đã đóng → unload
            timer -= Time.deltaTime;
            yield return null;
        }
        SceneLoaderManager.UnloadCombatScene();
        yield break;
    }

    private static MapEnemy LastTouchedEnemy;
    public static void RegisterLastEnemy(MapEnemy enemy) => LastTouchedEnemy = enemy;
}