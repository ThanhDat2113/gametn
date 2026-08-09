using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingUIManager : MonoBehaviour
{
    public static LoadingUIManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject loadingPanel;
    public Slider progressBar;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI loadingMessage;

    [Header("Settings")]
    [Tooltip("Thời gian tối thiểu hiển thị loading (giây)")]
    public float minLoadTime = 1.5f;
    [Tooltip("Tự động ẩn loading khi cutscene bắt đầu")]
    public bool autoHideOnCutscene = true;

    private bool _isLoading = false;
    private float _loadStartTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Mặc định hiển thị loading khi game bắt đầu
        ShowLoading("Đang tải game...", true);
    }

    private void Update()
    {
        // Nếu autoHideOnCutscene bật, kiểm tra CutsceneIntro
        if (autoHideOnCutscene && _isLoading)
        {
            CutsceneIntro cutscene = FindObjectOfType<CutsceneIntro>();
            if (cutscene != null && cutscene.HasStartedMoving)
            {
                // Cutscene đã bắt đầu di chuyển → ẩn loading
                HideLoading();
            }
        }
    }

    /// <summary>
    /// Hiển thị loading panel
    /// </summary>
    public void ShowLoading(string message = "Đang tải...", bool force = false)
    {
        if (_isLoading && !force) return;

        _isLoading = true;
        _loadStartTime = Time.time;

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (loadingMessage != null)
            loadingMessage.text = message;

        if (progressBar != null)
            progressBar.value = 0f;

        if (progressText != null)
            progressText.text = "0%";

        Debug.Log("[LoadingUIManager] ShowLoading: " + message);
    }

    /// <summary>
    /// Cập nhật tiến trình loading
    /// </summary>
    public void UpdateProgress(float progress)
    {
        if (!_isLoading) return;

        progress = Mathf.Clamp01(progress);

        if (progressBar != null)
            progressBar.value = progress;

        if (progressText != null)
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    /// <summary>
    /// Ẩn loading panel (có đảm bảo thời gian tối thiểu)
    /// </summary>
    public void HideLoading()
    {
        if (!_isLoading) return;

        // Đảm bảo thời gian tối thiểu
        float elapsed = Time.time - _loadStartTime;
        if (elapsed < minLoadTime)
        {
            StartCoroutine(HideAfterDelay(minLoadTime - elapsed));
            return;
        }

        _isLoading = false;
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        Debug.Log("[LoadingUIManager] HideLoading");
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLoading();
    }

    /// <summary>
    /// Reset loading về trạng thái ban đầu
    /// </summary>
    public void ResetLoading()
    {
        _isLoading = false;
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        if (progressBar != null)
            progressBar.value = 0f;
        if (progressText != null)
            progressText.text = "0%";
        Debug.Log("[LoadingUIManager] ResetLoading");
    }

    /// <summary>
    /// Kiểm tra loading đang hiển thị không
    /// </summary>
    public bool IsLoading() => _isLoading;
}