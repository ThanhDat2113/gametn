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

    // 🆕 Minigame mới
    [Header("Jigsaw Puzzle Config")]
    public JigsawConfig jigsawConfig;

    [Header("Pull Block Puzzle Config")]
    public PullBlockConfig pullBlockConfig;
}

// ─────────────────────────────
// 🆕 Jigsaw Config
// ─────────────────────────────
[System.Serializable]
public class JigsawConfig
{
    [Header("Grid Settings")]
    public int gridCols = 3;
    public int gridRows = 2;

    [Header("Board Size")]
    public float boardWidth = 400f;
    public float boardHeight = 300f;
    public float pieceSpacing = 5f;

    [Header("Piece Sprites")]
    [Tooltip("Kéo 6 sprite ảnh mảnh ghép vào đây (có thể để trống để dùng màu mặc định)")]
    public Sprite[] pieceSprites;
}

// ─────────────────────────────
// 🆕 Pull Block Config
// ─────────────────────────────
[System.Serializable]
public class PullBlockConfig
{
    [Header("Block Settings")]
    public int totalBlocks = 10;
    public float blockWidth = 180f;
    public float blockHeight = 40f;
    public float containerWidth = 400f;
    public float containerHeight = 400f;

    [Header("Stack Settings")]
    public float stackOffsetX = 15f;
    public float stackOffsetY = 12f;
    public float rotationSpread = 15f;

    [Header("Colors")]
    public Color[] blockColors;

    [Header("Puzzle Data Reference")]
    public PuzzleData puzzleData;
}

// ─────────────────────────────
// Các config cũ (giữ nguyên)
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
    public CharacterData[] characterPool;
    public int gridCols = 4;
    public int gridRows = 3;
    public bool showLoreOnMatch = false;
}

[System.Serializable]
public class SlidePuzzleConfig
{
    public int gridSize = 3;
    public int maxMoves = 100;
}

[System.Serializable]
public class SpirePuzzleConfig
{
    public int diskCount = 4;
    public int maxMoves = 50;
}

[System.Serializable]
public class FlowPuzzleConfig
{
    [Range(3, 6)]
    public int pairCount = 4;
    public Color[] wireColors;
    public Sprite plugSprite;
    public Sprite socketSprite;
    public int gridSize = 5;
    public Color[] pairColors;
}

[System.Serializable]
public class UnblockConfig
{
    public int gridWidth = 4;
    public int gridHeight = 5;
    [TextArea(5, 10)]
    public string[] boardLayout;
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f);
    public Color wallColor = new Color(0.5f, 0.5f, 0.5f);
    public Color blockColor = new Color(0.6f, 0.4f, 0.2f);
    public Color redBlockColor = Color.red;
    public Color goalColor = Color.green;
}

[System.Serializable]
public class WoodQuizConfig
{
    public int gridWidth = 4;
    public int gridHeight = 5;
    [TextArea(5, 10)]
    public string[] boardLayout;
    public int maxMoves = 50;
    public Color emptyColor = new Color(0.12f, 0.08f, 0.05f, 1f);
    public Color wallColor = new Color(0.25f, 0.18f, 0.12f, 1f);
    public Color woodLightColor = new Color(0.72f, 0.52f, 0.30f, 1f);
    public Color woodDarkColor = new Color(0.55f, 0.38f, 0.20f, 1f);
    public Color masterColor = new Color(0.85f, 0.25f, 0.20f, 1f);
    public Color goalColor = new Color(0.20f, 0.75f, 0.35f, 1f);
}