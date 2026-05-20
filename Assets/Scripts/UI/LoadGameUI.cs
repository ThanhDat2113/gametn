using UnityEngine;
using UnityEngine.UI;

public class LoadGameUI : MonoBehaviour
{
    [Header("Panel References")]
    public ScrollRect scrollRect;
    public RectTransform slotContainer;   // Content
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
        // Buộc cập nhật layout và cuộn lên đầu
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    void CreateSlots()
    {
        // Xóa slot cũ
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        // Tạo slot mới
        for (int i = 0; i < totalSlots; i++)
        {
            Instantiate(slotPrefab, slotContainer);
        }

        Debug.Log($"Load panel: created {slotContainer.childCount} slots");
    }

    void SetupScrollRect()
    {
        if (scrollRect == null) return;

        // Không cho kéo quá biên
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Thêm ContentSizeFitter để tự động điều chỉnh chiều cao
        ContentSizeFitter csf = slotContainer.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = slotContainer.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Cập nhật kích thước content
        Canvas.ForceUpdateCanvases();
    }

    // Các hàm mở/đóng (gán từ nút)
    public void OpenLoadPanel() => gameObject.SetActive(true);
    public void CloseLoadPanel() => gameObject.SetActive(false);
}