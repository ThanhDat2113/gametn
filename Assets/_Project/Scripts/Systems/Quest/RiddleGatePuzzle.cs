using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle "Cánh Cổng Câu Đố" — Người chơi đọc câu đố và chọn đáp án từ 4 lựa chọn.
/// Đọc config từ PuzzleData.riddleConfig.
/// </summary>
public class RiddleGatePuzzle : PuzzleBase
{
    [Header("UI References")]
    public Text riddleText;
    public Button[] answerButtons; // 4 buttons A/B/C/D
    public Text progressText;
    public Text attemptText;
    public Button closeButton;

    [Header("Config")]
    public Color defaultBtnColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    // Internal state
    private int _currentRiddleIndex = 0;
    private int _correctCount = 0;
    private int _attemptsInThisRiddle = 0;
    private bool _isAnswering = false;
    private bool _puzzleCompleted = false;

    // Config từ PuzzleData
    private string[] _riddles;
    private string[] _correctAnswers;
    private string[] _wrongA;
    private string[] _wrongB;
    private string[] _wrongC;
    private int _maxAttempts = 3;
    private int _requiredCorrect = 1;

    // Lưu index của đáp án đúng (sau shuffle) cho câu hiện tại
    private int _correctButtonIndex = -1;

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);

        // Đọc config từ PuzzleData
        if (data?.riddleConfig != null)
        {
            _riddles = data.riddleConfig.riddles;
            _correctAnswers = data.riddleConfig.correctAnswers;
            _wrongA = data.riddleConfig.wrongAnswerA;
            _wrongB = data.riddleConfig.wrongAnswerB;
            _wrongC = data.riddleConfig.wrongAnswerC;
            _requiredCorrect = data.riddleConfig.requiredCorrect;
        }
        _maxAttempts = data != null ? data.allowedAttempts : 3;

        _currentRiddleIndex = 0;
        _correctCount = 0;
        _puzzleCompleted = false;

        SetupButtons();
        ShowRiddle(0);
    }

    private void SetupButtons()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            answerButtons[i].image.color = defaultBtnColor;
            answerButtons[i].interactable = true;
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
    }

    private void ShowRiddle(int index)
    {
        if (_riddles == null || _riddles.Length == 0 || index >= _riddles.Length)
        {
            if (riddleText != null)
                riddleText.text = "Không có câu đố nào.";
            Debug.LogWarning("[RiddleGate] Không có câu đố nào được cấu hình trong PuzzleData!");
            CompletePuzzle(false);
            return;
        }

        _isAnswering = true;
        _attemptsInThisRiddle = 0;

        // Hiển thị câu đố
        if (riddleText != null)
            riddleText.text = _riddles[index];

        // Tạo mảng 4 đáp án và shuffle (Fisher-Yates)
        var answers = new string[] {
            _correctAnswers[index],
            _wrongA[index],
            _wrongB[index],
            _wrongC[index]
        };
        int[] indices = { 0, 1, 2, 3 };
        // Fisher-Yates shuffle
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }
        // Apply shuffle to answers
        var shuffledAnswers = new string[4];
        for (int i = 0; i < 4; i++)
            shuffledAnswers[i] = answers[indices[i]];

        // Ghi nhớ index của đáp án đúng: indices[i] == 0 => correctAnswers[index] nằm ở shuffledAnswers[i]
        _correctButtonIndex = -1;
        for (int i = 0; i < 4; i++)
        {
            if (indices[i] == 0) // indices[i] == 0 nghĩa là _correctAnswers[index] ở vị trí i
            {
                _correctButtonIndex = i;
                break;
            }
        }

        // Gán lên UI
        for (int i = 0; i < 4; i++)
        {
            Text btnText = answerButtons[i].GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = shuffledAnswers[i];

            answerButtons[i].image.color = defaultBtnColor;
            answerButtons[i].interactable = true;
        }

        UpdateUI();
    }

    private void OnAnswerSelected(int buttonIndex)
    {
        if (!_isAnswering || _puzzleCompleted) return;

        var btn = answerButtons[buttonIndex];
        btn.interactable = false;

        if (buttonIndex == _correctButtonIndex)
        {
            // Đúng
            btn.image.color = correctColor;
            _correctCount++;
            _isAnswering = false;

            // Vô hiệu hóa tất cả buttons
            foreach (var b in answerButtons)
                b.interactable = false;

            if (_correctCount >= _requiredCorrect)
            {
                // Hoàn thành puzzle
                _puzzleCompleted = true;
                if (riddleText != null)
                    riddleText.text = "🎉 Tất cả đều đúng! Cánh cổng mở ra...";
                Invoke(nameof(DelayedSuccess), 1.0f);
            }
            else
            {
                // Câu tiếp theo
                if (riddleText != null)
                    riddleText.text = "✅ Đúng!";
                Invoke(nameof(NextRiddle), 1.0f);
            }
        }
        else
        {
            // Sai
            btn.image.color = wrongColor;
            _attemptsInThisRiddle++;

            int totalAttempts = GetTotalAttempts();
            if (totalAttempts >= _maxAttempts)
            {
                // Thất bại
                _isAnswering = false;
                foreach (var b in answerButtons)
                    b.interactable = false;
                if (riddleText != null)
                    riddleText.text = "❌ Hết lượt! Cánh cổng khóa lại...";
                Invoke(nameof(DelayedFailure), 1.0f);
            }
            else
            {
                UpdateUI();
            }
        }
    }

    private void NextRiddle()
    {
        _currentRiddleIndex++;
        if (_currentRiddleIndex < _riddles.Length)
        {
            ShowRiddle(_currentRiddleIndex);
        }
        else
        {
            // Hết câu đố nhưng chưa đủ số đúng → thất bại
            _isAnswering = false;
            DelayedFailure();
        }
    }

    private int GetTotalAttempts()
    {
        return _attemptsInThisRiddle + (_currentRiddleIndex * _maxAttempts / Mathf.Max(_riddles.Length, 1));
    }

    private void DelayedSuccess()
    {
        CompletePuzzle(true);
    }

    private void DelayedFailure()
    {
        CompletePuzzle(false);
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            if (_requiredCorrect > 1 && _riddles != null)
                progressText.text = $"Đã đúng: {_correctCount}/{_requiredCorrect}";
            else
                progressText.text = $"Câu {_currentRiddleIndex + 1}/{_riddles?.Length ?? 1}";
        }
        if (attemptText != null)
            attemptText.text = $"Sai: {GetTotalAttempts()}/{_maxAttempts}";
    }

    public override void ClosePuzzle()
    {
        CancelInvoke();
        Destroy(gameObject);
    }
}