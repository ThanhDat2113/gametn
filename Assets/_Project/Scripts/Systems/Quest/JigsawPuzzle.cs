using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class JigsawPuzzle : PuzzleBase
{
    [Header("UI References")]
    public RectTransform pieceContainer;
    public RectTransform boardContainer;
    public GameObject piecePrefab;
    public Text instructionText;
    public Button resetButton;
    public Button closeButton;

    [Header("Grid Settings")]
    public int gridCols = 3;
    public int gridRows = 2;
    public float boardWidth = 600f;
    public float boardHeight = 450f;

    [Header("Piece Sprites")]
    public Sprite[] pieceSprites;

    [Header("Visual Settings")]
    public Color boardBackgroundColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    public Color emptySlotColor = new Color(0.3f, 0.3f, 0.35f, 1f);

    [Header("Snap Settings")]
    [Tooltip("Khoảng cách tối đa để snap (để 0 để tự tính)")]
    public float snapDistance = 0f;

    [Header("Piece Spacing")]
    [Tooltip("Khoảng cách giữa các piece ban đầu với board")]
    public float pieceMargin = 30f;

    // ── Data ──────────────────────────────────────────────────────
    [System.Serializable]
    public class SlotData
    {
        public int index;
        public RectTransform slotRect;
        public Vector2 slotCenter;
        public int OccupiedPieceIndex = -1;
        public bool IsOccupied => OccupiedPieceIndex >= 0;
        public Vector2 slotSize;
    }

    public class PieceData
    {
        public int id;
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Vector2 originalPosition;
        public int correctSlotIndex;
        public int snappedSlotIndex = -1;
        public bool isDragging;
        public Image image;
        public Vector2 slotSize;
    }

    public List<SlotData> slots = new List<SlotData>();
    private List<PieceData> pieces = new List<PieceData>();
    private int totalPieces = 6;
    private bool puzzleCompleted = false;
    public bool IsPuzzleCompleted => puzzleCompleted;

    private float pieceWidth;
    private float pieceHeight;
    private Canvas parentCanvas;

    // ── Khởi tạo ──────────────────────────────────────────────────

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindObjectOfType<Canvas>();

        InitPuzzle();
        CreateBoard();
        CreatePieces();
        UpdateUI();
        RegisterButtons();
    }

    private void InitPuzzle()
    {
        puzzleCompleted = false;
        pieces.Clear();
        slots.Clear();
    }

    private void CreateBoard()
    {
        if (boardContainer == null) return;

        foreach (Transform child in boardContainer)
            Destroy(child.gameObject);

        int totalSlots = gridCols * gridRows;

        // Tính kích thước ô
        pieceWidth = boardWidth / gridCols;
        pieceHeight = boardHeight / gridRows;

        // Board nằm chính giữa màn hình
        boardContainer.sizeDelta = new Vector2(boardWidth, boardHeight);
        boardContainer.anchorMin = new Vector2(0.5f, 0.5f);
        boardContainer.anchorMax = new Vector2(0.5f, 0.5f);
        boardContainer.pivot = new Vector2(0.5f, 0.5f);
        boardContainer.anchoredPosition = Vector2.zero;

        Image boardBg = boardContainer.GetComponent<Image>();
        if (boardBg == null) boardBg = boardContainer.gameObject.AddComponent<Image>();
        boardBg.color = boardBackgroundColor;
        boardBg.raycastTarget = true;

        for (int i = 0; i < totalSlots; i++)
        {
            int row = i / gridCols;
            int col = i % gridCols;

            GameObject slotGo = new GameObject($"Slot_{i}");
            slotGo.transform.SetParent(boardContainer, false);
            RectTransform slotRect = slotGo.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0, 1);
            slotRect.anchorMax = new Vector2(0, 1);
            slotRect.pivot = new Vector2(0, 1);

            float xPos = col * pieceWidth;
            float yPos = -row * pieceHeight;
            slotRect.anchoredPosition = new Vector2(xPos, yPos);
            slotRect.sizeDelta = new Vector2(pieceWidth, pieceHeight);

            Image slotImg = slotGo.AddComponent<Image>();
            slotImg.color = emptySlotColor;
            slotImg.raycastTarget = false;

            // Tính slotCenter trong hệ tọa độ của pieceContainer
            Vector3[] corners = new Vector3[4];
            slotRect.GetWorldCorners(corners);
            Vector3 centerWorld = (corners[0] + corners[2]) / 2f;
            Vector2 localCenter;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                pieceContainer,
                RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, centerWorld),
                parentCanvas.worldCamera,
                out localCenter
            );

            slots.Add(new SlotData
            {
                index = i,
                slotRect = slotRect,
                slotCenter = localCenter,
                OccupiedPieceIndex = -1,
                slotSize = new Vector2(pieceWidth, pieceHeight)
            });
        }

        // Tự tính snapDistance nếu chưa set
        if (snapDistance <= 0)
        {
            snapDistance = Mathf.Min(pieceWidth, pieceHeight) * 0.4f;
        }
        Debug.Log($"[Jigsaw] Snap distance: {snapDistance}");
    }

    private void CreatePieces()
    {
        if (piecePrefab == null)
        {
            Debug.LogError("JigsawPuzzle: piecePrefab chưa gán!");
            return;
        }

        bool useSprites = (pieceSprites != null && pieceSprites.Length >= totalPieces);
        bool hasAllSprites = true;
        if (useSprites)
        {
            for (int i = 0; i < totalPieces; i++)
            {
                if (pieceSprites[i] == null)
                {
                    hasAllSprites = false;
                    break;
                }
            }
        }
        useSprites = useSprites && hasAllSprites;

        // Lấy tâm board trong hệ tọa độ của pieceContainer
        Vector2 boardCenter = boardContainer.anchoredPosition;
        float halfBoardWidth = boardWidth / 2f;

        for (int i = 0; i < totalPieces; i++)
        {
            GameObject pieceGo = Instantiate(piecePrefab, pieceContainer);
            pieceGo.name = $"Piece_{i}";
            RectTransform rt = pieceGo.GetComponent<RectTransform>();
            if (rt == null) rt = pieceGo.AddComponent<RectTransform>();

            rt.sizeDelta = new Vector2(pieceWidth, pieceHeight);

            Image img = pieceGo.GetComponent<Image>();
            if (img == null) img = pieceGo.AddComponent<Image>();

            if (useSprites)
            {
                img.sprite = pieceSprites[i];
                img.preserveAspect = false;
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
            else
            {
                img.color = GetPieceColor(i);
                img.preserveAspect = false;
                img.type = Image.Type.Simple;
            }
            img.raycastTarget = true;

            if (useSprites)
            {
                TextMeshProUGUI txt = pieceGo.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) Destroy(txt.gameObject);
            }
            else
            {
                TextMeshProUGUI txt = pieceGo.GetComponentInChildren<TextMeshProUGUI>();
                if (txt == null)
                {
                    GameObject txtGo = new GameObject("Number");
                    txtGo.transform.SetParent(pieceGo.transform, false);
                    txt = txtGo.AddComponent<TextMeshProUGUI>();
                    txt.text = (i + 1).ToString();
                    txt.fontSize = 30;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.color = Color.white;
                    RectTransform txtRt = txt.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;
                    txtRt.anchoredPosition = Vector2.zero;
                }
            }

            var drag = pieceGo.GetComponent<JigsawPieceDrag>();
            if (drag == null) drag = pieceGo.AddComponent<JigsawPieceDrag>();
            drag.pieceIndex = i;
            drag.puzzle = this;

            var cg = pieceGo.GetComponent<CanvasGroup>();
            if (cg == null) cg = pieceGo.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            // Tính vị trí ban đầu căn đều 2 bên
            Vector2 startPos;
            float ySpacing = (pieceHeight + 20f) * 0.8f;
            float yOffset = (i % 3 - 1) * ySpacing; // i%3 = 0,1,2 -> -1,0,1

            if (i < 3) // 3 mảnh bên trái
            {
                float xOffset = -(halfBoardWidth + pieceWidth / 2f + pieceMargin);
                startPos = boardCenter + new Vector2(xOffset, yOffset);
            }
            else // 3 mảnh bên phải
            {
                int idx = i - 3;
                float yOffset2 = (idx % 3 - 1) * ySpacing;
                float xOffset = halfBoardWidth + pieceWidth / 2f + pieceMargin;
                startPos = boardCenter + new Vector2(xOffset, yOffset2);
            }

            rt.anchoredPosition = startPos;

            pieces.Add(new PieceData
            {
                id = i,
                gameObject = pieceGo,
                rectTransform = rt,
                originalPosition = startPos,
                correctSlotIndex = i,
                snappedSlotIndex = -1,
                isDragging = false,
                image = img,
                slotSize = new Vector2(pieceWidth, pieceHeight)
            });
        }
    }

    private Color GetPieceColor(int index)
    {
        Color[] colors = new Color[]
        {
            new Color(0.9f, 0.2f, 0.2f, 1f),
            new Color(0.2f, 0.6f, 0.9f, 1f),
            new Color(0.2f, 0.9f, 0.2f, 1f),
            new Color(0.9f, 0.9f, 0.2f, 1f),
            new Color(0.9f, 0.4f, 0.6f, 1f),
            new Color(0.6f, 0.3f, 0.9f, 1f)
        };
        return colors[index % colors.Length];
    }

    // ── Public API ──────────────────────────────────────────────

    public void StartDraggingPiece(int pieceIndex)
    {
        if (puzzleCompleted) return;
        if (pieceIndex < 0 || pieceIndex >= pieces.Count) return;

        var piece = pieces[pieceIndex];
        if (piece.snappedSlotIndex >= 0)
        {
            int oldSlot = piece.snappedSlotIndex;
            if (oldSlot >= 0 && oldSlot < slots.Count)
                slots[oldSlot].OccupiedPieceIndex = -1;
            piece.snappedSlotIndex = -1;
        }
        piece.isDragging = true;
    }

    public void EndDraggingPiece(int pieceIndex, Vector2 dropPos)
    {
        if (puzzleCompleted) return;
        if (pieceIndex < 0 || pieceIndex >= pieces.Count) return;

        var piece = pieces[pieceIndex];
        piece.isDragging = false;

        piece.rectTransform.anchoredPosition = dropPos;

        Vector2 previousPosition = dropPos;
        int previousSlotIndex = piece.snappedSlotIndex;
        if (previousSlotIndex >= 0 && previousSlotIndex < slots.Count)
            previousPosition = slots[previousSlotIndex].slotCenter;

        // Tìm slot gần nhất
        int nearestSlot = -1;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < slots.Count; i++)
        {
            float dist = Vector2.Distance(dropPos, slots[i].slotCenter);
            if (dist < snapDistance && dist < nearestDist)
            {
                nearestDist = dist;
                nearestSlot = i;
            }
        }

        if (nearestSlot >= 0)
        {
            var targetSlot = slots[nearestSlot];
            piece.rectTransform.sizeDelta = targetSlot.slotSize;

            if (targetSlot.OccupiedPieceIndex >= 0 && targetSlot.OccupiedPieceIndex != pieceIndex)
            {
                int oldPieceIndex = targetSlot.OccupiedPieceIndex;
                var oldPiece = pieces[oldPieceIndex];

                if (previousSlotIndex >= 0 && previousSlotIndex < slots.Count)
                {
                    slots[previousSlotIndex].OccupiedPieceIndex = -1;
                    oldPiece.rectTransform.anchoredPosition = slots[previousSlotIndex].slotCenter;
                    oldPiece.snappedSlotIndex = previousSlotIndex;
                    slots[previousSlotIndex].OccupiedPieceIndex = oldPieceIndex;
                }
                else
                {
                    if (oldPiece.snappedSlotIndex >= 0)
                    {
                        slots[oldPiece.snappedSlotIndex].OccupiedPieceIndex = -1;
                    }
                    oldPiece.rectTransform.anchoredPosition = dropPos;
                    oldPiece.snappedSlotIndex = -1;
                }

                piece.rectTransform.anchoredPosition = targetSlot.slotCenter;
                piece.snappedSlotIndex = nearestSlot;
                targetSlot.OccupiedPieceIndex = pieceIndex;
            }
            else if (targetSlot.OccupiedPieceIndex == pieceIndex)
            {
                // đã ở slot này
            }
            else
            {
                piece.rectTransform.anchoredPosition = targetSlot.slotCenter;
                piece.snappedSlotIndex = nearestSlot;
                targetSlot.OccupiedPieceIndex = pieceIndex;
            }
        }
        else
        {
            piece.snappedSlotIndex = -1;
        }

        CheckWinCondition();
        UpdateUI();
    }

    private void CheckWinCondition()
    {
        if (puzzleCompleted) return;

        int correctCount = 0;
        foreach (var piece in pieces)
        {
            if (piece.snappedSlotIndex == piece.correctSlotIndex)
                correctCount++;
        }

        if (correctCount == totalPieces)
        {
            puzzleCompleted = true;
            if (instructionText != null)
                instructionText.text = "🎉 Hoàn thành! Bức tranh đã được ghép!";
            StartCoroutine(DelayedSuccess());
        }
    }

    // ── Reset ─────────────────────────────────────────────────────

    public void ResetPuzzle()
    {
        StopAllCoroutines();

        foreach (var piece in pieces)
        {
            piece.rectTransform.anchoredPosition = piece.originalPosition;
            piece.snappedSlotIndex = -1;
            piece.isDragging = false;
            piece.rectTransform.sizeDelta = piece.slotSize;
        }

        foreach (var slot in slots)
        {
            slot.OccupiedPieceIndex = -1;
        }

        puzzleCompleted = false;
        UpdateUI();
    }

    // ── UI update ─────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (instructionText != null && !puzzleCompleted)
        {
            int correct = 0;
            foreach (var piece in pieces)
                if (piece.snappedSlotIndex == piece.correctSlotIndex)
                    correct++;
            instructionText.text = $"Sắp xếp các mảnh vào đúng vị trí ({correct}/{totalPieces})";
        }

        // Giữ slot màu nền, không đổi màu
        foreach (var slot in slots)
        {
            if (slot.slotRect == null) continue;
            Image img = slot.slotRect.GetComponent<Image>();
            if (img == null) continue;
            img.color = emptySlotColor;
        }
    }

    private void RegisterButtons()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetPuzzle);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
        }
    }

    private IEnumerator DelayedSuccess()
    {
        yield return new WaitForSeconds(0.6f);
        CompletePuzzle(true);
    }

    public override void ClosePuzzle()
    {
        StopAllCoroutines();
        Destroy(gameObject);
    }
}

// ─── Component kéo thả ────────────────────────────────────────────

public class JigsawPieceDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public JigsawPuzzle puzzle;
    public int pieceIndex;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Canvas _parentCanvas;
    private GameObject _ghost;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas == null) _parentCanvas = FindObjectOfType<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (puzzle == null || puzzle.IsPuzzleCompleted) return;

        puzzle.StartDraggingPiece(pieceIndex);

        _ghost = new GameObject("DragGhost");
        _ghost.transform.SetParent(_parentCanvas.transform, false);
        var ghostRect = _ghost.AddComponent<RectTransform>();
        ghostRect.sizeDelta = _rectTransform.sizeDelta;
        ghostRect.position = eventData.position;

        var ghostImg = _ghost.AddComponent<Image>();
        ghostImg.sprite = GetComponent<Image>().sprite;
        ghostImg.color = GetComponent<Image>().color;

        _canvasGroup.alpha = 0.6f;
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (puzzle == null || puzzle.IsPuzzleCompleted) return;
        if (_ghost != null)
        {
            _ghost.GetComponent<RectTransform>().position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (puzzle == null || puzzle.IsPuzzleCompleted)
        {
            Destroy(_ghost);
            return;
        }

        Vector2 dropPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            puzzle.pieceContainer,
            eventData.position,
            _parentCanvas.worldCamera,
            out dropPos
        );

        if (_ghost != null) Destroy(_ghost);
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        puzzle.EndDraggingPiece(pieceIndex, dropPos);
    }
}