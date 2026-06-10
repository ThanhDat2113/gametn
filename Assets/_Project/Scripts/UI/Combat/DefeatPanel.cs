using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DefeatPanel : MonoBehaviour
{
    [Header("UI References")]
    public Button retryButton;
    public Button quitButton;
    public TextMeshProUGUI defeatText;

    [Header("Fade Settings")]
    public CanvasGroup panelCanvasGroup;
    public float fadeDuration = 0.3f;

    private bool isWaitingForInput = false;

    private void Awake()
    {
        gameObject.SetActive(false);
        if (panelCanvasGroup == null)
            panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null && GetComponent<Canvas>() != null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Tìm button theo tên nếu chưa gán
        if (retryButton == null)
            retryButton = FindButtonByName(new string[] { "RetryButton", "ChoiLai" });
        if (quitButton == null)
            quitButton = FindButtonByName(new string[] { "QuitButton", "Thoat" });

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
        else
            Debug.LogError("DefeatPanel: Không tìm thấy RetryButton! Hãy gán hoặc đặt tên button là 'RetryButton'.");

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        else
            Debug.LogError("DefeatPanel: Không tìm thấy QuitButton! Hãy gán hoặc đặt tên button là 'QuitButton'.");
    }

    private Button FindButtonByName(string[] names)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            foreach (string name in names)
            {
                if (btn.name == name)
                    return btn;
            }
        }
        return null;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            StartCoroutine(FadeIn());
        }
        else
        {
            isWaitingForInput = true;
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;
        isWaitingForInput = true;
    }

    private void OnRetryClicked()
    {
        if (!isWaitingForInput) return;
        isWaitingForInput = false;
        Debug.Log("[DefeatPanel] Retry clicked. Reloading combat scene.");
        SceneLoaderManager.ReloadCombatScene();
        Destroy(gameObject);
    }

    private void OnQuitClicked()
    {
        if (!isWaitingForInput) return;
        isWaitingForInput = false;
        Debug.Log("[DefeatPanel] Quit clicked. Returning to map.");
        SceneLoaderManager.UnloadCombatScene();
        Destroy(gameObject);
    }
}