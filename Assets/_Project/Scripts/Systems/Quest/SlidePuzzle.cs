using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle "Xếp Hình Trượt" — Grid 3×3 (8 ô số + 1 ô trống).
/// Trượt các ô liền kề vào ô trống để sắp xếp theo thứ tự 1-8.
/// </summary>
public class SlidePuzzle : PuzzleBase
{
    [Header("UI References")]
    public GridLayoutGroup gridLayout;
    public Button[] tileButtons; // 9 buttons (3x3)
    public Text instructionText;
    public Text moveCountText;
    public Button closeButton;

    [Header("Config")]
    public Color defaultColor = new Color(0.25f, 0.25f, 0.25f);
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f);
    public Color correctColor = Color.green;

    // Config từ PuzzleData
    private int _gridSize = 3;
    private int _maxMoves = 100;

    // Internal state
    private int[,] _board;
    private Button[] _buttons;
    private int _emptyRow, _emptyCol;
    private int _moveCount = 0;
    private bool _puzzleCompleted = false;

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);

        if (data?.slideConfig != null)
        {
            _gridSize = data.slideConfig.gridSize;
            _maxMoves = data.slideConfig.maxMoves;
        }

        _moveCount = 0;
        _puzzleCompleted = false;
        _board = new int[_gridSize, _gridSize];
        _buttons = new Button[_gridSize * _gridSize];

        SetupBoard();
        SetupButtons();
        ShuffleBoard();

        UpdateUI();
    }

    private void SetupBoard()
    {
        int total = _gridSize * _gridSize;
        for (int i = 0; i < total - 1; i++)
        {
            int r = i / _gridSize;
            int c = i % _gridSize;
            _board[r, c] = i + 1; // 1,2,3,4,5,6,7,8
        }
        _emptyRow = _gridSize - 1;
        _emptyCol = _gridSize - 1;
        _board[_emptyRow, _emptyCol] = 0; // 0 = empty
    }

    private void SetupButtons()
    {
        for (int i = 0; i < tileButtons.Length && i < _buttons.Length; i++)
        {
            int index = i;
            tileButtons[i].onClick.RemoveAllListeners();
            tileButtons[i].onClick.AddListener(() => OnTileClicked(index));
            _buttons[i] = tileButtons[i];
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
    }

    private void ShuffleBoard()
    {
        // Xáo trộn bằng cách thực hiện 100 bước di chuyển ngẫu nhiên
        int[] dr = { -1, 0, 1, 0 };
        int[] dc = { 0, 1, 0, -1 };
        System.Random rng = new System.Random();

        for (int step = 0; step < 100; step++)
        {
            int dir = rng.Next(4);
            int nr = _emptyRow + dr[dir];
            int nc = _emptyCol + dc[dir];
            if (nr >= 0 && nr < _gridSize && nc >= 0 && nc < _gridSize)
            {
                SwapTiles(_emptyRow, _emptyCol, nr, nc);
            }
        }

        // Nếu shuffle ra đúng thứ tự thì shuffle thêm
        if (IsSolved())
        {
            ShuffleBoard();
            return;
        }

        RefreshUI();
    }

    private void SwapTiles(int r1, int c1, int r2, int c2)
    {
        int temp = _board[r1, c1];
        _board[r1, c1] = _board[r2, c2];
        _board[r2, c2] = temp;

        if (_board[r1, c1] == 0) { _emptyRow = r1; _emptyCol = c1; }
        if (_board[r2, c2] == 0) { _emptyRow = r2; _emptyCol = c2; }
    }

    private void OnTileClicked(int index)
    {
        if (_puzzleCompleted) return;

        int r = index / _gridSize;
        int c = index % _gridSize;

        // Kiểm tra xem ô này có kề với ô trống không
        if ((Mathf.Abs(r - _emptyRow) == 1 && c == _emptyCol) ||
            (Mathf.Abs(c - _emptyCol) == 1 && r == _emptyRow))
        {
            SwapTiles(r, c, _emptyRow, _emptyCol);
            _moveCount++;
            RefreshUI();
            UpdateUI();

            if (IsSolved())
            {
                _puzzleCompleted = true;
                if (instructionText != null)
                    instructionText.text = "🎉 Hoàn thành!";
                StartCoroutine(DelayedSuccess());
            }
        }
    }

    private IEnumerator DelayedSuccess()
    {
        yield return new WaitForSeconds(0.5f);
        CompletePuzzle(true);
    }

    private bool IsSolved()
    {
        int total = _gridSize * _gridSize;
        for (int i = 0; i < total - 1; i++)
        {
            int r = i / _gridSize;
            int c = i % _gridSize;
            if (_board[r, c] != i + 1) return false;
        }
        return _board[_gridSize - 1, _gridSize - 1] == 0;
    }

    private void RefreshUI()
    {
        for (int r = 0; r < _gridSize; r++)
        {
            for (int c = 0; c < _gridSize; c++)
            {
                int index = r * _gridSize + c;
                if (index >= _buttons.Length) continue;

                Text txt = _buttons[index].GetComponentInChildren<Text>();
                if (_board[r, c] == 0)
                {
                    _buttons[index].image.color = emptyColor;
                    if (txt != null) txt.text = "";
                    _buttons[index].interactable = false;
                }
                else
                {
                    _buttons[index].image.color = defaultColor;
                    if (txt != null) txt.text = _board[r, c].ToString();
                    _buttons[index].interactable = true;
                }
            }
        }
    }

    private void UpdateUI()
    {
        if (instructionText != null)
            instructionText.text = _puzzleCompleted ? "🎉 Hoàn thành!" : "Sắp xếp các số 1-8";
        if (moveCountText != null)
            moveCountText.text = $"Số bước: {_moveCount}";
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}