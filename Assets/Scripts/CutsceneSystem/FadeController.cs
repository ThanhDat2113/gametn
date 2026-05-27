using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [Header("UI")]
    public Image fadeImage;

    [Header("Settings")]
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        if (fadeImage == null)
        {
            Debug.LogError("FadeController: fadeImage chưa được gán!");
            return;
        }
        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.raycastTarget = false; // Không chặn click khi màn hình trong suốt
    }

    public void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
        // Chỉ chặn click khi màn hình có độ mờ > 0 (đang fade)
        fadeImage.raycastTarget = alpha > 0.01f;
    }

    public IEnumerator FadeToBlack(System.Action onComplete = null)
    {
        fadeImage.raycastTarget = true; // Bắt đầu chặn click
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            SetAlpha(Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
        SetAlpha(1f);
        onComplete?.Invoke();
    }

    public IEnumerator FadeFromBlack(System.Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            SetAlpha(Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
        SetAlpha(0f);
        fadeImage.raycastTarget = false; // Kết thúc, không chặn click nữa
        onComplete?.Invoke();
    }
}