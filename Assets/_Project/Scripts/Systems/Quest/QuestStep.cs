using UnityEngine; // Thêm dòng này để sử dụng HeaderAttribute

public enum QuestStepType
{
    Talk,           // Nói chuyện với NPC (triggerID)
    Kill,           // Đánh bại EnemyGroup hoặc số lượng quái
    Gather,         // Thu thập vật phẩm
    Explore,        // Đến một địa điểm
    
    // Puzzle types
    SymbolSequence,
    RiddleGate,
    MemoryGrove,
    SlidePuzzle,
    SpirePuzzle,
    FlowPuzzle
}

[System.Serializable]
public class QuestStep
{
    public string stepId;
    public QuestStepType type;
    public string targetId;          // ID của NPC, enemy, item, location, puzzle
    public string description;       // Mô tả hiển thị, có thể dùng placeholder {current}/{required}
    public bool isCompleted = false;

    [Header("Kill Count Settings")] // Cần using UnityEngine để sử dụng Header
    public int requiredAmount = 1;   // Số lượng cần tiêu diệt (chỉ dùng cho Kill)
    public int currentAmount = 0;    // Số lượng đã tiêu diệt (runtime)
}