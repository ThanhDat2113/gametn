using UnityEngine;

public class CombatSceneStarter : MonoBehaviour
{
    public EnemyGroupData enemyGroup; // fallback nếu không có pending

    // Biến tĩnh để nhận dữ liệu từ MapEnemy
    public static EnemyGroupData PendingEnemyGroup { get; set; }

    void Start()
    {
        var pendingFormation = FormationDataStorage.PendingFormation;
        var pendingEnemy = PendingEnemyGroup != null ? PendingEnemyGroup : enemyGroup;

        // Kiểm tra: nếu không có pending data, cho TestStarter xử lý (standalone mode)
        if (pendingFormation == null)
        {
            var testStarter = GetComponent<CombatTestStarter>();
            if (testStarter != null && testStarter.enabled)
            {
                Debug.Log("[CombatSceneStarter] Không có pending data. Chuyển xử lý cho CombatTestStarter.");
                return;
            }
            // Nếu không có TestStarter → báo lỗi và thoát
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

            // Xóa pending ngay sau khi dùng
            FormationDataStorage.PendingFormation = null;
            PendingEnemyGroup = null;

            // Đăng ký sự kiện kết thúc combat để quay về map
            combat.OnVictory += () => StartCoroutine(HandleCombatEnd(true));
            combat.OnDefeat += () => StartCoroutine(HandleCombatEnd(false));
        }
        else
        {
            Debug.LogError("Không tìm thấy CombatManager trong CombatScene!");
        }
    }

    private System.Collections.IEnumerator HandleCombatEnd(bool isVictory)
    {
        // Dừng BGM combat khi kết thúc
        if (CombatAudioManager.Instance != null)
            CombatAudioManager.Instance.StopBGM();

        // Hiển thị panel Victory/Defeat với animation pop-up
        var resultUI = FindFirstObjectByType<CombatResultUI>();
        if (resultUI != null)
        {
            yield return resultUI.ShowResult(isVictory);
        }
        else
        {
            Debug.LogWarning("[CombatSceneStarter] Không tìm thấy CombatResultUI trong scene.");
        }

        // Fade to black trước khi rời combat scene
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

        // Nếu thắng, báo cho enemy đã kích hoạt và cho QuestManager
        if (isVictory && LastTouchedEnemy != null)
        {
            // Báo QuestManager
            if (QuestManager.Instance != null && LastTouchedEnemy.enemyGroup != null)
                QuestManager.Instance.OnEnemyGroupDefeated(LastTouchedEnemy.enemyGroup);
            
            LastTouchedEnemy.MarkAsDefeated();
        }

        // Unload combat scene (chỉ khi đang load additively qua Map)
        if (SceneLoaderManager.Instance != null)
        {
            SceneLoaderManager.UnloadCombatScene();
        }
        else
        {
            Debug.Log("[CombatSceneStarter] Standalone mode — không unload scene.");
        }
    }

    // Lưu enemy vừa chạm (gán từ MapEnemy trước khi load)
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