using UnityEngine;

[CreateAssetMenu(fileName = "NewPuzzle", menuName = "Quest/Puzzle Data")]
public class PuzzleData : ScriptableObject
{
    [Header("Identity")]
    public string puzzleID;
    public string puzzleName;

    [Header("Type")]
    public QuestStepType puzzleType;

    [Header("Config")]
    public int allowedAttempts = 3;
    public bool showOnComplete = true;

    // Batch 1
    [Header("Symbol Sequence Config")]
    public SymbolSequenceConfig symbolConfig;

    [Header("Riddle Gate Config")]
    public RiddleGateConfig riddleConfig;

    [Header("Memory Grove Config")]
    public MemoryGroveConfig memoryConfig;

    // Batch 2
    [Header("Slide Puzzle Config")]
    public SlidePuzzleConfig slideConfig;

    [Header("Spire Puzzle Config")]
    public SpirePuzzleConfig spireConfig;

    [Header("Flow Puzzle Config")]
    public FlowPuzzleConfig flowConfig;

    [Header("Unblock Puzzle Config")]
    public UnblockConfig unblockConfig;
}

// ─────────────────────────────
// Batch 1
// ─────────────────────────────

[System.Serializable]
public class SymbolSequenceConfig
{
    public int gridSize = 3;
    public int startLength = 3;
    public int maxLength = 7;
}

[System.Serializable]
public class RiddleGateConfig
{
    [TextArea(2, 5)]
    public string[] riddles;

    public string[] correctAnswers;
    public string[] wrongAnswerA;
    public string[] wrongAnswerB;
    public string[] wrongAnswerC;

    public int requiredCorrect = 1;
}

[System.Serializable]
public class MemoryGroveConfig
{
    [Header("Portrait Pool")]
    public Sprite[] portraitPool;

    // Giữ lại để tương thích với các Editor Tool cũ
    public CharacterData[] characterPool;

    [Header("Board Size")]
    public int gridCols = 4;
    public int gridRows = 3;

    [Header("Options")]
    public bool showLoreOnMatch = false;
}

// ─────────────────────────────
// Batch 2
// ─────────────────────────────

[System.Serializable]
public class SlidePuzzleConfig
{
    public int gridSize = 3;
    public int maxMoves = 100;
}

[System.Serializable]
public class SpirePuzzleConfig
{
    public int diskCount = 4; // 3-6
    public int maxMoves = 50;
}

[System.Serializable]
public class FlowPuzzleConfig
{
    [Header("Wire Drag & Drop")]
    [Range(3, 6)]
    public int pairCount = 4;
    public Color[] wireColors;

    [Header("Sprites (Optional)")]
    public Sprite plugSprite;
    public Sprite socketSprite;

    // Keep legacy fields
    public int gridSize = 5;
    public Color[] pairColors;
}

[System.Serializable]
public class UnblockConfig
{
    [Header("Grid Size")]
    public int gridWidth = 4;
    public int gridHeight = 5;

    [Header("Board Layout (rows of chars)")]
    [TextArea(5, 10)]
    public string[] boardLayout;

    [Header("Visual")]
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f);
    public Color wallColor = new Color(0.5f, 0.5f, 0.5f);
    public Color blockColor = new Color(0.6f, 0.4f, 0.2f);
    public Color redBlockColor = Color.red;
    public Color goalColor = Color.green;
}
