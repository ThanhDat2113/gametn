using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle "Khu Rừng Ký Ức" — Lật các ô để tìm cặp portrait nhân vật giống nhau.
/// Đọc config từ PuzzleData.memoryConfig.
/// </summary>
public class MemoryGrovePuzzle : PuzzleBase
{
    [Header("UI References")]
    public GridLayoutGroup cardGrid;
    public Button[] cardButtons;
    public Image[] cardImages; // Front image của mỗi card
    public Text matchCountText;
    public Text mismatchCountText;
    public Button closeButton;

    [Header("Lore Popup")]
    public GameObject lorePopup;
    public Text loreCharacterName;
    public Text loreText;
    public Button loreCloseButton;

    [Header("Config")]
    public Color defaultCardColor = Color.white;
    public Color matchedColor = new Color(0.7f, 1f, 0.7f, 0.5f);
    public float matchPause = 0.6f;
    public float mismatchPause = 0.8f;

    // Internal state
    private int _totalPairs;
    private int _matchedPairs = 0;
    private int _mismatches = 0;
    private int _firstFlippedIndex = -1;
    private int _secondFlippedIndex = -1;
    private bool _isChecking = false;
    private bool _puzzleCompleted = false;
    private bool[] _isMatched;
    private CharacterData[] _cardData;

    // Config từ PuzzleData
    private CharacterData[] _characterPool;
    private int _gridCols = 4;
    private int _gridRows = 3;
    private int _maxMismatches = 10;
    private bool _showLoreOnMatch = true;

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);

        // Đọc config từ PuzzleData
        if (data?.memoryConfig != null)
        {
            _characterPool = data.memoryConfig.characterPool;
            _gridCols = data.memoryConfig.gridCols;
            _gridRows = data.memoryConfig.gridRows;
            _showLoreOnMatch = data.memoryConfig.showLoreOnMatch;
        }
        _maxMismatches = data != null ? data.allowedAttempts : 10;

        _matchedPairs = 0;
        _mismatches = 0;
        _firstFlippedIndex = -1;
        _secondFlippedIndex = -1;
        _isChecking = false;
        _puzzleCompleted = false;

        int totalCards = _gridCols * _gridRows;
        _totalPairs = totalCards / 2;
        _isMatched = new bool[totalCards];
        _cardData = new CharacterData[totalCards];

        SetupCards();
        SetupButtons();

        if (lorePopup != null)
            lorePopup.SetActive(false);
    }

    private void SetupCards()
    {
        int totalCards = _gridCols * _gridRows;
        int pairCount = totalCards / 2;

        // Lấy danh sách nhân vật
        List<CharacterData> pool = new List<CharacterData>();
        if (_characterPool != null && _characterPool.Length > 0)
            pool = _characterPool.ToList();

        // Chọn ngẫu nhiên pairCount nhân vật
        List<CharacterData> selectedChars = new List<CharacterData>();
        System.Random rng = new System.Random();

        for (int i = 0; i < pairCount; i++)
        {
            CharacterData chosen = pool.Count > 0 ? pool[rng.Next(pool.Count)] : null;
            selectedChars.Add(chosen);
            selectedChars.Add(chosen); // Mỗi nhân vật xuất hiện 2 lần
        }

        // Xáo trộn
        _cardData = selectedChars.OrderBy(x => rng.Next()).ToArray();

        // Setup images
        for (int i = 0; i < totalCards && i < cardImages.Length; i++)
        {
            if (cardImages[i] != null)
            {
                if (_cardData[i] != null && _cardData[i].portrait != null)
                    cardImages[i].sprite = _cardData[i].portrait;
                else
                    cardImages[i].sprite = null;
            }
        }
    }

    private void SetupButtons()
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int index = i;
            cardButtons[i].onClick.RemoveAllListeners();
            cardButtons[i].onClick.AddListener(() => OnCardClicked(index));

            SetCardFaceDown(index);
            cardButtons[i].interactable = true;
            cardButtons[i].image.color = defaultCardColor;
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(() => CompletePuzzle(false));

        if (loreCloseButton != null)
            loreCloseButton.onClick.AddListener(() =>
            {
                if (lorePopup != null)
                    lorePopup.SetActive(false);
            });
    }

    private void OnCardClicked(int index)
    {
        if (_puzzleCompleted || _isChecking || _isMatched[index])
            return;

        SetCardFaceUp(index);

        if (_firstFlippedIndex == -1)
        {
            _firstFlippedIndex = index;
        }
        else if (_secondFlippedIndex == -1 && index != _firstFlippedIndex)
        {
            _secondFlippedIndex = index;
            _isChecking = true;
            SetAllButtonsInteractable(false);
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        yield return new WaitForSeconds(matchPause);

        int a = _firstFlippedIndex;
        int b = _secondFlippedIndex;

        bool isMatch = _cardData[a] != null && _cardData[b] != null
                    && _cardData[a].characterName == _cardData[b].characterName;

        if (isMatch)
        {
            _isMatched[a] = true;
            _isMatched[b] = true;
            _matchedPairs++;

            cardButtons[a].image.color = matchedColor;
            cardButtons[b].image.color = matchedColor;

            if (_showLoreOnMatch && _cardData[a] != null && lorePopup != null)
                ShowLorePopup(_cardData[a]);
        }
        else
        {
            _mismatches++;
            yield return new WaitForSeconds(mismatchPause);
            SetCardFaceDown(a);
            SetCardFaceDown(b);
        }

        _firstFlippedIndex = -1;
        _secondFlippedIndex = -1;
        _isChecking = false;

        UpdateUI();

        if (_matchedPairs >= _totalPairs)
        {
            _puzzleCompleted = true;
            yield return new WaitForSeconds(0.5f);
            CompletePuzzle(true);
            yield break;
        }

        if (_mismatches >= _maxMismatches)
        {
            yield return new WaitForSeconds(0.5f);
            CompletePuzzle(false);
            yield break;
        }

        SetAllButtonsInteractable(true);
    }

    private void ShowLorePopup(CharacterData character)
    {
        if (loreCharacterName != null)
            loreCharacterName.text = character.characterName;

        if (loreText != null)
        {
            if (!string.IsNullOrEmpty(character.lore))
                loreText.text = character.lore;
            else
                loreText.text = $"Bạn đã tìm thấy {character.characterName}!";
        }

        lorePopup.SetActive(true);
    }

    private void SetCardFaceUp(int index)
    {
        if (cardImages[index] != null)
            cardImages[index].gameObject.SetActive(true);
        cardButtons[index].image.color = defaultCardColor;
    }

    private void SetCardFaceDown(int index)
    {
        if (cardImages[index] != null)
            cardImages[index].gameObject.SetActive(false);
        cardButtons[index].image.color = defaultCardColor;
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            if (!_isMatched[i])
                cardButtons[i].interactable = interactable;
        }
    }

    private void UpdateUI()
    {
        if (matchCountText != null)
            matchCountText.text = $"Cặp đã tìm: {_matchedPairs}/{_totalPairs}";
        if (mismatchCountText != null)
            mismatchCountText.text = $"Sai: {_mismatches}/{_maxMismatches}";
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}