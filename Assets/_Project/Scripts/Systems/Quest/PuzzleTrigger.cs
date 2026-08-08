using UnityEngine;

public class PuzzleTrigger : MonoBehaviour
{
    [Header("Puzzle Data (ScriptableObject)")]
    public PuzzleData puzzleData;

    [Header("Puzzle Prefab (UI Canvas)")]
    public GameObject puzzleUIPrefab;

    [Header("Visual")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt;

    private bool _playerInRange;
    private bool _hasPlayedSuccess;
    private PuzzleBase _activePuzzle;

    void Update()
    {
        if (_playerInRange && Input.GetKeyDown(interactKey))
        {
            TryStartPuzzle();
        }
    }

    public void TryStartPuzzle()
    {
        if (_hasPlayedSuccess)
        {
            Debug.Log($"[PuzzleTrigger] {puzzleData?.puzzleID} đã hoàn thành.");
            return;
        }

        if (puzzleData == null)
        {
            Debug.LogError("[PuzzleTrigger] Chưa gán PuzzleData!");
            return;
        }

        OpenPuzzleUI();
    }

    private void OpenPuzzleUI()
    {
        if (puzzleUIPrefab == null)
        {
            Debug.LogError($"[PuzzleTrigger] {puzzleData.puzzleID} chưa gán puzzleUIPrefab!");
            return;
        }

        if (_activePuzzle != null)
        {
            Debug.LogWarning("[PuzzleTrigger] Đã có puzzle đang mở.");
            return;
        }

        var go = Instantiate(puzzleUIPrefab);
        _activePuzzle = go.GetComponent<PuzzleBase>();

        if (_activePuzzle == null)
        {
            Debug.LogError($"[PuzzleTrigger] Prefab {puzzleUIPrefab.name} không có component PuzzleBase!");
            Destroy(go);
            return;
        }

        _activePuzzle.OnPuzzleFinished += OnPuzzleUIResult;
        _activePuzzle.StartPuzzle(puzzleData, this);

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void OnPuzzleUIResult(bool success)
    {
        if (_activePuzzle != null)
        {
            _activePuzzle.OnPuzzleFinished -= OnPuzzleUIResult;
            _activePuzzle = null;
        }

        if (success)
        {
            _hasPlayedSuccess = true;

            if (QuestManager.Instance != null && puzzleData != null)
            {
                Debug.Log($"[PuzzleTrigger] ✅ Gọi QuestManager.OnPuzzleCompleted({puzzleData.puzzleID})");
                QuestManager.Instance.OnPuzzleCompleted(puzzleData.puzzleID);
            }
            else
            {
                Debug.LogError($"[PuzzleTrigger] ❌ Không thể gọi QuestManager: Instance={QuestManager.Instance != null}, puzzleData={puzzleData != null}");
            }
        }
        else
        {
            // Cho phép thử lại
            if (interactionPrompt != null && _playerInRange)
                interactionPrompt.SetActive(true);
            Debug.Log("[PuzzleTrigger] Puzzle thất bại, có thể thử lại.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;

        if (interactionPrompt != null && !_hasPlayedSuccess)
            interactionPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
}