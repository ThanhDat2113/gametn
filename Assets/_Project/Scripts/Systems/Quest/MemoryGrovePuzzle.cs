using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MemoryCardData
{
    public Sprite portrait;
    public string id;
}

/// <summary>
/// Puzzle "Khu Rừng Ký Ức"
/// Sử dụng Sprite Pool thay vì CharacterData.
/// Ưu tiên dùng ảnh khác nhau trước khi tái sử dụng.
/// </summary>
public class MemoryGrovePuzzle : PuzzleBase
{
    [Header("UI References")]
    public GridLayoutGroup cardGrid;
    public Button[] cardButtons;
    public Image[] cardImages;
    public Text matchCountText;
    public Text mismatchCountText;
    public Button closeButton;

    // Giữ lại để tương thích với PuzzlePrefabCreator
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

    private int _totalPairs;
    private int _matchedPairs;
    private int _mismatches;

    private int _firstFlippedIndex = -1;
    private int _secondFlippedIndex = -1;

    private bool _isChecking;
    private bool _puzzleCompleted;

    private bool[] _isMatched;
    private MemoryCardData[] _cardData;

    private Sprite[] _portraitPool;
    private int _gridCols = 4;
    private int _gridRows = 3;
    private int _maxMismatches = 10;

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);

        if (data?.memoryConfig != null)
        {
            _portraitPool = data.memoryConfig.portraitPool;
            _gridCols = data.memoryConfig.gridCols;
            _gridRows = data.memoryConfig.gridRows;
        }

        _maxMismatches = data != null ? data.allowedAttempts : 10;

        _matchedPairs = 0;
        _mismatches = 0;

        _firstFlippedIndex = -1;
        _secondFlippedIndex = -1;

        _isChecking = false;
        _puzzleCompleted = false;

        int totalCards = _gridCols * _gridRows;

        if (totalCards % 2 != 0)
        {
            Debug.LogError("Memory Grove requires an even number of cards.");
            return;
        }

        _totalPairs = totalCards / 2;

        _isMatched = new bool[totalCards];
        _cardData = new MemoryCardData[totalCards];

        SetupCards();
        SetupButtons();
        UpdateUI();

        if (lorePopup != null)
            lorePopup.SetActive(false);
    }

    private void SetupCards()
    {
        int totalCards = _gridCols * _gridRows;
        int pairCount = totalCards / 2;

        if (_portraitPool == null || _portraitPool.Length == 0)
        {
            Debug.LogError("Memory Grove: Portrait Pool is empty.");
            return;
        }

        System.Random rng = new System.Random();

        List<Sprite> availablePortraits = new List<Sprite>(_portraitPool);
        List<MemoryCardData> generatedCards = new List<MemoryCardData>();

        for (int i = 0; i < pairCount; i++)
        {
            Sprite selectedPortrait;

            if (availablePortraits.Count > 0)
            {
                int randomIndex = rng.Next(availablePortraits.Count);

                selectedPortrait = availablePortraits[randomIndex];

                availablePortraits.RemoveAt(randomIndex);
            }
            else
            {
                selectedPortrait =
                    _portraitPool[rng.Next(_portraitPool.Length)];
            }

            string pairID = Guid.NewGuid().ToString();

            generatedCards.Add(new MemoryCardData
            {
                portrait = selectedPortrait,
                id = pairID
            });

            generatedCards.Add(new MemoryCardData
            {
                portrait = selectedPortrait,
                id = pairID
            });
        }

        _cardData = generatedCards
            .OrderBy(x => rng.Next())
            .ToArray();

        for (int i = 0; i < totalCards && i < cardImages.Length; i++)
        {
            if (cardImages[i] != null)
            {
                cardImages[i].sprite = _cardData[i].portrait;
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
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
        }

        if (loreCloseButton != null)
        {
            loreCloseButton.onClick.RemoveAllListeners();
            loreCloseButton.onClick.AddListener(() =>
            {
                if (lorePopup != null)
                    lorePopup.SetActive(false);
            });
        }
    }

    private void OnCardClicked(int index)
    {
        if (_puzzleCompleted) return;
        if (_isChecking) return;
        if (_isMatched[index]) return;

        SetCardFaceUp(index);

        if (_firstFlippedIndex == -1)
        {
            _firstFlippedIndex = index;
            return;
        }

        if (_secondFlippedIndex == -1 && index != _firstFlippedIndex)
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

        bool isMatch =
            _cardData[a] != null &&
            _cardData[b] != null &&
            _cardData[a].id == _cardData[b].id;

        if (isMatch)
        {
            _isMatched[a] = true;
            _isMatched[b] = true;

            _matchedPairs++;

            cardButtons[a].image.color = matchedColor;
            cardButtons[b].image.color = matchedColor;
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

    private void SetCardFaceUp(int index)
    {
        if (index < 0 || index >= cardImages.Length)
            return;

        if (cardImages[index] != null)
            cardImages[index].gameObject.SetActive(true);
    }

    private void SetCardFaceDown(int index)
    {
        if (index < 0 || index >= cardImages.Length)
            return;

        if (cardImages[index] != null)
            cardImages[index].gameObject.SetActive(false);
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