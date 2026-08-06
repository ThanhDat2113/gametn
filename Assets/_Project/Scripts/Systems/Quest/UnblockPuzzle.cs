using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Puzzle Unblock Me – bảng 6x6 với các khối gỗ trượt.
/// Mục tiêu: đưa khối số 8 (C3:D3) đến vị trí E3:F3 (cột 4-5, hàng 2).
/// </summary>
public class UnblockMePuzzle : PuzzleBase
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
    public Color goalColor = new Color(0.20f, 0.75f, 0.35f, 1f);
    public Color targetBlockColor = new Color(0.85f, 0.25f, 0.20f, 1f);
    public Color[] blockColors = new Color[]
    {
        new Color(0.72f, 0.52f, 0.30f, 1f),
        new Color(0.55f, 0.38f, 0.20f, 1f),
        new Color(0.60f, 0.70f, 0.40f, 1f),
        new Color(0.40f, 0.60f, 0.80f, 1f),
        new Color(0.80f, 0.50f, 0.60f, 1f),
        new Color(0.50f, 0.70f, 0.70f, 1f),
        new Color(0.90f, 0.70f, 0.30f, 1f),
        new Color(0.70f, 0.40f, 0.60f, 1f),
    };

    // ── Dữ liệu khối ──────────────────────────────────────────────
    private class BlockData
    {
        public int id;
        public List<Vector2Int> cells;
        public bool isHorizontal;
        public bool isTarget;
        public Color color;
        public GameObject gameObject;
    }

    private List<BlockData> blocks = new List<BlockData>();
    private int[,] board;
    private int gridRows = 6;
    private int gridCols = 6;
    private int targetBlockId = 8;
    private int moves = 0;
    private bool puzzleCompleted = false;

    // ✅ Public property để UnblockBlockDrag có thể kiểm tra
    public bool IsPuzzleCompleted => puzzleCompleted;

    // Vị trí đích: E3:F3 → (row=2, col=4) và (row=2, col=5)
    private Vector2Int[] goalPositions = new Vector2Int[]
    {
        new Vector2Int(4, 2),
        new Vector2Int(5, 2)
    };

    private Image[,] cellImages;
    private float cellSize;
    private Vector2 gridOrigin;

    // ── Khởi tạo ──────────────────────────────────────────────────

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);
        InitBoard();
        BuildGridUI();
        RenderBlocks();
        UpdateUI();
        RegisterButtons();
    }

    private void InitBoard()
    {
        board = new int[gridRows, gridCols];
        for (int r = 0; r < gridRows; r++)
            for (int c = 0; c < gridCols; c++)
                board[r, c] = -1;

        blocks.Clear();

        // Định nghĩa các khối theo dữ liệu đề bài
        var rawBlocks = new List<(int id, List<(int row, int col)> cells)>
        {
            (1, new List<(int,int)> { (0,0), (0,1) }),      // A1:B1
            (2, new List<(int,int)> { (0,3), (1,3) }),      // D1:D2
            (3, new List<(int,int)> { (0,4), (1,4), (2,4) }),// E1:E3
            (4, new List<(int,int)> { (0,5), (1,5) }),      // F1:F2
            (5, new List<(int,int)> { (1,0), (1,1) }),      // A2:B2
            (6, new List<(int,int)> { (2,0), (3,0) }),      // A3:A4
            (7, new List<(int,int)> { (2,1), (3,1) }),      // B3:B4
            (8, new List<(int,int)> { (2,2), (2,3) }),      // C3:D3 – TARGET
            (9, new List<(int,int)> { (3,2), (4,2) }),      // C4:C5
            (10, new List<(int,int)> { (4,0), (5,0) }),     // A5:A6
            (11, new List<(int,int)> { (4,3), (4,4) }),     // D5:E5
            (12, new List<(int,int)> { (5,3), (5,4) }),     // D6:E6
        };

        for (int i = 0; i < rawBlocks.Count; i++)
        {
            var raw = rawBlocks[i];
            var block = new BlockData
            {
                id = raw.id,
                cells = new List<Vector2Int>(),
                isTarget = (raw.id == targetBlockId)
            };
            foreach (var cell in raw.cells)
            {
                Vector2Int pos = new Vector2Int(cell.col, cell.row);
                block.cells.Add(pos);
                board[pos.y, pos.x] = raw.id;
            }
            // Xác định hướng
            if (block.cells.Count > 1)
            {
                bool sameRow = true;
                bool sameCol = true;
                for (int j = 1; j < block.cells.Count; j++)
                {
                    if (block.cells[j].y != block.cells[0].y) sameRow = false;
                    if (block.cells[j].x != block.cells[0].x) sameCol = false;
                }
                block.isHorizontal = sameRow;
                if (!sameRow && !sameCol) block.isHorizontal = true;
            }
            else block.isHorizontal = true;

            block.color = block.isTarget ? targetBlockColor : blockColors[(block.id - 1) % blockColors.Length];
            blocks.Add(block);
        }
    }

    // ── Xây dựng UI ──────────────────────────────────────────────

    private void BuildGridUI()
    {
        if (gridLayout == null)
        {
            Debug.LogError("UnblockMePuzzle: gridLayout chưa gán!");
            return;
        }

        foreach (Transform child in gridLayout.transform)
            if (child != null) Destroy(child.gameObject);

        float containerWidth = gridLayout.GetComponent<RectTransform>().rect.width;
        float containerHeight = gridLayout.GetComponent<RectTransform>().rect.height;
        cellSize = Mathf.Min(containerWidth / gridCols, containerHeight / gridRows);
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = Vector2.zero;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridCols;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        cellImages = new Image[gridRows, gridCols];
        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                GameObject cell = new GameObject($"Cell_{r}_{c}");
                cell.transform.SetParent(gridLayout.transform, false);
                Image img = cell.AddComponent<Image>();
                img.raycastTarget = false;

                // Đánh dấu ô đích
                bool isGoal = false;
                foreach (var goal in goalPositions)
                {
                    if (goal.x == c && goal.y == r) isGoal = true;
                }
                img.color = isGoal ? goalColor : emptyColor;
                cellImages[r, c] = img;
            }
        }

        float gridW = gridCols * cellSize;
        float gridH = gridRows * cellSize;
        gridOrigin = new Vector2(-gridW / 2f + cellSize / 2f, gridH / 2f - cellSize / 2f);
    }

    private void RenderBlocks()
    {
        foreach (var block in blocks)
        {
            if (block.gameObject != null) Destroy(block.gameObject);
        }

        Transform container = blockContainer != null ? blockContainer : gridLayout.transform.parent;

        foreach (var block in blocks)
        {
            GameObject go;
            if (blockPrefab != null)
            {
                go = Instantiate(blockPrefab, container, false);
                var img = go.GetComponent<Image>();
                if (img == null) img = go.AddComponent<Image>();
                var drag = go.GetComponent<UnblockBlockDrag>();
                if (drag == null) drag = go.AddComponent<UnblockBlockDrag>();
                block.gameObject = go;
            }
            else
            {
                go = new GameObject($"Block_{block.id}");
                go.transform.SetParent(container, false);
                var img = go.AddComponent<Image>();
                var drag = go.AddComponent<UnblockBlockDrag>();
                block.gameObject = go;
            }

            int minCol = int.MaxValue, minRow = int.MaxValue;
            int maxCol = int.MinValue, maxRow = int.MinValue;
            foreach (var cell in block.cells)
            {
                if (cell.x < minCol) minCol = cell.x;
                if (cell.x > maxCol) maxCol = cell.x;
                if (cell.y < minRow) minRow = cell.y;
                if (cell.y > maxRow) maxRow = cell.y;
            }
            int blockW = maxCol - minCol + 1;
            int blockH = maxRow - minRow + 1;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(blockW * cellSize - 4f, blockH * cellSize - 4f);
            rt.anchoredPosition = CellToBlockPosition(minCol, minRow, blockW, blockH);

            var imgComp = go.GetComponent<Image>();
            imgComp.color = block.color;
            imgComp.raycastTarget = true;

            var dragComp = go.GetComponent<UnblockBlockDrag>();
            dragComp.puzzle = this;
            dragComp.blockId = block.id;

            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        UpdateCellColors();
    }

    private Vector2 CellToBlockPosition(int col, int row, int blockW, int blockH)
    {
        float x = gridOrigin.x + col * cellSize;
        float y = gridOrigin.y - row * cellSize;
        x += (blockW - 1) * cellSize / 2f;
        y -= (blockH - 1) * cellSize / 2f;
        return new Vector2(x, y);
    }

    private void UpdateCellColors()
    {
        if (cellImages == null) return;

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                if (cellImages[r, c] == null) continue;

                int owner = board[r, c];
                bool isGoal = false;
                foreach (var goal in goalPositions)
                {
                    if (goal.x == c && goal.y == r) isGoal = true;
                }

                if (owner == -1)
                {
                    cellImages[r, c].color = isGoal ? goalColor : emptyColor;
                }
                else
                {
                    var block = blocks.Find(b => b.id == owner);
                    cellImages[r, c].color = block != null ? block.color : emptyColor;
                }
            }
        }
    }

    // ── Di chuyển khối ──────────────────────────────────────────

    public bool TryMoveBlock(int blockId, int dx, int dy)
    {
        if (puzzleCompleted || blocks == null || board == null) return false;
        if (dx == 0 && dy == 0) return false;
        if (dx != 0 && dy != 0) return false;

        var block = blocks.Find(b => b.id == blockId);
        if (block == null) return false;

        if (block.isHorizontal && dy != 0) return false;
        if (!block.isHorizontal && dx != 0) return false;

        // ── Tính số bước di chuyển tối đa (chỉ trong bảng) ──
        int maxSteps = 0;
        while (true)
        {
            bool canMoveOne = true;
            foreach (var cell in block.cells)
            {
                int nc = cell.x + dx * (maxSteps + 1);
                int nr = cell.y + dy * (maxSteps + 1);
                if (!IsInBounds(nc, nr))
                {
                    canMoveOne = false;
                    break;
                }
                int owner = board[nr, nc];
                if (owner != -1 && owner != blockId)
                {
                    canMoveOne = false;
                    break;
                }
            }
            if (!canMoveOne) break;
            maxSteps++;
        }

        if (maxSteps == 0) return false;

        // ── Lưu vị trí cũ để rollback nếu cần ──
        var oldCells = new List<Vector2Int>(block.cells);

        // ── Xóa vị trí cũ ──
        foreach (var cell in oldCells)
            board[cell.y, cell.x] = -1;

        // ── Cập nhật vị trí mới ──
        for (int i = 0; i < block.cells.Count; i++)
        {
            int newX = oldCells[i].x + dx * maxSteps;
            int newY = oldCells[i].y + dy * maxSteps;
            block.cells[i] = new Vector2Int(newX, newY);
            board[newY, newX] = blockId;
        }

        // ── Kiểm tra chiến thắng ──
        if (block.isTarget)
        {
            if (CheckWinCondition(block))
            {
                puzzleCompleted = true;
                if (instructionText != null)
                    instructionText.text = "🎉 Hoàn thành! Đã đưa khối đỏ đến đích!";
                UpdateBlockPositions();
                UpdateUI();
                StartCoroutine(DelayedSuccess());
                return true;
            }
        }

        UpdateBlockPositions();
        moves++;
        UpdateUI();
        return true;
    }

    private bool CheckWinCondition(BlockData block)
    {
        if (block == null || block.cells == null) return false;
        if (block.cells.Count != goalPositions.Length) return false;

        foreach (var cell in block.cells)
        {
            bool found = false;
            foreach (var goal in goalPositions)
            {
                if (cell.x == goal.x && cell.y == goal.y)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }

    private bool IsInBounds(int c, int r)
    {
        return c >= 0 && c < gridCols && r >= 0 && r < gridRows;
    }

    private void UpdateBlockPositions()
    {
        if (blocks == null) return;

        foreach (var block in blocks)
        {
            if (block == null || block.gameObject == null) continue;
            if (block.cells == null || block.cells.Count == 0) continue;

            int minCol = int.MaxValue, minRow = int.MaxValue;
            int maxCol = int.MinValue, maxRow = int.MinValue;
            foreach (var cell in block.cells)
            {
                if (cell.x < minCol) minCol = cell.x;
                if (cell.x > maxCol) maxCol = cell.x;
                if (cell.y < minRow) minRow = cell.y;
                if (cell.y > maxRow) maxRow = cell.y;
            }
            int blockW = maxCol - minCol + 1;
            int blockH = maxRow - minRow + 1;

            var rt = block.gameObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = CellToBlockPosition(minCol, minRow, blockW, blockH);
            }
        }
        UpdateCellColors();
    }

    // ── Reset ─────────────────────────────────────────────────────

    public void ResetPuzzle()
    {
        StopAllCoroutines();

        foreach (var block in blocks)
            if (block.gameObject != null) Destroy(block.gameObject);

        puzzleCompleted = false;
        moves = 0;
        InitBoard();
        BuildGridUI();
        RenderBlocks();
        UpdateUI();
    }

    // ── UI update ─────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (instructionText != null && !puzzleCompleted)
            instructionText.text = "Kéo khối đỏ (C3:D3) đến vị trí màu xanh lá (E3:F3)";
        if (moveCountText != null)
            moveCountText.text = $"Bước: {moves}";
    }

    private void RegisterButtons()
    {
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

    private IEnumerator DelayedSuccess()
    {
        yield return new WaitForSeconds(0.6f);
        CompletePuzzle(true);
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}

// ─── Component kéo cho từng khối ────────────────────────────────

public class UnblockBlockDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public UnblockMePuzzle puzzle;
    public int blockId;

    private Vector2 _dragStartPos;
    private bool _isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (puzzle == null || puzzle.IsPuzzleCompleted) return;
        _dragStartPos = eventData.position;
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Có thể thêm hiệu ứng kéo nếu muốn
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (puzzle == null || puzzle.IsPuzzleCompleted) return;

        Vector2 delta = eventData.position - _dragStartPos;
        float threshold = 15f;

        if (delta.magnitude < threshold)
        {
            if (puzzle.TryMoveBlock(blockId, 1, 0)) return;
            if (puzzle.TryMoveBlock(blockId, -1, 0)) return;
            if (puzzle.TryMoveBlock(blockId, 0, 1)) return;
            if (puzzle.TryMoveBlock(blockId, 0, -1)) return;
            return;
        }

        int dx = 0, dy = 0;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            dx = delta.x > 0 ? 1 : -1;
        else
            dy = delta.y > 0 ? -1 : 1;

        puzzle.TryMoveBlock(blockId, dx, dy);
    }
}