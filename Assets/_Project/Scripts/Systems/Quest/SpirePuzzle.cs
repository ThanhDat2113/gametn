using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle "Tháp Huyền Thoại" — 3 cọc, N đĩa (mặc định 4).
/// Di chuyển từng đĩa từ cọc A sang cọc C.
/// Không được đặt đĩa to lên đĩa nhỏ.
/// Click cọc nguồn → click cọc đích.
/// </summary>
public class SpirePuzzle : PuzzleBase
{
    [Header("UI References")]
    public Button[] pegButtons; // 3 buttons: A, B, C
    public Transform[] pegContainers; // 3 containers chứa các đĩa
    public Text instructionText;
    public Text moveCountText;
    public Button closeButton;

    [Header("Config")]
    public Color diskColorLight = new Color(0.6f, 0.5f, 0.3f);
    public Color diskColorDark = new Color(0.4f, 0.3f, 0.15f);
    public Color selectedColor = Color.yellow;
    public Color baseColor = new Color(0.3f, 0.3f, 0.3f);

    // Config từ PuzzleData
    private int _diskCount = 4;
    private int _maxMoves = 50;

    // Internal
    private Stack<int>[] _pegs; // 3 cọc, mỗi cọc chứa các đĩa (int = kích thước)
    private int _selectedPeg = -1;
    private int _moveCount = 0;
    private bool _puzzleCompleted = false;
    private List<GameObject> _diskObjects = new List<GameObject>();

    private readonly string[] _pegNames = { "A", "B", "C" };

    // Prefab disk dimensions
    private readonly float _diskWidthBase = 40f;
    private readonly float _diskWidthStep = 18f;
    private readonly float _diskHeight = 20f;

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);

        if (data?.spireConfig != null)
        {
            _diskCount = Mathf.Clamp(data.spireConfig.diskCount, 3, 6);
            _maxMoves = data.spireConfig.maxMoves;
        }

        _selectedPeg = -1;
        _moveCount = 0;
        _puzzleCompleted = false;

        SetupPegs();
        CreateDisks();
        SetupButtons();
        UpdateUI();
    }

    private void SetupPegs()
    {
        _pegs = new Stack<int>[3];
        for (int i = 0; i < 3; i++)
            _pegs[i] = new Stack<int>();

        // Đặt tất cả đĩa lên cọc A (0), đĩa to nhất ở dưới
        for (int i = _diskCount; i >= 1; i--)
            _pegs[0].Push(i);
    }

    private void CreateDisks()
    {
        // Xóa đĩa cũ
        foreach (var d in _diskObjects)
            if (d != null) Destroy(d);
        _diskObjects.Clear();

        // Tạo đĩa cho mỗi cọc
        for (int p = 0; p < 3; p++)
        {
            int idx = 0;
            foreach (int diskSize in _pegs[p].Reverse())
            {
                var disk = CreateDiskObject(p, diskSize, idx);
                _diskObjects.Add(disk);
                idx++;
            }
        }
    }

    private GameObject CreateDiskObject(int peg, int size, int indexFromBottom)
    {
        var go = new GameObject($"Disk_{peg}_{size}");
        go.transform.SetParent(pegContainers[peg], false);

        var img = go.AddComponent<Image>();
        img.color = size % 2 == 0 ? diskColorLight : diskColorDark;

        float w = _diskWidthBase + (size - 1) * _diskWidthStep;
        float y = -(indexFromBottom * _diskHeight + _diskHeight / 2);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(w, _diskHeight);
        rect.anchoredPosition = new Vector2(0, y + 20);

        return go;
    }

    private void SetupButtons()
    {
        for (int i = 0; i < pegButtons.Length; i++)
        {
            int peg = i;
            pegButtons[i].onClick.RemoveAllListeners();
            pegButtons[i].onClick.AddListener(() => OnPegClicked(peg));
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
    }

    private void OnPegClicked(int peg)
    {
        if (_puzzleCompleted) return;

        if (_selectedPeg == -1)
        {
            // Chọn cọc nguồn
            if (_pegs[peg].Count == 0)
            {
                instructionText.text = "Cọc này không có đĩa!";
                return;
            }

            _selectedPeg = peg;
            pegButtons[peg].image.color = selectedColor;
            instructionText.text = $"Chọn cọc đích cho đĩa {_pegs[peg].Peek()}";
        }
        else
        {
            // Chọn cọc đích
            if (peg == _selectedPeg)
            {
                // Bỏ chọn
                pegButtons[_selectedPeg].image.color = baseColor;
                _selectedPeg = -1;
                instructionText.text = "Chọn cọc nguồn";
                return;
            }

            // Kiểm tra luật
            int topDisk = _pegs[_selectedPeg].Peek();
            if (_pegs[peg].Count > 0 && _pegs[peg].Peek() < topDisk)
            {
                instructionText.text = "Không thể đặt đĩa to lên đĩa nhỏ!";
                return;
            }

            // Di chuyển
            _pegs[_selectedPeg].Pop();
            _pegs[peg].Push(topDisk);
            _moveCount++;

            // Reset selection
            pegButtons[_selectedPeg].image.color = baseColor;
            _selectedPeg = -1;

            // Refresh UI
            RefreshDisks();
            UpdateUI();

            // Kiểm tra hoàn thành
            if (_pegs[2].Count == _diskCount)
            {
                _puzzleCompleted = true;
                instructionText.text = "🎉 Tháp đã được dời!";
                StartCoroutine(DelayedSuccess());
            }
            else
            {
                instructionText.text = "Chọn cọc nguồn";
            }
        }
    }

    private void RefreshDisks()
    {
        foreach (var d in _diskObjects)
            if (d != null) Destroy(d);
        _diskObjects.Clear();

        for (int p = 0; p < 3; p++)
        {
            int idx = 0;
            foreach (int diskSize in _pegs[p].Reverse())
            {
                var disk = CreateDiskObject(p, diskSize, idx);
                _diskObjects.Add(disk);
                idx++;
            }
        }
    }

    private IEnumerator DelayedSuccess()
    {
        yield return new WaitForSeconds(0.8f);
        CompletePuzzle(true);
    }

    private void UpdateUI()
    {
        if (instructionText != null && !_puzzleCompleted)
            instructionText.text = _selectedPeg == -1 ? "Chọn cọc nguồn" : "Chọn cọc đích";
        if (moveCountText != null)
            moveCountText.text = $"Bước: {_moveCount}/{_maxMoves}";
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}