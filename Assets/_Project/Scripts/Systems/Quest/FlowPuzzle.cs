using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FlowPuzzle đơn giản hóa — click start → click các ô kề → click end.
/// Lưu vết đường đi bằng Stack, dễ debug.
/// </summary>
public class FlowPuzzle : PuzzleBase
{
    [Header("UI References")]
    public GridLayoutGroup gridLayout;
    public Button[] cellButtons;
    public Text instructionText;
    public Text progressText;
    public Button closeButton;

    [Header("Config")]
    public Color emptyColor = new Color(0.15f, 0.15f, 0.15f);
    public Color[] defaultPairColors;

    private int _gridSize = 5;
    private int _pairCount = 5;

    private int[,] _owner;
    private Vector2Int[] _starts;
    private Vector2Int[] _ends;
    private Color[] _colors;

    private int _currentPair = -1;
    // Lưu đường đi hiện tại của cặp đang nối (theo thứ tự)
    private List<Vector2Int> _currentPath = new List<Vector2Int>();
    private bool _puzzleCompleted = false;

    private readonly int[] _dr = { -1, 0, 1, 0 };
    private readonly int[] _dc = { 0, 1, 0, -1 };

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);
        _pairCount = 5;
        if (data?.flowConfig != null)
        {
            _gridSize = Mathf.Clamp(data.flowConfig.gridSize, 4, 7);
            if (data.flowConfig.pairColors != null && data.flowConfig.pairColors.Length >= 3)
                _pairCount = Mathf.Min(data.flowConfig.pairColors.Length, _gridSize);
        }

        _puzzleCompleted = false;
        _owner = new int[_gridSize, _gridSize];
        _starts = new Vector2Int[_pairCount];
        _ends = new Vector2Int[_pairCount];
        _colors = new Color[_pairCount];

        for (int r = 0; r < _gridSize; r++)
            for (int c = 0; c < _gridSize; c++)
                _owner[r, c] = -1;

        InitColors();
        GenerateBoard();
        SetupButtons();
        RefreshUI();
        UpdateUI();
    }

    private void InitColors()
    {
        if (defaultPairColors != null && defaultPairColors.Length >= _pairCount)
            for (int i = 0; i < _pairCount; i++) _colors[i] = defaultPairColors[i];
        else
        {
            Color[] b = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
            for (int i = 0; i < _pairCount; i++) _colors[i] = b[i % b.Length];
        }
    }

    // ──────── SINH BOARD — ĐƠN GIẢN, CHẮC CHẮN GIẢI ĐƯỢC ────────
    private void GenerateBoard()
    {
        int pairs = Mathf.Min(_pairCount, _gridSize);
        _pairCount = pairs;

        // Mỗi cặp nằm trên 1 hàng riêng, start bên trái, end bên phải
        for (int i = 0; i < pairs; i++)
        {
            int col = (i % 2 == 0) ? 0 : _gridSize - 1; // chẵn: trái, lẻ: phải
            _starts[i] = new Vector2Int(i, col);
            _ends[i] = new Vector2Int(i, (col == 0) ? _gridSize - 1 : 0);
            _owner[_starts[i].x, _starts[i].y] = i;
            _owner[_ends[i].x, _ends[i].y] = i;
        }
    }

    // ──────── GAMEPLAY ────────
    private void SetupButtons()
    {
        for (int i = 0; i < cellButtons.Length; i++)
        {
            int idx = i;
            cellButtons[i].onClick.RemoveAllListeners();
            cellButtons[i].onClick.AddListener(() => OnCellClick(idx));
        }
        if (closeButton != null)
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
    }

    private void OnCellClick(int index)
    {
        if (_puzzleCompleted) return;

        int r = index / _gridSize;
        int c = index % _gridSize;

        // Chưa chọn cặp
        if (_currentPair == -1)
        {
            if (_owner[r, c] < 0) return;
            int pp = _owner[r, c];
            if (IsComplete(pp)) return;
            _currentPair = pp;
            _currentPath.Clear();
            _currentPath.Add(new Vector2Int(r, c));
            instructionText.text = $"Đang nối cặp {pp + 1}";
            return;
        }

        int p = _currentPair;
        Vector2Int last = _currentPath[_currentPath.Count - 1];

        // Click vào end (cùng cặp) → hoàn thành
        if (r == _ends[p].x && c == _ends[p].y)
        {
            if (!IsAdj(last.x, last.y, r, c)) return;
            _currentPair = -1;
            _currentPath.Clear();
            RefreshUI();
            UpdateUI();
            if (CheckAllComplete())
            {
                _puzzleCompleted = true;
                instructionText.text = "🎉 Hoàn thành!";
                StartCoroutine(Delayed());
            }
            return;
        }

        // Click vào ô trống kề → mở rộng
        if (_owner[r, c] == -1 && IsAdj(last.x, last.y, r, c))
        {
            _owner[r, c] = p;
            _currentPath.Add(new Vector2Int(r, c));
            RefreshUI();
            return;
        }

        // Click vào start (cùng cặp) → hủy đường đã vẽ
        if (r == _starts[p].x && c == _starts[p].y)
        {
            // Xóa toàn bộ đường đã vẽ của cặp này (trừ start và end)
            for (int rr = 0; rr < _gridSize; rr++)
                for (int cc = 0; cc < _gridSize; cc++)
                    if (_owner[rr, cc] == p && !IsSE(p, rr, cc))
                        _owner[rr, cc] = -1;
            _currentPath.Clear();
            _currentPath.Add(new Vector2Int(r, c));
            RefreshUI();
            return;
        }
    }

    private bool IsAdj(int r1, int c1, int r2, int c2)
    {
        return (Mathf.Abs(r1 - r2) + Mathf.Abs(c1 - c2)) == 1;
    }

    private bool IsSE(int p, int r, int c)
    {
        return (_starts[p].x == r && _starts[p].y == c) || (_ends[p].x == r && _ends[p].y == c);
    }

    private bool IsComplete(int p)
    {
        // BFS
        bool[,] v = new bool[_gridSize, _gridSize];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(_starts[p]); v[_starts[p].x, _starts[p].y] = true;
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur.x == _ends[p].x && cur.y == _ends[p].y) return true;
            for (int d = 0; d < 4; d++)
            {
                int nr = cur.x + _dr[d], nc = cur.y + _dc[d];
                if (nr < 0 || nr >= _gridSize || nc < 0 || nc >= _gridSize) continue;
                if (v[nr, nc]) continue;
                if (_owner[nr, nc] == p) { v[nr, nc] = true; q.Enqueue(new Vector2Int(nr, nc)); }
            }
        }
        return false;
    }

    private bool CheckAllComplete()
    {
        for (int p = 0; p < _pairCount; p++)
            if (!IsComplete(p)) return false;
        return true;
    }

    private IEnumerator Delayed()
    {
        yield return new WaitForSeconds(0.8f);
        CompletePuzzle(true);
    }

    private void RefreshUI()
    {
        for (int r = 0; r < _gridSize; r++)
        {
            for (int c = 0; c < _gridSize; c++)
            {
                int idx = r * _gridSize + c;
                if (idx >= cellButtons.Length) continue;

                Text txt = cellButtons[idx].GetComponentInChildren<Text>();
                Image img = cellButtons[idx].image;

                if (_owner[r, c] >= 0)
                {
                    int p = _owner[r, c];
                    img.color = _colors[p % _colors.Length];
                    bool isS = _starts[p].x == r && _starts[p].y == c;
                    bool isE = _ends[p].x == r && _ends[p].y == c;
                    if (txt != null) txt.text = isS ? "●" : isE ? "○" : "·";
                }
                else
                {
                    img.color = emptyColor;
                    if (txt != null) txt.text = "";
                }
            }
        }
    }

    private void UpdateUI()
    {
        if (instructionText != null && !_puzzleCompleted)
            instructionText.text = _currentPair >= 0 ? $"Cặp {_currentPair + 1}" : "Chọn ● để nối";
        if (progressText != null)
        {
            int d = 0;
            for (int p = 0; p < _pairCount; p++) if (IsComplete(p)) d++;
            progressText.text = $"{d}/{_pairCount}";
        }
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}