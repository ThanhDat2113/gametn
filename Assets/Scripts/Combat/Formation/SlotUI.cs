using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image icon;

    private int slotIndex;
    private FormationManager manager;
    private CanvasGroup canvasGroup;
    private GameObject dragGhost;

    public CharacterData CurrentCharacter { get; private set; }

    public void Initialize(int index, FormationManager mgr)
    {
        slotIndex = index;
        manager = mgr;
        Clear();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetCharacter(CharacterData character)
    {
        CurrentCharacter = character;
        nameText.text = character.characterName;
        if (character.portrait != null)
            icon.sprite = character.portrait;
        icon.gameObject.SetActive(true);
    }

    public void Clear()
    {
        CurrentCharacter = null;
        nameText.text = "Empty";
        icon.gameObject.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Kéo từ roster
        var draggedItem = eventData.pointerDrag?.GetComponent<CharacterDragItem>();
        if (draggedItem != null && draggedItem.CharacterData != null)
        {
            manager.TryPlaceCharacter(draggedItem.CharacterData, slotIndex);
        }
        else
        {
            // Kéo từ slot khác
            var sourceSlot = eventData.pointerDrag?.GetComponent<SlotUI>();
            if (sourceSlot != null && sourceSlot.CurrentCharacter != null)
            {
                manager.TrySwapCharacters(sourceSlot.slotIndex, slotIndex);
            }
        }
    }

    // Double‑click xóa nhân vật
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && CurrentCharacter != null)
        {
            manager.RemoveCharacter(slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CurrentCharacter == null) return;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Tạo ghost kéo
        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragGhost.transform.SetParent(transform.root);
        var ghostRect = dragGhost.GetComponent<RectTransform>();
        ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        ghostRect.position = eventData.position;

        var ghostImg = dragGhost.GetComponent<Image>();
        ghostImg.sprite = icon.sprite;
        ghostImg.color = new Color(1, 1, 1, 0.6f);

        var ghostGroup = dragGhost.GetComponent<CanvasGroup>();
        ghostGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            dragGhost.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null) Destroy(dragGhost);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        var targetSlot = eventData.pointerEnter?.GetComponent<SlotUI>();
        if (targetSlot != null && targetSlot != this)
        {
            manager.TrySwapCharacters(slotIndex, targetSlot.slotIndex);
        }
    }

    public bool IsPointerOver(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            GetComponent<RectTransform>(), screenPos);
    }
}