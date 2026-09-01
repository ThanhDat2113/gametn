// ============================================
// OuttroCreditManager.cs - Đơn giản, dùng Image
// ============================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class OuttroCreditManager : MonoBehaviour
{
    [Header("=== THÀNH PHẦN UI ===")]
    public CanvasGroup mainCanvas;
    public RectTransform creditContainer;
    public TextMeshProUGUI creditText;
    public TextMeshProUGUI endingText;
    public Image blackOverlay;
    
    [Header("=== LOGO TEAM ===")]
    public Image logoImage;                // Image logo (tắt ở Hierarchy)
    public float logoDisplayTime = 3f;
    
    [Header("=== CÀI ĐẶT ===")]
    public float scrollSpeed = 40f;
    public float fadeDuration = 1.5f;
    
    [Header("=== AUDIO ===")]
    public AudioSource bgmSource;
    public AudioClip bgmCredit;
    
    private bool isScrolling = false;
    private bool hasEnded = false;
    private Vector2 startPos;
    
    void Start()
    {
        startPos = creditContainer.anchoredPosition;
        
        mainCanvas.alpha = 0;
        creditContainer.gameObject.SetActive(false);
        endingText.gameObject.SetActive(false);
        blackOverlay.color = new Color(0, 0, 0, 1);
        
        // Tắt logo
        if (logoImage != null)
        {
            logoImage.gameObject.SetActive(false);
        }
        
        StartCoroutine(PlaySequence());
    }
    
    void Update()
    {
        if (isScrolling)
        {
            Vector2 pos = creditContainer.anchoredPosition;
            pos.y += scrollSpeed * Time.deltaTime;
            creditContainer.anchoredPosition = pos;
        }
        
        if (Input.GetKeyDown(KeyCode.Space) && !hasEnded)
        {
            SkipCredit();
        }
    }
    
    IEnumerator PlaySequence()
    {
        // === 1. ENDING TEXT ===
        endingText.gameObject.SetActive(true);
        endingText.alpha = 1;
        yield return new WaitForSeconds(2f);
        
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 0.3f;
            endingText.alpha = 1 - t;
            yield return null;
        }
        endingText.alpha = 0;
        endingText.gameObject.SetActive(false);
        
        // === 2. FADE OUT BLACK ===
        yield return FadeImage(blackOverlay, 1, 0, fadeDuration);
        
        // === 3. HIỆN CREDIT ===
        creditContainer.gameObject.SetActive(true);
        creditContainer.anchoredPosition = startPos;
        mainCanvas.alpha = 1;
        
        if (bgmSource != null && bgmCredit != null)
        {
            bgmSource.clip = bgmCredit;
            bgmSource.Play();
        }
        
        // === 4. CUỘN CREDIT ===
        isScrolling = true;
        yield return StartCoroutine(WaitForCreditComplete());
        
        isScrolling = false;
        
        // === 5. FADE OUT CREDIT ===
        yield return FadeCanvas(mainCanvas, 1, 0, fadeDuration);
        
        // === 6. HIỆN LOGO ===
        yield return StartCoroutine(ShowLogo());
        
        // === 7. KẾT THÚC ===
        hasEnded = true;
        yield return new WaitForSeconds(1f);
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // ============================================
    // HIỂN THỊ LOGO
    // ============================================
    IEnumerator ShowLogo()
    {
        if (logoImage == null)
        {
            Debug.LogWarning("⚠️ Logo chưa được gán!");
            yield break;
        }
        
        // Bật logo
        logoImage.gameObject.SetActive(true);
        
        // Fade in
        Color c = logoImage.color;
        c.a = 0;
        logoImage.color = c;
        
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 1f;
            c.a = t;
            logoImage.color = c;
            yield return null;
        }
        c.a = 1;
        logoImage.color = c;
        
        // Giữ logo
        yield return new WaitForSeconds(logoDisplayTime);
        
        // Fade out
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 0.5f;
            c.a = 1 - t;
            logoImage.color = c;
            yield return null;
        }
        c.a = 0;
        logoImage.color = c;
    }
    
    // ============================================
    // HÀM ĐỢI CREDIT
    // ============================================
    IEnumerator WaitForCreditComplete()
    {
        Canvas.ForceUpdateCanvases();
        
        float textHeight = creditText.preferredHeight;
        float containerHeight = creditContainer.rect.height;
        float totalDistance = textHeight + containerHeight + 200f;
        float timeNeeded = totalDistance / scrollSpeed;
        float waitTime = timeNeeded + 3f;
        
        yield return new WaitForSeconds(waitTime);
    }
    
    // ============================================
    // SKIP
    // ============================================
    void SkipCredit()
    {
        if (hasEnded) return;
        
        isScrolling = false;
        hasEnded = true;
        
        StopAllCoroutines();
        StartCoroutine(SkipSequence());
    }
    
    IEnumerator SkipSequence()
    {
        yield return FadeCanvas(mainCanvas, 1, 0, 0.5f);
        yield return new WaitForSeconds(0.3f);
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // ============================================
    // HÀM FADE
    // ============================================
    IEnumerator FadeCanvas(CanvasGroup target, float from, float to, float duration)
    {
        target.alpha = from;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            target.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        target.alpha = to;
    }
    
    IEnumerator FadeImage(Image target, float from, float to, float duration)
    {
        Color c = target.color;
        c.a = from;
        target.color = c;
        
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            c.a = Mathf.Lerp(from, to, t);
            target.color = c;
            yield return null;
        }
        c.a = to;
        target.color = c;
    }
}