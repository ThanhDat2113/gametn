using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Puzzle "Wood Quiz" (Klotski) — Kéo các khối gỗ trong hộp để tạo đường
/// cho khối master (màu đỏ) chui ra qua lối thoát.
///
/// Layout ký tự (từ WoodQuizConfig.boardLayout):
///   '#' = tường
///   '.' = ô trống
///   'G' = lối thoát (goal)
///   'M' = khối master màu đỏ (cần đẩy ra)
///   'A'-'Z' = các khối gỗ chặn (cùng ký tự = cùng block)
///
/// Tương tác: kéo block → block trượt liên tục đến khi chạm tường/block khác.
/// </summary>
public class WoodQuizPuzzle : PuzzleBase
{
    [Header("UI References")]
    public GridLayoutGroup gridLayout;
    public RectTransform blockContainer;
    public Text instructionText;
    public Text moveCountText;
    public Button resetButton;
    public Button closeButton;

    [Header("Block Prefab (optional)")]
    public GameObject blockPrefab;

    [Header("Visual Settings")]
    public Color emptyColor = new Color(0.12f, 0.08f, 0.05f, 1f);
    public Color wallColor = new Color(0.25f, 0.18f, 0.12f, 1f);
    public Color woodLightColor = new Color(0.72f, 0.52f, 0.30f, 1f);
    public Color woodDarkColor = new Color(0.55f, 0.38f, 0.20f, 1f);
    public Color masterColor = new Color(0.85f, 0.25f, 0.20f, 1f);
    public Color goalColor = new Color(0.20f, 0.75f, 0.35f, 1f);

    private WoodQuizConfig _config;
    private int _gridWidth = 4;
    private int _gridHeight = 5;
    private int _maxMoves = 50;

    private int[,] _cellOwner;
    private List<WoodBlock> _blocks = new List<WoodBlock>();
    private int _masterId = -1;
    private Vector2Int _goalPos;

    private Image[,] _cellImages;
    private List<GameObject> _blockObjects = new List<GameObject>();
    private float _cellSize = 70f;
    private Vector2 _gridOrigin;

    private int _moveCount = 0;
    private bool _puzzleCompleted = false;

    private const int EMPTY = -1;
    private const int WALL = -2;
    private const int GOAL = -3;

    private class WoodBlock
    {
        public int id;
        public char label;
        public bool isMaster;
        public List<Vector2Int> cells = new List<Vector2Int>();
    }

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);

        if (data?.woodQuizConfig != null)
        {
            _config = data.woodQuizConfig;
            _gridWidth = _config.gridWidth;
            _gridHeight = _config.gridHeight;
            _maxMoves = _config.maxMoves;
            emptyColor = _config.emptyColor;
            wallColor = _config.wallColor;
            woodLightColor = _config.woodLightColor;
            woodDarkColor = _config.woodDarkColor;
            masterColor = _config.masterColor;
            goalColor = _config.goalColor;
        }

        _moveCount = 0;
        _puzzleCompleted = false;

        ParseBoard();
        BuildGrid();
        BuildBlocks();
        UpdateUI();

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetPuzzle);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
        }
    }

    private void ParseBoard()
    {
        _cellOwner = new int[_gridHeight, _gridWidth];
        _blocks.Clear();
        _masterId = -1;

        for (int r = 0; r < _gridHeight; r++)
            for (int c = 0; c < _gridWidth; c++)
                _cellOwner[r, c] = EMPTY;

        Dictionary<char, int> charToId = new Dictionary<char, int>();
        Dictionary<char, List<Vector2Int>> charToCells = new Dictionary<char, List<Vector2Int>>();

        if (_config != null && _config.boardLayout != null && _config.boardLayout.Length > 0)
        {
            for (int r = 0; r < _gridHeight; r++)
            {
                string row = r < _config.boardLayout.Length ? _config.boardLayout[r] : "";
                for (int c = 0; c < _gridWidth; c++)
                {
                    char ch = c < row.Length ? row[c] : '.';
                    switch (ch)
                    {
                        case '#':
                            _cellOwner[r, c] = WALL;
                            break;
                        case 'G':
                            _cellOwner[r, c] = GOAL;
                            _goalPos = new Vector2Int(c, r);
                            break;
                        case '.':
                            _cellOwner[r, c] = EMPTY;
                            break;
                        default:
                            if (!charToId.ContainsKey(ch))
                            {
                                int newId = _blocks.Count;
                                charToId[ch] = newId;
                                charToCells[ch] = new List<Vector2Int>();
                                _blocks.Add(new WoodBlock
                                {
                                    id = newId,
                                    label = ch,
                                    isMaster = (ch == 'M')
                                });
                            }
                            charToCells[ch].Add(new Vector2Int(c, r));
                            _cellOwner[r, c] = charToId[ch];
                            if (ch == 'M') _masterId = charToId[ch];
                            break;
                    }
                }
            }

            foreach (var kvp in charToCells)
            {
                var block = _blocks[charToId[kvp.Key]];
                block.cells = kvp.Value;
            }
        }
        else
        {
            ApplyDefaultLayout();
        }
    }

    /// <summary>
    /// Layout mẫu (4x5) — block đỏ (M) là block DỌC 1x2:
    ///
    /// ####
    /// #.M#
    /// #.M#
    /// #AB#
    /// #.G#
    ///
    /// M = master đỏ DỌC 1x2 (chiếm 2 ô theo chiều dọc, cần đẩy ra)
    /// A = block 1x1 (màu gỗ sáng)
    /// B = block 1x1 (màu gỗ tối)
    /// G = goal (lối thoát)
    /// Cách giải: A xuống → B trái → M xuống
    /// </summary>
    private void ApplyDefaultLayout()
    {
        Log("[WoodQuiz] Dùng layout mẫu có sẵn.");
        string[] layout = new string[]
        {
"####",
            "#.M#",
            "#.M#",
            "#AB#",
            "#.G#"
        };

        _gridWidth = 4;
        _gridHeight = 5;
        _cellOwner = new int[_gridHeight, _gridWidth];
        _blocks.Clear();
        _masterId = -1;
        _goalPos = new Vector2Int(2, 4);

        Dictionary<char, int> charToId = new Dictionary<char, int>();
        Dictionary<char, List<Vector2Int>> charToCells = new Dictionary<char, List<Vector2Int>>();

        for (int r = 0; r < _gridHeight; r++)
        {
            string row = r < layout.Length ? layout[r] : "";
            for (int c = 0; c < _gridWidth; c++)
            {
                char ch = c < row.Length ? row[c] : '.';
                switch (ch)
                {
                    case '#':
                        _cellOwner[r, c] = WALL;
                        break;
                    case 'G':
                        _cellOwner[r, c] = GOAL;
                        break;
                    case '.':
                        _cellOwner[r, c] = EMPTY;
                        break;
                    default:
                        if (!charToId.ContainsKey(ch))
                        {
                            int newId = _blocks.Count;
                            charToId[ch] = newId;
                            charToCells[ch] = new List<Vector2Int>();
                            _blocks.Add(new WoodBlock
                            {
                                id = newId,
                                label = ch,
                                isMaster = (ch == 'M')
                            });
                        }
                        charToCells[ch].Add(new Vector2Int(c, r));
                        _cellOwner[r, c] = charToId[ch];
                        if (ch == 'M') _masterId = charToId[ch];
                        break;
                }
            }
        }

        foreach (var kvp in charToCells)
        {
            var block = _blocks[charToId[kvp.Key]];
            block.cells = kvp.Value;
        }
    }

    private void BuildGrid()
    {
        if (gridLayout == null)
        {
            LogError("[WoodQuiz] gridLayout chưa gán!");
            return;
        }

        foreach (Transform child in gridLayout.transform)
        {
            if (child != null) Destroy(child.gameObject);
        }

        _cellSize = Mathf.Min(280f / _gridWidth, 340f / _gridHeight);
        gridLayout.cellSize = new Vector2(_cellSize, _cellSize);
        gridLayout.spacing = Vector2.zero;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = _gridWidth;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.padding = new RectOffset(0, 0, 0, 0);

        _cellImages = new Image[_gridHeight, _gridWidth];

        for (int r = 0; r < _gridHeight; r++)
        {
            for (int c = 0; c < _gridWidth; c++)
            {
                GameObject cell = new GameObject($"Cell_{r}_{c}");
                cell.transform.SetParent(gridLayout.transform, false);
                Image img = cell.AddComponent<Image>();
                img.raycastTarget = false;
                _cellImages[r, c] = img;
            }
        }

        float gridW = _gridWidth * _cellSize;
        float gridH = _gridHeight * _cellSize;
        _gridOrigin = new Vector2(-gridW / 2f + _cellSize / 2f, gridH / 2f - _cellSize / 2f);
    }

    private void BuildBlocks()
    {
        foreach (var go in _blockObjects)
            if (go != null) Destroy(go);
        _blockObjects.Clear();

        Transform container = blockContainer != null ? blockContainer : gridLayout.transform.parent;

        foreach (var block in _blocks)
        {
            GameObject blockGo;
            Image blockImg;
            WoodQuizBlockDrag drag;
            CanvasGroup cg;

            if (blockPrefab != null)
            {
                blockGo = Instantiate(blockPrefab, container, false);
                blockImg = blockGo.GetComponent<Image>();
                if (blockImg == null) blockImg = blockGo.AddComponent<Image>();
                drag = blockGo.GetComponent<WoodQuizBlockDrag>();
                if (drag == null) drag = blockGo.AddComponent<WoodQuizBlockDrag>();
                cg = blockGo.GetComponent<CanvasGroup>();
                if (cg == null) cg = blockGo.AddComponent<CanvasGroup>();
            }
            else
            {
                blockGo = new GameObject($"Block_{block.label}");
                blockGo.transform.SetParent(container, false);
                blockImg = blockGo.AddComponent<Image>();
                cg = blockGo.AddComponent<CanvasGroup>();
                drag = blockGo.AddComponent<WoodQuizBlockDrag>();
            }

            int w, h;
            GetBlockSize(block, out w, out h);

            int minC = int.MaxValue, minR = int.MaxValue;
            foreach (var cell in block.cells)
            {
                if (cell.x < minC) minC = cell.x;
                if (cell.y < minR) minR = cell.y;
            }

            var rt = blockGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w * _cellSize - 4f, h * _cellSize - 4f);
            rt.anchoredPosition = CellToBlockPosition(minC, minR, w, h);

            if (block.isMaster)
                blockImg.color = masterColor;
            else
                blockImg.color = (block.id % 2 == 0) ? woodLightColor : woodDarkColor;

            blockImg.raycastTarget = true;
            cg.blocksRaycasts = true;
            cg.interactable = true;

            drag.puzzle = this;
            drag.blockId = block.id;

            _blockObjects.Add(blockGo);
        }

        RenderCells();
    }

    private void GetBlockSize(WoodBlock block, out int w, out int h)
    {
        int minC = int.MaxValue, maxC = int.MinValue;
        int minR = int.MaxValue, maxR = int.MinValue;
        foreach (var cell in block.cells)
        {
            if (cell.x < minC) minC = cell.x;
            if (cell.x > maxC) maxC = cell.x;
            if (cell.y < minR) minR = cell.y;
            if (cell.y > maxR) maxR = cell.y;
        }
        w = maxC - minC + 1;
        h = maxR - minR + 1;
    }

    private Vector2 CellToBlockPosition(int col, int row, int blockW, int blockH)
    {
        float x = _gridOrigin.x + col * _cellSize;
        float y = _gridOrigin.y - row * _cellSize;
        x += (blockW - 1) * _cellSize / 2f;
        y -= (blockH - 1) * _cellSize / 2f;
        return new Vector2(x, y);
    }

    private void RenderCells()
    {
        for (int r = 0; r < _gridHeight; r++)
        {
            for (int c = 0; c < _gridWidth; c++)
            {
                if (_cellImages[r, c] == null) continue;
                int owner = _cellOwner[r, c];
                switch (owner)
                {
                    case WALL:
                        _cellImages[r, c].color = wallColor;
                        break;
                    case GOAL:
                        _cellImages[r, c].color = goalColor;
                        break;
                    case EMPTY:
                        _cellImages[r, c].color = emptyColor;
                        break;
                    default:
                        _cellImages[r, c].color = emptyColor;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Di chuyển block liên tục theo hướng (dx, dy) cho đến khi chạm tường/block khác.
    /// </summary>
    public bool TryMoveBlock(int blockId, int dx, int dy)
    {
        if (_puzzleCompleted) return false;
        if (blockId < 0 || blockId >= _blocks.Count) return false;
        if (dx == 0 && dy == 0) return false;
        dx = Mathf.Clamp(dx, -1, 1);
        dy = Mathf.Clamp(dy, -1, 1);
        if (dx != 0 && dy != 0) return false;

        var block = _blocks[blockId];

        // Tìm số ô có thể trượt liên tục
        int slideDistance = 0;
        while (true)
        {
            bool canMoveOneMore = true;
            foreach (var cell in block.cells)
            {
                int nc = cell.x + dx * (slideDistance + 1);
                int nr = cell.y + dy * (slideDistance + 1);
                if (!IsInBounds(nc, nr)) { canMoveOneMore = false; break; }
                int owner = _cellOwner[nr, nc];
                if (owner != EMPTY && owner != GOAL && owner != blockId)
                {
                    canMoveOneMore = false;
                    break;
                }
            }
            if (!canMoveOneMore) break;
            slideDistance++;
        }

        if (slideDistance == 0) return false; // Không thể di chuyển

        // Clear old positions
        foreach (var cell in block.cells)
        {
            _cellOwner[cell.y, cell.x] = IsGoalCell(cell.x, cell.y) ? GOAL : EMPTY;
        }

        // Cập nhật vị trí mới — trượt slideDistance ô
        for (int i = 0; i < block.cells.Count; i++)
        {
            Vector2Int cell = block.cells[i];
            int newX = cell.x + dx * slideDistance;
            int newY = cell.y + dy * slideDistance;
            block.cells[i] = new Vector2Int(newX, newY);
            _cellOwner[newY, newX] = blockId;
        }

        _moveCount++;
        UpdateBlockPositions();
        RenderCells();
        UpdateUI();

        if (CheckWin())
        {
            _puzzleCompleted = true;
            if (instructionText != null)
                instructionText.text = "🎉 Khối gỗ đã ra ngoài!";
            StartCoroutine(DelayedSuccess());
        }
        else if (_moveCount >= _maxMoves)
        {
            if (instructionText != null)
                instructionText.text = "❌ Hết lượt di chuyển!";
            StartCoroutine(DelayedFailure());
        }

        return true;
    }

    private bool IsGoalCell(int c, int r)
    {
        return _goalPos.x == c && _goalPos.y == r;
    }

    private bool IsInBounds(int c, int r)
    {
        return c >= 0 && c < _gridWidth && r >= 0 && r < _gridHeight;
    }

    private bool CheckWin()
    {
        if (_masterId < 0) return false;
        var master = _blocks[_masterId];
        foreach (var cell in master.cells)
        {
            if (IsGoalCell(cell.x, cell.y)) return true;
        }
        return false;
    }

    private void UpdateBlockPositions()
    {
        for (int i = 0; i < _blocks.Count && i < _blockObjects.Count; i++)
        {
            var block = _blocks[i];
            var go = _blockObjects[i];
            if (go == null) continue;

            int w, h;
            GetBlockSize(block, out w, out h);

            int minC = int.MaxValue, minR = int.MaxValue;
            foreach (var cell in block.cells)
            {
                if (cell.x < minC) minC = cell.x;
                if (cell.y < minR) minR = cell.y;
            }

            go.GetComponent<RectTransform>().anchoredPosition = CellToBlockPosition(minC, minR, w, h);
        }
    }

    private void UpdateUI()
    {
        if (instructionText != null && !_puzzleCompleted)
            instructionText.text = "Kéo khối gỗ đỏ ra lối thoát (G) → trượt liên tục";
        if (moveCountText != null)
            moveCountText.text = $"Bước: {_moveCount}/{_maxMoves}";
    }

    public void ResetPuzzle()
    {
        ParseBoard();
        BuildBlocks();
        _moveCount = 0;
        _puzzleCompleted = false;
        UpdateUI();
    }

    private IEnumerator DelayedSuccess()
    {
        yield return new WaitForSeconds(0.6f);
        CompletePuzzle(true);
    }

    private IEnumerator DelayedFailure()
    {
        yield return new WaitForSeconds(0.6f);
        CompletePuzzle(false);
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }

    private void Log(string msg)
    {
        Debug.Log(msg);
    }

    private void LogError(string msg)
    {
        Debug.LogError(msg);
    }
}

public class WoodQuizBlockDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public WoodQuizPuzzle puzzle;
    public int blockId;

    private Vector2 _dragStartPos;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Có thể thêm preview kéo ở đây nếu muốn
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (puzzle == null) return;

        Vector2 delta = eventData.position - _dragStartPos;
        float threshold = 15f;

        if (delta.magnitude < threshold)
        {
            TryClickMove();
            return;
        }

        int dx = 0, dy = 0;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            dx = delta.x > 0 ? 1 : -1;
        else
            dy = delta.y > 0 ? -1 : 1;

        puzzle.TryMoveBlock(blockId, dx, dy);
    }

    private void TryClickMove()
    {
        int[][] dirs = { new[] { 0, -1 }, new[] { 0, 1 }, new[] { -1, 0 }, new[] { 1, 0 } };
        foreach (var d in dirs)
        {
            if (puzzle.TryMoveBlock(blockId, d[0], d[1]))
                return;
        }
    }
}
