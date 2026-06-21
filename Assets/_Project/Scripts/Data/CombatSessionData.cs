/// <summary>
/// Nguồn dữ liệu duy nhất cho toàn bộ session combat (bao gồm cả retry).
/// Thay thế hoàn toàn CombatRetryData và CombatTransitionData.
///
/// Vòng đời:
///   Set()   → khi MapEnemy hoặc NPCInteraction bắt đầu chuyển cảnh vào combat
///   (tồn tại qua tất cả lần retry — KHÔNG xóa khi thua)
///   Clear() → chỉ khi thắng hoặc bấm Quit về map
/// </summary>
public static class CombatSessionData
{
    public static FormationData Formation { get; private set; }
    public static EnemyGroupData EnemyGroup { get; private set; }
    public static bool HasData => Formation != null && EnemyGroup != null;
    public static bool IsFromMap { get; private set; } = false;

    /// <summary>
    /// ID của quest step đang chờ combat (dùng cho NPCInteraction).
    /// Nếu có giá trị, khi thắng combat sẽ gọi QuestManager.OnEnemyGroupDefeated(questTargetId)
    /// thay vì dùng MapEnemy.
    /// </summary>
    public static string QuestTargetId { get; private set; } = null;

    public static void Set(FormationData formation, EnemyGroupData enemyGroup, bool fromMap = true, string questTargetId = null)
    {
        Formation = formation;
        EnemyGroup = enemyGroup;
        IsFromMap = fromMap;
        QuestTargetId = questTargetId;
        UnityEngine.Debug.Log($"[CombatSessionData] Set: formation={formation?.slots?.Length} slots, enemy={enemyGroup?.name}, fromMap={fromMap}, questTargetId={questTargetId}");
    }

    public static void Clear()
    {
        UnityEngine.Debug.Log("[CombatSessionData] Cleared.");
        Formation = null;
        EnemyGroup = null;
        IsFromMap = false;
        QuestTargetId = null;
    }
}