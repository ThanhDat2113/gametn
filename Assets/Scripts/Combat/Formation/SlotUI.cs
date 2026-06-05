using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI levelText;

    private int slotIndex;
    private FormationManager manager;
    private CanvasGroup canvasGroup;
    private GameObject dragGhost;
    private Canvas rootCanvas;
    private bool isDragging = false;

    public CharacterData CurrentCharacter { get; private set; }

    public void Initialize(int index, FormationManager mgr)
    {
        slotIndex = index;
        manager = mgr;

        rootCanvas = GetComponentInParent<Canvas>();
        while (rootCanvas != null && !rootCanvas.isRootCanvas)
            rootCanvas = rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Clear();
    }

    public void SetCharacter(CharacterData character)
    {
        SetCharacter(character, 1);
    }

    public void SetCharacter(CharacterData character, int level)
    {
        CurrentCharacter = character;
        if (character.portrait != null)
            icon.sprite = character.portrait;
        icon.gameObject.SetActive(true);

        if (levelText != null)
        {
            levelText.text = $"LV.{level}";
            levelText.gameObject.SetActive(true);
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }

    public void Clear()
    {
        CurrentCharacter = null;
        icon.gameObject.SetActive(false);
        if (levelText != null)
            levelText.gameObject.SetActive(false);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedItem = eventData.pointerDrag?.GetComponent<CharacterDragItem>();
        if (draggedItem != null && draggedItem.CharacterData != null)
        {
            draggedItem.DestroyGhost();
            manager.TryPlaceCharacter(draggedItem.CharacterData, slotIndex);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && CurrentCharacter != null)
            manager.RemoveCharacter(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CurrentCharacter == null)
        {
            isDragging = false;
            return;
        }

        isDragging = true;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        Transform ghostParent = rootCanvas != null ? rootCanvas.transform : transform.root;
        dragGhost.transform.SetParent(ghostParent, false);

        var ghostRect = dragGhost.GetComponent<RectTransform>();
        ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.position = eventData.position;

        var ghostImg = dragGhost.GetComponent<Image>();
        ghostImg.sprite = icon.sprite;
        ghostImg.color = new Color(1f, 1f, 1f, 0.7f);
        ghostImg.raycastTarget = false;

        var ghostGroup = dragGhost.GetComponent<CanvasGroup>();
        ghostGroup.blocksRaycasts = false;
        ghostGroup.interactable = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragGhost == null) return;
        dragGhost.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        if (dragGhost != null)
        {
            Destroy(dragGhost);
            dragGhost = null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        var targetSlot = eventData.pointerEnter?.GetComponent<SlotUI>();
        if (targetSlot != null && targetSlot != this)
            manager.TrySwapCharacters(slotIndex, targetSlot.slotIndex);
    }

    public bool IsPointerOver(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), screenPos);
    }
}