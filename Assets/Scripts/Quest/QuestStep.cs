public enum QuestStepType
{
    Talk,       // Nói chuyện với NPC (triggerID)
    Kill,       // Đánh bại EnemyGroup (tên asset)
    Gather,     // Thu thập vật phẩm (mở rộng)
    Explore     // Đến một địa điểm
}

[System.Serializable]
public class QuestStep
{
    public string stepId;              // Ví dụ: "talk_to_old_man"
    public QuestStepType type;
    public string targetId;            // triggerID của DialogueTrigger hoặc tên EnemyGroupData
    public string description;         // "Nói chuyện với ông lão"
    public bool isCompleted = false;
}