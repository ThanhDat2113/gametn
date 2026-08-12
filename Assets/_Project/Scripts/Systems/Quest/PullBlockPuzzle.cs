using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Puzzle rút gỗ - các thanh gỗ xếp đè chồng theo thứ tự layer.
/// Layer 1 ở dưới cùng, layer 10 ở trên cùng.
/// Các thanh kế thừa đè lên nhau (1 đè 2, 2 đè 3, ..., 9 đè 10).
/// Chỉ kéo được thanh có layer cao nhất còn sống (10 → 1).
/// Khi kéo ra khỏi vùng, thanh biến mất.
/// </summary>
public class PullBlockPuzzle : PuzzleBase
{
    [Header("UI References")]
    public RectTransform blockContainer;
    public GameObject blockPrefab;
    public Text instructionText;
    public Text remainingText;
    public Button resetButton;
    public Button closeButton;

    [Header("Block Settings")]
    public int totalBlocks = 10;
    public float blockWidth = 180f;
    public float blockHeight = 40f;
    public float containerWidth = 400f;
    public float containerHeight = 400f;

    [Header("Stack Settings")]
    public float stackOffsetX = 15f;
    public float stackOffsetY = 12f;
    public float rotationSpread = 15f;

    [Header("Visual Settings")]
    public Color[] blockColors;
    public Color topBlockColor = Color.yellow;

    [Header("Block Sprites")]
    [Tooltip("Kéo ảnh cho từng thanh gỗ (có thể để trống để dùng màu)")]
    public Sprite[] blockSprites;

    // ── Data ──────────────────────────────────────────────────────
    public class BlockData
    {
        public int id;
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image image;
        public CanvasGroup canvasGroup;
        public PullBlockDrag drag;
        public bool isRemoved;
        public Vector2 originalPosition;
        public Quaternion originalRotation;
        public int layer; // 1 → totalBlocks
    }

    private List<BlockData> blocks = new List<BlockData>();
    private bool puzzleCompleted = false;
    public bool IsPuzzleCompleted => puzzleCompleted;

    private Vector2 containerCenter;
    private float containerLeft, containerRight, containerTop, containerBottom;

    // ── Khởi tạo ──────────────────────────────────────────────────

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);
        InitPuzzle();
        CreateBlocks();
        UpdateUI();
        RegisterButtons();
    }

    private void InitPuzzle()
    {
        puzzleCompleted = false;
        blocks.Clear();
    }

    private void CreateBlocks()
    {
        if (blockContainer == null || blockPrefab == null)
        {
            Debug.LogError("PullBlockPuzzle: Thiếu container hoặc prefab!");
            return;
        }

        foreach (Transform child in blockContainer)
            Destroy(child.gameObject);

        blockContainer.sizeDelta = new Vector2(containerWidth, containerHeight);
        containerCenter = blockContainer.anchoredPosition;
        containerLeft = containerCenter.x - containerWidth / 2f;
        containerRight = containerCenter.x + containerWidth / 2f;
        containerTop = containerCenter.y + containerHeight / 2f;
        containerBottom = containerCenter.y - containerHeight / 2f;

        // ── Tạo danh sách layer từ 1 → totalBlocks và xáo trộn ──
        List<int> layers = new List<int>();
        for (int i = 1; i <= totalBlocks; i++) layers.Add(i);
        for (int i = 0; i < layers.Count; i++)
        {
            int j = Random.Range(i, layers.Count);
            int temp = layers[i];
            layers[i] = layers[j];
            layers[j] = temp;
        }

        // ── Xây dựng vị trí đè chồng theo layer ──
        List<Vector2> positions = new List<Vector2>();
        Vector2 basePos = Vector2.zero;

        for (int i = 0; i < totalBlocks; i++)
        {
            float offsetX = Random.Range(-stackOffsetX, stackOffsetX);
            float offsetY = Random.Range(-stackOffsetY, stackOffsetY);
            Vector2 pos = basePos + new Vector2(offsetX, offsetY);

            float halfW = blockWidth / 2f;
            float halfH = blockHeight / 2f;
            pos.x = Mathf.Clamp(pos.x, -containerWidth / 2f + halfW, containerWidth / 2f - halfW);
            pos.y = Mathf.Clamp(pos.y, -containerHeight / 2f + halfH, containerHeight / 2f - halfH);

            positions.Add(pos);
            basePos = pos;
        }

        // ── Kiểm tra có đủ ảnh không ──
        bool useSprites = (blockSprites != null && blockSprites.Length >= totalBlocks);
        bool hasAllSprites = true;
        if (useSprites)
        {
            for (int i = 0; i < totalBlocks; i++)
            {
                if (blockSprites[i] == null)
                {
                    hasAllSprites = false;
                    break;
                }
            }
        }
        useSprites = useSprites && hasAllSprites;

        // ── Tạo các thanh gỗ ──
        for (int i = 0; i < totalBlocks; i++)
        {
            int layer = layers[i];
            Vector2 pos = positions[i];

            GameObject go = Instantiate(blockPrefab, blockContainer);
            go.name = $"Block_{i}";

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(blockWidth, blockHeight);
            rt.anchoredPosition = pos;

            float angle = Random.Range(-rotationSpread, rotationSpread);
            rt.rotation = Quaternion.Euler(0, 0, angle);

            Image img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();

            // ✅ Nếu có ảnh, dùng ảnh thay vì màu
            if (useSprites)
            {
                img.sprite = blockSprites[i];
                img.preserveAspect = false; // co giãn vừa khít
                img.type = Image.Type.Simple;
                img.color = Color.white;
            }
            else
            {
                Color color = blockColors != null && blockColors.Length > 0
                    ? blockColors[i % blockColors.Length]
                    : new Color(Random.value, Random.value, Random.value);
                img.color = color;
                img.preserveAspect = false;
                img.type = Image.Type.Simple;
            }
            img.raycastTarget = true;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = true;
            cg.interactable = true;

            PullBlockDrag drag = go.GetComponent<PullBlockDrag>();
            if (drag == null) drag = go.AddComponent<PullBlockDrag>();
            drag.blockIndex = i;
            drag.puzzle = this;

            // Nếu dùng ảnh, xóa số thứ tự (nếu có)
            if (useSprites)
            {
                TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) Destroy(txt.gameObject);
            }
            else
            {
                // Thêm số thứ tự nếu chưa có
                TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
                if (txt == null)
                {
                    GameObject txtGo = new GameObject("Number");
                    txtGo.transform.SetParent(go.transform, false);
                    txt = txtGo.AddComponent<TextMeshProUGUI>();
                    txt.text = (i + 1).ToString();
                    txt.fontSize = 20;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.color = Color.white;
                    RectTransform txtRt = txt.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;
                    txtRt.anchoredPosition = Vector2.zero;
                }
            }

            blocks.Add(new BlockData
            {
                id = i,
                gameObject = go,
                rectTransform = rt,
                image = img,
                canvasGroup = cg,
                drag = drag,
                isRemoved = false,
                originalPosition = pos,
                originalRotation = rt.rotation,
                layer = layer
            });
        }

        // ── Sắp xếp blocks theo layer và set sibling index ──
        blocks.Sort((a, b) => a.layer.CompareTo(b.layer));

        for (int i = 0; i < blocks.Count; i++)
        {
            blocks[i].rectTransform.SetSiblingIndex(i);
        }

        UpdateTopBlockVisual();
    }

    // ── Public API ──────────────────────────────────────────────

    public BlockData GetTopBlock()
    {
        BlockData top = null;
        foreach (var block in blocks)
        {
            if (block.isRemoved) continue;
            if (top == null || block.layer > top.layer)
                top = block;
        }
        return top;
    }

    public bool IsTopBlock(int blockIndex)
    {
        var top = GetTopBlock();
        return top != null && top.id == blockIndex;
    }

    public bool TryStartDragging(int blockIndex)
    {
        if (puzzleCompleted) return false;
        if (!IsTopBlock(blockIndex)) return false;

        var block = GetBlockById(blockIndex);
        if (block == null || block.isRemoved) return false;

        return true;
    }

    private BlockData GetBlockById(int id)
    {
        foreach (var b in blocks)
            if (b.id == id) return b;
        return null;
    }

    public void UpdateBlockPosition(int blockIndex, Vector2 newPos)
    {
        if (puzzleCompleted) return;
        var block = GetBlockById(blockIndex);
        if (block == null || block.isRemoved) return;

        block.rectTransform.anchoredPosition = newPos;
    }

    public bool TryRemoveBlock(int blockIndex)
    {
        if (puzzleCompleted) return false;

        var block = GetBlockById(blockIndex);
        if (block == null || block.isRemoved) return false;

        Vector2 pos = block.rectTransform.anchoredPosition;
        float halfWidth = blockWidth / 2f;
        float halfHeight = blockHeight / 2f;

        float angle = block.rectTransform.eulerAngles.z * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        float dx = halfWidth * cos + halfHeight * sin;
        float dy = halfWidth * sin + halfHeight * cos;

        bool isOutLeft = pos.x - dx < containerLeft;
        bool isOutRight = pos.x + dx > containerRight;
        bool isOutBottom = pos.y - dy < containerBottom;
        bool isOutTop = pos.y + dy > containerTop;

        if (isOutLeft || isOutRight || isOutBottom || isOutTop)
        {
            RemoveBlock(block);
            return true;
        }

        block.rectTransform.anchoredPosition = block.originalPosition;
        block.rectTransform.rotation = block.originalRotation;
        return false;
    }

    private void RemoveBlock(BlockData block)
    {
        block.isRemoved = true;
        block.canvasGroup.blocksRaycasts = false;
        block.canvasGroup.interactable = false;

        StartCoroutine(FadeOutAndDestroy(block));
    }

    private IEnumerator FadeOutAndDestroy(BlockData block)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Image img = block.image;
        Color startColor = img.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            img.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        blocks.Remove(block);
        Destroy(block.gameObject);

        UpdateTopBlockVisual();
        UpdateUI();

        if (blocks.Count == 0)
        {
            puzzleCompleted = true;
            if (instructionText != null)
                instructionText.text = "🎉 Hoàn thành! Bạn đã rút hết gỗ!";
            StartCoroutine(DelayedSuccess());
        }
    }

    private void UpdateTopBlockVisual()
    {
        var top = GetTopBlock();
        foreach (var block in blocks)
        {
            if (block.isRemoved) continue;

            if (block == top)
            {
                block.image.color = topBlockColor;
            }
            else
            {
                // Nếu dùng ảnh, giữ nguyên màu trắng, không đổi màu
                // Nếu dùng màu, giữ màu gốc
                if (blockSprites != null && blockSprites.Length > block.id && blockSprites[block.id] != null)
                {
                    // Dùng ảnh → giữ màu trắng
                    block.image.color = Color.white;
                }
                else
                {
                    // Dùng màu → giữ màu gốc
                    Color color = blockColors != null && blockColors.Length > 0
                        ? blockColors[block.id % blockColors.Length]
                        : new Color(Random.value, Random.value, Random.value);
                    block.image.color = color;
                }
            }
        }
    }

    // ── Reset ─────────────────────────────────────────────────────

    public void ResetPuzzle()
    {
        StopAllCoroutines();
        foreach (var block in blocks)
            if (block.gameObject != null) Destroy(block.gameObject);
        blocks.Clear();
        puzzleCompleted = false;
        CreateBlocks();
        UpdateUI();
    }

    // ── UI ────────────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (instructionText != null && !puzzleCompleted)
        {
            int remaining = 0;
            foreach (var b in blocks)
                if (!b.isRemoved) remaining++;
            instructionText.text = $"Kéo thanh gỗ trên cùng ra ngoài ({remaining}/{totalBlocks})";
        }

        if (remainingText != null)
        {
            int remaining = 0;
            foreach (var b in blocks)
                if (!b.isRemoved) remaining++;
            remainingText.text = remaining.ToString();
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

// ─── Component kéo cho từng thanh gỗ ────────────────────────────

public class PullBlockDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PullBlockPuzzle puzzle;
    public int blockIndex;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Vector2 _startPos;
    private bool _isDragging = false;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (puzzle == null || puzzle.IsPuzzleCompleted) return;

        if (!puzzle.TryStartDragging(blockIndex))
        {
            eventData.pointerDrag = null;
            return;
        }

        _isDragging = true;
        _startPos = _rectTransform.anchoredPosition;

        _canvasGroup.alpha = 0.7f;
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || puzzle == null || puzzle.IsPuzzleCompleted) return;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            puzzle.blockContainer,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );

        puzzle.UpdateBlockPosition(blockIndex, localPos);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (puzzle == null || puzzle.IsPuzzleCompleted)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            return;
        }

        bool removed = puzzle.TryRemoveBlock(blockIndex);
        if (!removed)
        {
            // TryRemoveBlock đã reset vị trí
        }

        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
    }
}