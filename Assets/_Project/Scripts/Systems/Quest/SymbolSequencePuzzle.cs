using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle "Thần Điện Ký Ức" — Người chơi phải nhấn lại đúng thứ tự
/// các ký hiệu đã phát sáng. Mỗi round sequence dài hơn.
/// Đọc config từ PuzzleData.symbolConfig.
/// </summary>
public class SymbolSequencePuzzle : PuzzleBase
{
    [Header("UI References")]
    public GridLayoutGroup gridLayout;
    public Button[] symbolButtons; // 9 buttons (3x3)
    public Text instructionText;
    public Text progressText;
    public Text mistakeText;
    public Button closeButton;

    [Header("Config")]
    public Color defaultColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public float displayDuration = 0.6f;
    public float pauseBetween = 0.3f;

    // Internal state
    private List<int> _sequence = new List<int>();
    private int _currentRound = 0;
    private int _playerIndex = 0;
    private int _mistakes = 0;
    private bool _isShowingSequence = false;
    private bool _isPlayerTurn = false;
    private bool _puzzleCompleted = false;

    // Config từ PuzzleData
    private int _startLength = 3;
    private int _maxLength = 7;
    private int _maxMistakes = 3;

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);

        // Đọc config từ PuzzleData
        if (data?.symbolConfig != null)
        {
            _startLength = data.symbolConfig.startLength;
            _maxLength = data.symbolConfig.maxLength;
        }
        _maxMistakes = data != null ? data.allowedAttempts : 3;

        _mistakes = 0;
        _currentRound = 0;
        _puzzleCompleted = false;

        SetupButtons();
        StartNextRound();
    }

    private void SetupButtons()
    {
        for (int i = 0; i < symbolButtons.Length; i++)
        {
            int index = i;
            symbolButtons[i].onClick.RemoveAllListeners();
            symbolButtons[i].onClick.AddListener(() => OnSymbolClicked(index));
            symbolButtons[i].image.color = defaultColor;
            symbolButtons[i].interactable = false;
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
    }

    private void StartNextRound()
    {
        _currentRound++;
        int currentLength = Mathf.Min(_startLength + _currentRound - 1, _maxLength);
        _playerIndex = 0;

        // Tạo sequence ngẫu nhiên
        _sequence.Clear();
        for (int i = 0; i < currentLength; i++)
        {
            _sequence.Add(Random.Range(0, symbolButtons.Length));
        }

        UpdateUI();
        StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        _isShowingSequence = true;
        _isPlayerTurn = false;
        SetButtonsInteractable(false);

        if (instructionText != null)
            instructionText.text = "Ghi nhớ thứ tự...";

        // Reset all buttons
        foreach (var btn in symbolButtons)
            btn.image.color = defaultColor;

        yield return new WaitForSeconds(0.5f);

        // Hiển thị sequence
        foreach (int symbolIndex in _sequence)
        {
            symbolButtons[symbolIndex].image.color = highlightColor;
            yield return new WaitForSeconds(displayDuration);
            symbolButtons[symbolIndex].image.color = defaultColor;
            yield return new WaitForSeconds(pauseBetween);
        }

        _isShowingSequence = false;
        _isPlayerTurn = true;
        SetButtonsInteractable(true);

        if (instructionText != null)
            instructionText.text = "Nhấn lại theo thứ tự!";
    }

    private void OnSymbolClicked(int index)
    {
        if (!_isPlayerTurn || _isShowingSequence || _puzzleCompleted)
            return;

        // Flash khi nhấn
        StartCoroutine(FlashButton(index, highlightColor));

        if (index == _sequence[_playerIndex])
        {
            // Đúng
            _playerIndex++;

            if (_playerIndex >= _sequence.Count)
            {
                // Hoàn thành round này
                StartCoroutine(OnRoundComplete());
            }
        }
        else
        {
            // Sai
            _mistakes++;
            StartCoroutine(OnMistake());
        }
    }

    private IEnumerator FlashButton(int index, Color color)
    {
        symbolButtons[index].image.color = color;
        yield return new WaitForSeconds(0.15f);
        symbolButtons[index].image.color = defaultColor;
    }

    private IEnumerator OnRoundComplete()
    {
        _isPlayerTurn = false;
        SetButtonsInteractable(false);

        if (instructionText != null)
            instructionText.text = "Chính xác!";

        // Flash xanh tất cả
        foreach (var btn in symbolButtons)
            btn.image.color = correctColor;

        yield return new WaitForSeconds(0.8f);

        foreach (var btn in symbolButtons)
            btn.image.color = defaultColor;

        // Kiểm tra nếu đã đạt maxLength thì hoàn thành puzzle
        int currentLength = Mathf.Min(_startLength + _currentRound - 1, _maxLength);
        if (currentLength >= _maxLength)
        {
            // Hoàn thành toàn bộ puzzle
            _puzzleCompleted = true;
            if (instructionText != null)
                instructionText.text = "🎉 Hoàn thành!";
            yield return new WaitForSeconds(0.5f);
            CompletePuzzle(true);
        }
        else
        {
            // Round tiếp theo
            yield return new WaitForSeconds(0.5f);
            StartNextRound();
        }
    }

    private IEnumerator OnMistake()
    {
        _isPlayerTurn = false;
        SetButtonsInteractable(false);

        // Flash đỏ
        foreach (var btn in symbolButtons)
            btn.image.color = wrongColor;

        if (instructionText != null)
            instructionText.text = $"Sai! Còn {_maxMistakes - _mistakes} lần";

        yield return new WaitForSeconds(0.8f);

        foreach (var btn in symbolButtons)
            btn.image.color = defaultColor;

        if (_mistakes >= _maxMistakes)
        {
            // Thất bại
            if (instructionText != null)
                instructionText.text = "Thất bại!";
            yield return new WaitForSeconds(0.5f);
            CompletePuzzle(false);
        }
        else
        {
            // Thử lại round hiện tại
            _playerIndex = 0;
            StartCoroutine(ShowSequence());
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var btn in symbolButtons)
            btn.interactable = interactable;
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            int currentLength = Mathf.Min(_startLength + _currentRound - 1, _maxLength);
            progressText.text = $"Vòng {_currentRound}: {currentLength}/{_maxLength} ký hiệu";
        }
        if (mistakeText != null)
            mistakeText.text = $"Sai: {_mistakes}/{_maxMistakes}";
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}