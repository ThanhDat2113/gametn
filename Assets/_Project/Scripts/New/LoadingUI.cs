using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingUI : MonoBehaviour
{
    public static LoadingUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject loadingPanel;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Không DontDestroyOnLoad vì Persistent Scene đã có sẵn, nhưng vẫn an toàn
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Đảm bảo panel tắt khi bắt đầu
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    /// <summary>
    /// Hiển thị Loading UI với progress
    /// </summary>
    public void Show(float progress = 0f)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        UpdateProgress(progress);
    }

    /// <summary>
    /// Cập nhật thanh progress
    /// </summary>
    public void UpdateProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progressSlider != null)
            progressSlider.value = progress;
        if (progressText != null)
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    /// <summary>
    /// Ẩn Loading UI
    /// </summary>
    public void Hide()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    /// <summary>
    /// Kiểm tra xem loading UI có đang hiển thị không
    /// </summary>
    public bool IsVisible()
    {
        return loadingPanel != null && loadingPanel.activeSelf;
    }
}