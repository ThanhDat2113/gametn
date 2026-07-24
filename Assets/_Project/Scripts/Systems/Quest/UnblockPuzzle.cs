using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle "Unblock" - Đẩy khối gỗ màu đỏ ra lối thoát.
/// Board layout dùng ký tự:
///   R = Red block (master, cần đẩy ra goal)
///   H = Horizontal block (di chuyển ngang)
///   V = Vertical block (di chuyển dọc)
///   # = Wall
///   . = Empty
///   G = Goal (vị trí ra thoát, thường ở dưới cùng)
/// </summary>
public class UnblockPuzzle : PuzzleBase
{
    [Header("UI References")]
    public GridLayoutGroup gridLayout;
    public Text instructionText;
    public Text moveCountText;
    public Button resetButton;
    public Button closeButton;

    [Header("Visual Settings")]
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f);
    public Color wallColor = new Color(0.5f, 0.5f, 0.5f);
    public Color blockColor = new Color(0.6f, 0.4f, 0.2f);
    public Color redBlockColor = Color.red;
    public Color goalColor = Color.green;

    private UnblockConfig _config;
    private int _gridWidth;
    private int _gridHeight;
    private char[,] _board;
    private Image[,] _cellImages;
    private int _moveCount = 0;
    private bool _puzzleCompleted = false;
    private Vector2Int _redBlockPos;
    private bool _redBlockHorizontal;
    private List<Vector2Int> _goalCells = new List<Vector2Int>();

    private const char RED_BLOCK = 'R';
    private const char HORIZONTAL_BLOCK = 'H';
    private const char VERTICAL_BLOCK = 'V';
    private const char WALL = '#';
    private const char EMPTY = '.';
    private const char GOAL = 'G';

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);

        if (data?.unblockConfig != null)
        {
            _config = data.unblockConfig;
            _gridWidth = _config.gridWidth;
            _gridHeight = _config.gridHeight;
            emptyColor = _config.emptyColor;
            wallColor = _config.wallColor;
            blockColor = _config.blockColor;
            redBlockColor = _config.redBlockColor;
            goalColor = _config.goalColor;
        }
        else
        {
            _gridWidth = 4;
            _gridHeight = 5;
        }

        _moveCount = 0;
        _puzzleCompleted = false;

        ParseBoard();
        CreateGrid();
        RenderBoard();
        UpdateUI();
    }

    private void ParseBoard()
    {
        _board = new char[_gridHeight, _gridWidth];

        if (_config != null && _config.boardLayout != null && _config.boardLayout.Length > 0)
        {
            for (int r = 0; r < _gridHeight; r++)
            {
                string row = r < _config.boardLayout.Length ? _config.boardLayout[r] : "";
                for (int c = 0; c < _gridWidth; c++)
                {
                    char ch = c < row.Length ? row[c] : EMPTY;
                    _board[r, c] = ch;

                    if (ch == RED_BLOCK)
                    {
                        _redBlockPos = new Vector2Int(c, r);
                        _redBlockHorizontal = true; // Mặc định R là 1x2 ngang
                    }
                    else if (ch == GOAL)
                    {
                        _goalCells.Add(new Vector2Int(c, r));
                    }
                }
            }
        }
        else
        {
            // Default trivial board for testing
            _board[2, 0] = RED_BLOCK;
            _redBlockPos = new Vector2Int(0, 2);
            _redBlockHorizontal = true;
            _board[2, 2] = GOAL;
        }
    }

    private void CreateGrid()
    {
        if (gridLayout == null) return;

        // Clear old children
        foreach (Transform child in gridLayout.transform)
        {
            if (child != null) Destroy(child.gameObject);
        }

        _cellImages = new Image[_gridHeight, _gridWidth];

        for (int r = 0; r < _gridHeight; r++)
        {
            for (int c = 0; c < _gridWidth; c++)
            {
                GameObject cell = new GameObject($"Cell_{r}_{c}");
                cell.transform.SetParent(gridLayout.transform, false);
                Image img = cell.AddComponent<Image>();
                img.color = emptyColor;
                _cellImages[r, c] = img;

                // Add button for interaction
                Button btn = cell.AddComponent<Button>();
                int row = r, col = c;
                btn.onClick.AddListener(() => OnCellClicked(row, col));
            }
        }

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetPuzzle);
        if (closeButton != null)
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
    }

    private void RenderBoard()
    {
        for (int r = 0; r < _gridHeight; r++)
        {
            for (int c = 0; c < _gridWidth; c++)
            {
                if (_cellImages[r, c] == null) continue;

                char ch = _board[r, c];
                switch (ch)
                {
                    case WALL:
                        _cellImages[r, c].color = wallColor;
                        break;
                    case RED_BLOCK:
                        _cellImages[r, c].color = redBlockColor;
                        break;
                    case HORIZONTAL_BLOCK:
                    case VERTICAL_BLOCK:
                        _cellImages[r, c].color = blockColor;
                        break;
                    case GOAL:
                        _cellImages[r, c].color = goalColor;
                        break;
                    default:
                        _cellImages[r, c].color = emptyColor;
                        break;
                }
            }
        }
    }

    private void OnCellClicked(int row, int col)
    {
        if (_puzzleCompleted) return;

        // Try to move any block that occupies (row, col)
        bool moved = TryMoveBlock(row, col);
        if (!moved)
        {
            // If clicking on goal near red block, also try move red
            if (_board[row, col] == GOAL)
            {
                moved = TryMoveRedBlockTowardsGoal(row, col);
            }
        }

        if (moved)
        {
            _moveCount++;
            RenderBoard();
            UpdateUI();

            if (CheckWin())
            {
                _puzzleCompleted = true;
                if (instructionText != null)
                    instructionText.text = "🎉 Hoàn thành!";
                StartCoroutine(DelayedSuccess());
            }
        }
    }

    private bool TryMoveBlock(int row, int col)
    {
        char ch = _board[row, col];
        if (ch == RED_BLOCK)
        {
            return TryMoveRedBlock(row, col);
        }
        else if (ch == HORIZONTAL_BLOCK)
        {
            return TryMoveHorizontalBlock(row, col);
        }
        else if (ch == VERTICAL_BLOCK)
        {
            return TryMoveVerticalBlock(row, col);
        }
        return false;
    }

    private bool TryMoveRedBlock(int row, int col)
    {
        // Red block is 1x2 horizontal by design in this simple version
        // Determine orientation from board: if adjacent right is also R, vertical? Actually R stored at top-left
        // Simplified: assume red block always horizontal spanning (col, col+1)
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (int dir = 0; dir < 4; dir++)
        {
            int nr = row + dr[dir];
            int nc = col + dc[dir];
            int nr2 = row + dr[dir];
            int nc2 = col + 1 + dc[dir]; // second cell for horizontal

            if (CanPlaceRedBlock(nr, nc, nr2, nc2))
            {
                // Clear old
                _board[row, col] = EMPTY;
                _board[row, col + 1] = EMPTY;
                // Place new
                _board[nr, nc] = RED_BLOCK;
                _board[nr2, nc2] = RED_BLOCK;
                _redBlockPos = new Vector2Int(nc, nr);
                return true;
            }
        }
        return false;
    }

    private bool TryMoveRedBlockTowardsGoal(int goalRow, int goalCol)
    {
        // Try to move red block one step towards goal if adjacent
        int targetR = _redBlockPos.y;
        int targetC = _redBlockPos.x;

        if (goalRow == targetR)
        {
            int dir = goalCol > targetC ? 1 : -1;
            int nc1 = targetC + dir;
            int nc2 = targetC + 1 + dir;
            if (CanPlaceRedBlock(targetR, nc1, targetR, nc2))
            {
                _board[targetR, targetC] = EMPTY;
                _board[targetR, targetC + 1] = EMPTY;
                _board[targetR, nc1] = RED_BLOCK;
                _board[targetR, nc2] = RED_BLOCK;
                _redBlockPos = new Vector2Int(nc1, targetR);
                return true;
            }
        }
        else if (goalCol == targetC || goalCol == targetC + 1)
        {
            int dir = goalRow > targetR ? 1 : -1;
            int nr = targetR + dir;
            if (CanPlaceRedBlock(nr, targetC, nr, targetC + 1))
            {
                _board[targetR, targetC] = EMPTY;
                _board[targetR, targetC + 1] = EMPTY;
                _board[nr, targetC] = RED_BLOCK;
                _board[nr, targetC + 1] = RED_BLOCK;
                _redBlockPos = new Vector2Int(targetC, nr);
                return true;
            }
        }
        return false;
    }

    private bool CanPlaceRedBlock(int r1, int c1, int r2, int c2)
    {
        if (!IsInBounds(r1, c1) || !IsInBounds(r2, c2)) return false;
        char a = _board[r1, c1];
        char b = _board[r2, c2];
        return (a == EMPTY || a == GOAL) && (b == EMPTY || b == GOAL);
    }

    private bool IsInBounds(int r, int c)
    {
        return r >= 0 && r < _gridHeight && c >= 0 && c < _gridWidth;
    }

    private bool TryMoveHorizontalBlock(int row, int col)
    {
        // Horizontal block occupies single cell in this simple version
        int[] dc = { -1, 1 };
        foreach (int dir in dc)
        {
            int nc = col + dir;
            if (IsInBounds(row, nc) && (_board[row, nc] == EMPTY || _board[row, nc] == GOAL))
            {
                _board[row, nc] = HORIZONTAL_BLOCK;
                _board[row, col] = EMPTY;
                return true;
            }
        }
        return false;
    }

    private bool TryMoveVerticalBlock(int row, int col)
    {
        // Vertical block occupies single cell in this simple version
        int[] dr = { -1, 1 };
        foreach (int dir in dr)
        {
            int nr = row + dir;
            if (IsInBounds(nr, col) && (_board[nr, col] == EMPTY || _board[nr, col] == GOAL))
            {
                _board[nr, col] = VERTICAL_BLOCK;
                _board[row, col] = EMPTY;
                return true;
            }
        }
        return false;
    }

    private bool CheckWin()
    {
        // Win condition: red block (2 ô ngang) đè lên vị trí Goal
        int r1 = _redBlockPos.y;
        int c1 = _redBlockPos.x;
        int r2 = r1;
        int c2 = c1 + 1;

        bool coversGoal = _goalCells.Contains(new Vector2Int(c1, r1)) || _goalCells.Contains(new Vector2Int(c2, r2));
        return coversGoal;
    }

    private IEnumerator DelayedSuccess()
    {
        yield return new WaitForSeconds(0.5f);
        CompletePuzzle(true);
    }

    private void UpdateUI()
    {
        if (instructionText != null)
            instructionText.text = _puzzleCompleted ? "🎉 Hoàn thành!" : "Đẩy khối đỏ ra lối thoát (G)";
        if (moveCountText != null)
            moveCountText.text = $"Số bước: {_moveCount}";
    }

    private void ResetPuzzle()
    {
        ParseBoard();
        CreateGrid();
        _moveCount = 0;
        _puzzleCompleted = false;
        RenderBoard();
        UpdateUI();
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}