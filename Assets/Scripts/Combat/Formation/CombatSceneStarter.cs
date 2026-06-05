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

        Debug.Log($"[CombatSceneStarter] Formation: {(pendingFormation == null ? "NULL" : "OK")} Enemy: {(pendingEnemy == null ? "NULL" : pendingEnemy.name)}");

        if (pendingFormation == null || pendingEnemy == null)
        {
            Debug.LogError("Thiếu dữ liệu đội hình hoặc enemy group. Không thể bắt đầu combat.");
            // Quay về map nếu lỗi
            ReturnToMapAfterError();
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

        // Unload combat scene (sẽ tự động kích hoạt lại MapRoot và fade từ đen bên trong SceneLoaderManager)
        SceneLoaderManager.UnloadCombatScene();
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