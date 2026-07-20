using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle "Xếp Hình Trượt" — Grid 3×3 (8 ô + 1 ô trống).
/// Hỗ trợ 3 chế độ hiển thị:
///   1. Gán từng ảnh riêng lẻ (customTileSprites)
///   2. Tự động cắt từ 1 ảnh duy nhất (puzzleImage)
///   3. Hiển thị số (mặc định)
/// </summary>
public class SlidePuzzle : PuzzleBase
{
    [Header("UI References")]
    public GridLayoutGroup gridLayout;
    public Button[] tileButtons; // 9 buttons (3x3)
    public Text instructionText;
    public Text moveCountText;
    public Button closeButton;

    [Header("Image Puzzle")]
    [Tooltip("Mảng ảnh cho từng ô (thứ tự 0-8, ô cuối là ô trống). " +
             "Nếu có đủ 9 ảnh, sẽ ưu tiên dùng mảng này.")]
    public Sprite[] customTileSprites; // 👈 THÊM MỚI: kéo từng ảnh vào đây

    [Tooltip("Kéo 1 ảnh duy nhất vào đây để tự động cắt. " +
             "Chỉ dùng khi customTileSprites trống hoặc không đủ.")]
    public Sprite puzzleImage;

    [Tooltip("Màu nền cho ô trống")]
    public Color emptyColor = new Color(0.1f, 0.1f, 0.1f);
    [Tooltip("Màu viền cho các ô (tùy chọn)")]
    public Color borderColor = Color.white;

    [Header("Config")]
    public Color defaultColor = new Color(0.25f, 0.25f, 0.25f);
    public Color correctColor = Color.green;

    // Config từ PuzzleData
    private int _gridSize = 3;
    private int _maxMoves = 100;

    // Internal state
    private int[,] _board;
    private Button[] _buttons;
    private Image[] _buttonImages;
    private Sprite[] _tileSprites;   // mảng sprite đã chuẩn bị (từ custom hoặc slice)
    private int _emptyRow, _emptyCol;
    private int _moveCount = 0;
    private bool _puzzleCompleted = false;
    private bool _useImageMode = false;

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
        _buttonImages = new Image[_gridSize * _gridSize];

        // ── Chuẩn bị sprites cho các ô ──
        PrepareSprites();

        SetupBoard();
        SetupButtons();
        ShuffleBoard();

        UpdateUI();
    }

    /// <summary>
    /// Chuẩn bị mảng _tileSprites từ nguồn ưu tiên:
    /// 1. customTileSprites (nếu có đủ)
    /// 2. puzzleImage (cắt tự động)
    /// 3. null (dùng chế độ số)
    /// </summary>
    private void PrepareSprites()
    {
        int totalTiles = _gridSize * _gridSize;

        // Nếu có customTileSprites và đủ số lượng
        if (customTileSprites != null && customTileSprites.Length >= totalTiles)
        {
            _tileSprites = new Sprite[totalTiles];
            for (int i = 0; i < totalTiles; i++)
            {
                _tileSprites[i] = customTileSprites[i];
            }
            _useImageMode = true;
            Debug.Log($"[SlidePuzzle] Dùng {totalTiles} ảnh từ customTileSprites.");
            return;
        }

        // Nếu có puzzleImage -> cắt tự động
        if (puzzleImage != null)
        {
            Texture2D tex = puzzleImage.texture;
            if (tex != null)
            {
                int tileWidth = tex.width / _gridSize;
                int tileHeight = tex.height / _gridSize;

                _tileSprites = new Sprite[totalTiles];
                for (int row = 0; row < _gridSize; row++)
                {
                    for (int col = 0; col < _gridSize; col++)
                    {
                        int index = row * _gridSize + col;
                        Rect rect = new Rect(
                            col * tileWidth,
                            tex.height - (row + 1) * tileHeight,
                            tileWidth,
                            tileHeight
                        );
                        _tileSprites[index] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
                    }
                }
                _useImageMode = true;
                Debug.Log($"[SlidePuzzle] Đã cắt ảnh thành {totalTiles} mảnh.");
                return;
            }
        }

        // Fallback: không có ảnh
        _tileSprites = null;
        _useImageMode = false;
        Debug.Log($"[SlidePuzzle] Dùng chế độ số (không có ảnh).");
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

            Image img = tileButtons[i].GetComponent<Image>();
            if (img == null) img = tileButtons[i].gameObject.AddComponent<Image>();
            _buttonImages[i] = img;

            // Có thể thêm border hoặc outline nếu muốn
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
    }

    private void ShuffleBoard()
    {
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
                if (index >= _buttonImages.Length) continue;

                Image img = _buttonImages[index];
                int tileNumber = _board[r, c];

                if (tileNumber == 0) // ô trống
                {
                    img.sprite = null;
                    img.color = emptyColor;
                    _buttons[index].interactable = false;
                }
                else
                {
                    if (_useImageMode && _tileSprites != null && tileNumber - 1 < _tileSprites.Length)
                    {
                        // Hiển thị ảnh (từ custom hoặc slice)
                        Sprite sprite = _tileSprites[tileNumber - 1];
                        if (sprite != null)
                        {
                            img.sprite = sprite;
                            img.color = Color.white;
                        }
                        else
                        {
                            // Nếu sprite bị null, fallback về số
                            img.sprite = null;
                            img.color = defaultColor;
                            Text txt = _buttons[index].GetComponentInChildren<Text>();
                            if (txt != null) txt.text = tileNumber.ToString();
                        }
                    }
                    else
                    {
                        // Chế độ số
                        img.sprite = null;
                        img.color = defaultColor;
                        Text txt = _buttons[index].GetComponentInChildren<Text>();
                        if (txt != null) txt.text = tileNumber.ToString();
                    }
                    _buttons[index].interactable = true;
                }
            }
        }
    }

    private void UpdateUI()
    {
        if (instructionText != null)
            instructionText.text = _puzzleCompleted ? "🎉 Hoàn thành!" : "Sắp xếp các mảnh ghép";
        if (moveCountText != null)
            moveCountText.text = $"Số bước: {_moveCount}";
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}