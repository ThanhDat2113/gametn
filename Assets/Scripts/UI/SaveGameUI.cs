using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveGameUI : MonoBehaviour
{
    [Header("Panel References")]
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
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slot = Instantiate(slotPrefab, slotContainer);
            TextMeshProUGUI numberText = slot.transform.Find("SlotNumber")?.GetComponent<TextMeshProUGUI>();
            if (numberText != null)
                numberText.text = (i + 1).ToString();
            else
                Debug.LogWarning("Không tìm thấy Text 'SlotNumber' trong prefab slot");
        }
        Debug.Log($"Save panel: created {slotContainer.childCount} slots");
    }

    void SetupScrollRect()
    {
        if (scrollRect == null) return;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        ContentSizeFitter csf = slotContainer.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = slotContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Canvas.ForceUpdateCanvases();
    }

    public void OpenSavePanel() => gameObject.SetActive(true);
    public void CloseSavePanel() => gameObject.SetActive(false);
}