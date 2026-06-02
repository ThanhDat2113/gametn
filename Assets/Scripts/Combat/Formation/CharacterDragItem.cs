using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CharacterData CharacterData { get; private set; }

    [SerializeField] private Image icon;
    [SerializeField] private TMPro.TextMeshProUGUI levelText;

    private FormationManager manager;
    private CanvasGroup canvasGroup;
    private GameObject ghost;
    private Canvas rootCanvas;

    public void Initialize(CharacterData data, FormationManager mgr)
    {
        CharacterData = data;
        manager = mgr;
        icon.sprite = data.portrait;

        // Hiển thị level thực từ PlayerProgression
        if (levelText != null)
        {
            int level = 1;
            if (PlayerProgression.Instance != null)
                level = PlayerProgression.Instance.GetLevel(data);
            levelText.text = $"LV.{level}";
            levelText.gameObject.SetActive(true);
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rootCanvas = GetComponentInParent<Canvas>();
        while (rootCanvas != null && !rootCanvas.isRootCanvas)
            rootCanvas = rootCanvas.transform.parent?.GetComponentInParent<Canvas>();
    }

    public void ResetVisual()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void DestroyGhost()
    {
        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        Transform ghostParent = rootCanvas != null ? rootCanvas.transform : transform.root;
        ghost.transform.SetParent(ghostParent, false);

        var ghostRect = ghost.GetComponent<RectTransform>();
        ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.position = eventData.position;

        var ghostImg = ghost.GetComponent<Image>();
        ghostImg.sprite = icon.sprite;
        ghostImg.color = new Color(1f, 1f, 1f, 0.8f);
        ghostImg.raycastTarget = false;

        var ghostGroup = ghost.GetComponent<CanvasGroup>();
        ghostGroup.blocksRaycasts = false;
        ghostGroup.interactable = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghost != null)
            ghost.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}