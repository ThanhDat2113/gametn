using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveGameUI : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public int totalSlots = 24;
    public int columns = 4;

    private bool isInitialized = false;

    void OnEnable()
    {
        if (!isInitialized)
        {
            CreateSlots();
            SetupScrollRect();
            isInitialized = true;
        }
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    void CreateSlots()
    {
        foreach (Transform child in slotContainer) Destroy(child.gameObject);
        for (int i = 0; i < totalSlots; i++) Instantiate(slotPrefab, slotContainer);
    }

    void SetupScrollRect()
    {
        if (scrollRect == null) return;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        ContentSizeFitter csf = slotContainer.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = slotContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
}