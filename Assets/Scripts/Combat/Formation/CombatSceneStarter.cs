using UnityEngine;

public class CombatSceneStarter : MonoBehaviour
{
    public EnemyGroupData enemyGroup;

    void Start()
    {
        var pending = FormationDataStorage.PendingFormation;
        Debug.Log($"[CombatSceneStarter] PendingFormation: {(pending == null ? "NULL" : $"slots={pending.slots.Length}")}");

        if (pending == null)
        {
            Debug.LogError("Không có đội hình. Hãy bắt đầu từ Map Scene.");
            return;
        }

        var combat = CombatManager.Instance;
        if (combat != null)
        {
            combat.StartCombat(pending, enemyGroup);
            // Xóa pending ngay sau khi dùng để tránh tái sử dụng
            FormationDataStorage.PendingFormation = null;
        }
        else
        {
            Debug.LogError("Không tìm thấy CombatManager");
        }
    }
}