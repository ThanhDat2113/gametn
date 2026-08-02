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

    [Header("Wood Quiz (Klotski) Config")]
    public WoodQuizConfig woodQuizConfig;
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

// ─────────────────────────────
// Wood Quiz (Klotski)
// ─────────────────────────────

[System.Serializable]
public class WoodQuizConfig
{
    [Header("Grid Size")]
    public int gridWidth = 4;
    public int gridHeight = 5;

    [Header("Board Layout (rows of chars)")]
    [Tooltip("Ký tự layout:\n" +
             "  '#' = tường\n" +
             "  '.' = ô trống\n" +
             "  'G' = lối thoát (goal)\n" +
             "  'M' = khối master ĐỎ (block dọc 1x2, cần đẩy ra)\n" +
             "  'A'-'Z' = các khối gỗ khác (cùng ký tự = cùng block, có thể 1x1, 1x2, 2x1)")]
    [TextArea(5, 10)]
    public string[] boardLayout;

    [Header("Rules")]
    public int maxMoves = 50;

    [Header("Visual")]
    public Color emptyColor = new Color(0.12f, 0.08f, 0.05f, 1f);
    public Color wallColor = new Color(0.25f, 0.18f, 0.12f, 1f);
    public Color woodLightColor = new Color(0.72f, 0.52f, 0.30f, 1f);
    public Color woodDarkColor = new Color(0.55f, 0.38f, 0.20f, 1f);
    public Color masterColor = new Color(0.85f, 0.25f, 0.20f, 1f);
    public Color goalColor = new Color(0.20f, 0.75f, 0.35f, 1f);
}
