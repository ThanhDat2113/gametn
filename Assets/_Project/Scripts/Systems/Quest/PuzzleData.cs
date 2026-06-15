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
}

// ─── Batch 1 ───
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
    [TextArea(2, 5)] public string[] riddles;
    public string[] correctAnswers;
    public string[] wrongAnswerA;
    public string[] wrongAnswerB;
    public string[] wrongAnswerC;
    public int requiredCorrect = 1;
}

[System.Serializable]
public class MemoryGroveConfig
{
    public CharacterData[] characterPool;
    public int gridCols = 4;
    public int gridRows = 3;
    public bool showLoreOnMatch = true;
}

// ─── Batch 2 ───
[System.Serializable]
public class SlidePuzzleConfig
{
    public int gridSize = 3;
    public int maxMoves = 100;
}

[System.Serializable]
public class SpirePuzzleConfig
{
    public int diskCount = 4;   // 3-6
    public int maxMoves = 50;
}

[System.Serializable]
public class FlowPuzzleConfig
{
    public int gridSize = 5;
    public Color[] pairColors;
}