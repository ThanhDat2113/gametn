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

        // Chỉ chạy khi có dữ liệu từ map
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
            Debug.Log("[CombatSceneStarter] No session data (likely test mode). Skipping.");
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
        // KHÔNG unload scene ở đây - để VictoryPanel.OnContinue() xử lý sau khi cộng EXP
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
        yield return new WaitForSeconds(0.5f);
        float timer = 5f;
        while (timer > 0f)
        {
            if (FindFirstObjectByType<DefeatPanel>() == null)
                break;
            timer -= Time.deltaTime;
            yield return null;
        }
        SceneLoaderManager.UnloadCombatScene();
        yield break;
    }

    private static MapEnemy LastTouchedEnemy;
    public static void RegisterLastEnemy(MapEnemy enemy) => LastTouchedEnemy = enemy;
}