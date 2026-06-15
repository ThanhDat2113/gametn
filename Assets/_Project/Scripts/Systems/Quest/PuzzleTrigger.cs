using UnityEngine;

/// <summary>
/// Component gắn lên GameObject trong scene (tượng cổ, cánh cổng, khu rừng thiêng...).
/// Khi player đến gần và nhấn nút tương tác, nó LUÔN mở puzzle UI
/// (không phụ thuộc vào quest). Nếu quest đang có step tương ứng
/// (step.type == puzzleType && step.targetId == puzzleData.puzzleID),
/// thì tự động advance quest khi puzzle hoàn thành.
/// </summary>
public class PuzzleTrigger : MonoBehaviour
{
    [Header("Puzzle Data (ScriptableObject)")]
    public PuzzleData puzzleData; // Kéo PuzzleData asset vào đây

    [Header("Puzzle Prefab (UI Canvas)")]
    public GameObject puzzleUIPrefab; // Prefab chứa PuzzleBase component

    [Header("Visual")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt; // Dấu nhắc "Nhấn E"

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

    /// <summary>
    /// Luôn mở puzzle không cần check quest.
    /// </summary>
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

            // Gửi event cho QuestManager (nếu có quest đang chờ step này)
            if (QuestManager.Instance != null && puzzleData != null)
            {
                QuestManager.Instance.OnPuzzleCompleted(puzzleData.puzzleID);
            }
        }
        else
        {
            // Cho phép thử lại
            if (interactionPrompt != null && _playerInRange)
                interactionPrompt.SetActive(true);
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