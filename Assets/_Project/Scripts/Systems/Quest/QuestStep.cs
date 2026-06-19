public enum QuestStepType
{
    Talk,           // Nói chuyện với NPC (triggerID)
    Kill,           // Đánh bại EnemyGroup (tên asset)
    Gather,         // Thu thập vật phẩm (mở rộng)
    Explore,        // Đến một địa điểm

    // 🧩 Puzzle types (batch 1)
    SymbolSequence, // Nhấn lại sequence ký hiệu phát sáng
    RiddleGate,     // Trả lời câu đố
    MemoryGrove,    // Lật ô tìm cặp portrait nhân vật

    // 🧩 Puzzle types (batch 2)
    SlidePuzzle,    // Xếp hình trượt 3x3
    SpirePuzzle,    // Tháp Huyền Thoại (Hanoi Tower)
    FlowPuzzle      // Nối các cặp màu không chồng chéo
}

[System.Serializable]
public class QuestStep
{
    public string stepId;
    public QuestStepType type;
    public string targetId;
    public string description;
    public bool isCompleted = false;
}