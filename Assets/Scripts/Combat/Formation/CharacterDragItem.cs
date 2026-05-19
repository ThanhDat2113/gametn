using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CharacterDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CharacterData CharacterData { get; private set; }

    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    private FormationManager manager;
    private CanvasGroup canvasGroup;
    private GameObject ghost;

    public void Initialize(CharacterData data, FormationManager mgr)
    {
        CharacterData = data;
        manager = mgr;
        icon.sprite = data.portrait;
        nameText.text = data.characterName;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        ghost = Instantiate(gameObject, transform.parent);
        ghost.GetComponent<CanvasGroup>().alpha = 0.8f;
        ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
        Destroy(ghost.GetComponent<CharacterDragItem>());
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghost != null)
            ghost.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        Destroy(ghost);

        int targetSlot = manager.GetSlotAtPosition(eventData.position);
        if (targetSlot != -1)
            manager.TryPlaceCharacter(CharacterData, targetSlot);
    }
}