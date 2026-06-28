using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// FlowPuzzle — Nối dây điện kiểu Among Us (Drag & Drop).
/// Kéo thả đầu dây trái sang ổ cắm phải cùng màu, tự động vẽ dây nối.
/// Auto-generate UI từ prefabs.
/// </summary>
public class FlowPuzzle : PuzzleBase
{
    [Header("=== UI CONTAINERS (Bắt buộc) ===")]
    public RectTransform leftColumn;
    public RectTransform rightColumn;
    public Transform wireContainer;

    [Header("=== PREFABS (Bắt buộc) ===")]
    public WireDragItem leftItemPrefab;
    public WireDropTarget rightItemPrefab;
    public Image wireSegmentPrefab;

    [Header("=== UI TEXT (Optional) ===")]
    public Text instructionText;
    public Text progressText;
    public Button closeButton;

    [Header("=== WIRE THEME ===")]
    public Color[] wireColors = {
        new Color(1f, 0.2f, 0.2f),
        new Color(0.2f, 0.4f, 1f),
        new Color(1f, 0.8f, 0f),
        new Color(0.2f, 1f, 0.2f),
    };
    public Sprite plugSprite;
    public Sprite socketSprite;
    public Sprite connectedSprite;
    public float wireThickness = 6f;

    private int _pairCount = 4;
    private int[] _leftOrder;
    private int[] _rightOrder;
    private WireDragItem[] _leftItems;
    private WireDropTarget[] _rightItems;
    private int _connectedCount = 0;
    private bool _puzzleCompleted = false;

    private Image _currentDragWire;
    private WireDragItem _currentDragItem;
    private Vector2 _dragStartPos;
    public bool IsDragging => _currentDragWire != null;

    public override void StartPuzzle(PuzzleData data, PuzzleTrigger source)
    {
        base.StartPuzzle(data, source);
        LoadConfig(data);
        ClearGeneratedItems();
        _leftOrder = GenerateShuffledOrder(_pairCount);
        do _rightOrder = GenerateShuffledOrder(_pairCount);
        while (AreArraysIdentical(_leftOrder, _rightOrder));
        GenerateUI();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => CompletePuzzle(false));
        }
        UpdateUI();
    }

    private void LoadConfig(PuzzleData data)
    {
        if (data?.flowConfig == null) return;
        _pairCount = Mathf.Clamp(data.flowConfig.pairCount, 3, 6);
        if (data.flowConfig.wireColors != null && data.flowConfig.wireColors.Length >= _pairCount)
            wireColors = data.flowConfig.wireColors;
        if (data.flowConfig.plugSprite != null) plugSprite = data.flowConfig.plugSprite;
        if (data.flowConfig.socketSprite != null) socketSprite = data.flowConfig.socketSprite;
    }

    private void ClearGeneratedItems()
    {
        if (_leftItems != null)
            foreach (var item in _leftItems)
                if (item != null) Destroy(item.gameObject);
        if (_rightItems != null)
            foreach (var item in _rightItems)
                if (item != null) Destroy(item.gameObject);
        if (wireContainer != null)
        {
            foreach (Transform child in wireContainer)
                Destroy(child.gameObject);
        }
    }

    private void GenerateUI()
    {
        if (leftItemPrefab == null || rightItemPrefab == null)
        {
            Debug.LogError("[FlowPuzzle] Thieu leftItemPrefab hoac rightItemPrefab!");
            CompletePuzzle(false);
            return;
        }
        _leftItems = new WireDragItem[_pairCount];
        _rightItems = new WireDropTarget[_pairCount];
        for (int i = 0; i < _pairCount; i++)
        {
            int leftColorIdx = _leftOrder[i];
            var left = Instantiate(leftItemPrefab, leftColumn);
            left.gameObject.SetActive(true);
            left.name = "WireLeft_" + i + "_Color" + leftColorIdx;
            left.colorIndex = leftColorIdx;
            left.flowPuzzleRef = this;
            left.image.color = GetWireColor(leftColorIdx);
            left.image.sprite = plugSprite;
            left.isConnected = false;
            left.canvasGroup.blocksRaycasts = true;
            _leftItems[i] = left;

            int rightColorIdx = _rightOrder[i];
            var right = Instantiate(rightItemPrefab, rightColumn);
            right.gameObject.SetActive(true);
            right.name = "WireRight_" + i + "_Color" + rightColorIdx;
            right.colorIndex = rightColorIdx;
            right.flowPuzzleRef = this;
            right.image.color = GetWireColor(rightColorIdx);
            right.image.sprite = socketSprite;
            right.isConnected = false;
            right.canvasGroup.blocksRaycasts = true;
            _rightItems[i] = right;
        }
    }

    // ═══════════════════════════════════════════
    //  DRAG HANDLING
    // ═══════════════════════════════════════════

    public void OnBeginDragWire(WireDragItem item, PointerEventData eventData)
    {
        _currentDragItem = item;
        _dragStartPos = item.GetScreenCenter();
        if (wireSegmentPrefab != null)
        {
            _currentDragWire = Instantiate(wireSegmentPrefab, wireContainer);
            _currentDragWire.gameObject.SetActive(true);
            _currentDragWire.name = "DragWire";
            _currentDragWire.color = GetWireColor(item.colorIndex);
            _currentDragWire.raycastTarget = false;
        }
        if (instructionText != null)
            instructionText.text = "Keo sang o cung mau...";
    }

    public void OnDragWire(PointerEventData eventData)
    {
        if (_currentDragWire == null) return;
        UpdateWirePosition(_currentDragWire, _dragStartPos, eventData.position);
    }

    public void OnEndDragWire(WireDragItem item, PointerEventData eventData)
    {
        if (_currentDragWire == null) return;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        bool handled = false;
        foreach (var hit in results)
        {
            var target = hit.gameObject.GetComponent<WireDropTarget>();
            if (target != null && !target.isConnected)
            {
                if (target.colorIndex == item.colorIndex)
                    StartCoroutine(ConnectWire(item, target));
                else
                    StartCoroutine(WrongWire(item, target));
                handled = true;
                break;
            }
        }
        if (!handled)
        {
            Destroy(_currentDragWire);
            _currentDragWire = null;
            _currentDragItem = null;
            if (instructionText != null)
                instructionText.text = "Keo dau day sang o cam!";
        }
    }

    private void UpdateWirePosition(Image wireImage, Vector2 startPos, Vector2 endPos)
    {
        Vector2 dir = endPos - startPos;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        RectTransform rt = wireImage.rectTransform;
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(distance, wireThickness);
        rt.position = startPos;
        rt.rotation = Quaternion.Euler(0, 0, angle);
    }

    // ═══════════════════════════════════════════
    //  CONNECT / WRONG
    // ═══════════════════════════════════════════

    private IEnumerator ConnectWire(WireDragItem left, WireDropTarget right)
    {
        Destroy(_currentDragWire);
        _currentDragWire = null;
        _currentDragItem = null;
        Color c = GetWireColor(left.colorIndex);
        var wire = Instantiate(wireSegmentPrefab, wireContainer);
        wire.gameObject.SetActive(true);
        wire.name = "ConnectedWire_" + left.colorIndex;
        wire.color = c;
        wire.raycastTarget = false;
        UpdateWirePosition(wire, left.GetScreenCenter(), right.GetScreenCenter());
        RectTransform rt = wire.rectTransform;
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(1.5f, 1f, t / 0.3f);
            rt.localScale = new Vector3(scale, scale, 1);
            yield return null;
        }
        rt.localScale = Vector3.one;
        left.isConnected = true;
        right.isConnected = true;
        _connectedCount++;
        if (connectedSprite != null)
        {
            left.image.sprite = connectedSprite;
            right.image.sprite = connectedSprite;
        }
        left.image.color = Color.white;
        right.image.color = Color.white;
        left.canvasGroup.blocksRaycasts = false;
        right.canvasGroup.blocksRaycasts = false;
        right.transform.localScale = Vector3.one;
        if (instructionText != null)
            instructionText.text = "Ket noi thanh cong!";
        yield return new WaitForSeconds(0.5f);
        UpdateUI();
        if (_connectedCount >= _pairCount)
        {
            _puzzleCompleted = true;
            if (instructionText != null)
                instructionText.text = "Tat ca da duoc ket noi!";
            yield return new WaitForSeconds(0.8f);
            CompletePuzzle(true);
        }
        else
        {
            if (instructionText != null)
                instructionText.text = "Keo dau day sang o cam!";
        }
    }

    private IEnumerator WrongWire(WireDragItem left, WireDropTarget right)
    {
        Destroy(_currentDragWire);
        _currentDragWire = null;
        _currentDragItem = null;
        Color originalColor = GetWireColor(right.colorIndex);
        right.image.color = Color.red;
        right.transform.localScale = Vector3.one * 1.2f;
        if (instructionText != null)
            instructionText.text = "Sai mau! Thu lai!";
        yield return new WaitForSeconds(0.4f);
        right.image.color = originalColor;
        right.transform.localScale = Vector3.one;
        if (instructionText != null)
            instructionText.text = "Keo dau day sang o cam!";
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    public Color GetWireColor(int colorIndex)
    {
        if (wireColors == null || wireColors.Length == 0) return Color.white;
        return wireColors[colorIndex % wireColors.Length];
    }

    private int[] GenerateShuffledOrder(int count)
    {
        int[] arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = i;
        System.Random rng = new System.Random();
        for (int i = count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            int temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }
        return arr;
    }

    private bool AreArraysIdentical(int[] a, int[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private void UpdateUI()
    {
        if (instructionText != null && !_puzzleCompleted)
            instructionText.text = "Keo dau day sang o cam!";
        if (progressText != null)
            progressText.text = "Da noi: " + _connectedCount + "/" + _pairCount;
    }

    public override void ClosePuzzle()
    {
        _currentDragWire = null;
        _currentDragItem = null;
        StopAllCoroutines();
        Destroy(gameObject);
    }
}


