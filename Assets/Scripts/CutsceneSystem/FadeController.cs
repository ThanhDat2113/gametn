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

        if (fadeImage == null) Debug.LogError("FadeController: fadeImage chưa được gán!");
        else
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    public void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    public IEnumerator FadeToBlack(System.Action onComplete = null)
    {
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
        onComplete?.Invoke();
    }
}