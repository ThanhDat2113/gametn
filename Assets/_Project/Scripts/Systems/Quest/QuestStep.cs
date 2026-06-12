using UnityEngine;

public enum QuestStepType
{
    Talk,       // Nói chuyện với NPC (targetId = triggerID)
    Kill,       // Đánh bại EnemyGroup (targetId = tên asset EnemyGroupData)
    Gather,     // Thu thập vật phẩm (requiredItem + requiredAmount)
    Explore     // Đến một địa điểm (mở rộng)
}

[System.Serializable]
public class QuestStep
{
    public string stepId;              // Ví dụ: "talk_to_old_man"
    public QuestStepType type;
    public string targetId;            // triggerID của DialogueTrigger hoặc tên EnemyGroupData

    [Header("Gather (Chỉ dùng khi type = Gather)")]
    public ItemData requiredItem;      // Vật phẩm cần thu thập
    public int requiredAmount = 1;     // Số lượng cần

    public string description;         // "Nói chuyện với ông lão"
    public bool isCompleted = false;
}
