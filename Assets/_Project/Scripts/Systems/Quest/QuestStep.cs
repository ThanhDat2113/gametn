using UnityEngine;

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
    FlowPuzzle,
    Unblock,
    WoodQuiz,
    
    // 🆕 Minigame mới
    JigsawPuzzle,   // Ghép hình 6 ô
    PullBlockPuzzle // Rút gỗ
}

[System.Serializable]
public class QuestStep
{
    public string stepId;
    public QuestStepType type;
    public string targetId;          // ID của NPC, enemy, item, location, puzzle
    public string description;       // Mô tả hiển thị
    public bool isCompleted = false;

    [Header("Kill Count Settings")]
    public int requiredAmount = 1;
    public int currentAmount = 0;
}