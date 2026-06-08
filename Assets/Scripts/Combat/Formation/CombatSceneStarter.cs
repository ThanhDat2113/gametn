using UnityEngine;
using System.Collections;

public class CombatSceneStarter : MonoBehaviour
{
    public EnemyGroupData enemyGroup; // fallback nếu không có pending

    public static EnemyGroupData PendingEnemyGroup { get; set; }

    void Start()
    {
        var pendingFormation = FormationDataStorage.PendingFormation;
        var pendingEnemy = PendingEnemyGroup != null ? PendingEnemyGroup : enemyGroup;

        if (pendingFormation == null)
        {
            var testStarter = GetComponent<CombatTestStarter>();
            if (testStarter != null && testStarter.enabled)
            {
                Debug.Log("[CombatSceneStarter] Không có pending data. Chuyển xử lý cho CombatTestStarter.");
                return;
            }
            Debug.LogError("[CombatSceneStarter] Không có pending data và không có TestStarter!");
            return;
        }

        Debug.Log($"[CombatSceneStarter] Formation: OK Enemy: {(pendingEnemy == null ? "NULL" : pendingEnemy.name)}");

        if (pendingEnemy == null)
        {
            Debug.LogError("[CombatSceneStarter] Enemy group is null!");
            if (SceneLoaderManager.Instance != null) ReturnToMapAfterError();
            return;
        }

        var combat = CombatManager.Instance;
        if (combat != null)
        {
            combat.StartCombat(pendingFormation, pendingEnemy);

            FormationDataStorage.PendingFormation = null;
            PendingEnemyGroup = null;

            combat.OnVictory += () => StartCoroutine(HandleVictory());
            combat.OnDefeat += () => StartCoroutine(HandleDefeat());
        }
        else
        {
            Debug.LogError("Không tìm thấy CombatManager trong CombatScene!");
        }
    }

    /// <summary>
    /// Xử lý khi thắng: xóa enemy, cập nhật quest, nhưng KHÔNG unload scene.
    /// Việc unload sẽ do VictoryPanel xử lý sau khi người chơi click.
    /// </summary>
    private IEnumerator HandleVictory()
    {
        Debug.Log("[CombatSceneStarter] Victory - Xóa enemy và cập nhật quest.");
        if (CombatAudioManager.Instance != null)
            CombatAudioManager.Instance.StopBGM();

        // Xóa enemy đã trigger và cập nhật quest (giống logic cũ)
        if (LastTouchedEnemy != null)
        {
            if (QuestManager.Instance != null && LastTouchedEnemy.enemyGroup != null)
                QuestManager.Instance.OnEnemyGroupDefeated(LastTouchedEnemy.enemyGroup);
            LastTouchedEnemy.MarkAsDefeated();
        }

        // Không gọi UnloadCombatScene ở đây – để VictoryPanel xử lý khi người chơi click
        yield break;
    }

    /// <summary>
    /// Xử lý khi thua: hiển thị panel defeat, fade, rồi unload scene.
    /// </summary>
    private IEnumerator HandleDefeat()
    {
        Debug.Log("[CombatSceneStarter] Defeat - tự động quay về map.");
        if (CombatAudioManager.Instance != null)
            CombatAudioManager.Instance.StopBGM();

        var resultUI = FindFirstObjectByType<CombatResultUI>();
        if (resultUI != null)
            yield return resultUI.ShowResult(false);
        else
            yield return new WaitForSeconds(1f);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

        if (LastTouchedEnemy != null)
        {
            if (QuestManager.Instance != null && LastTouchedEnemy.enemyGroup != null)
                QuestManager.Instance.OnEnemyGroupDefeated(LastTouchedEnemy.enemyGroup);
            LastTouchedEnemy.MarkAsDefeated();
        }

        SceneLoaderManager.UnloadCombatScene();
    }

    private static MapEnemy LastTouchedEnemy;
    public static void RegisterLastEnemy(MapEnemy enemy) => LastTouchedEnemy = enemy;

    private void ReturnToMapAfterError()
    {
        if (FadeController.Instance != null)
            FadeController.Instance.FadeToBlack(() => SceneLoaderManager.UnloadCombatScene());
        else
            SceneLoaderManager.UnloadCombatScene();
    }
}