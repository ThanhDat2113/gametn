// ============================================
// OuttroCreditManager.cs - FIXED (Ending text nhanh, mượt)
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
    
    [Header("=== CÀI ĐẶT ===")]
    public float scrollSpeed = 40f;
    public float fadeDuration = 1.5f;
    public float endDelay = 2f;
    
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
        // === 1. ENDING TEXT - HIỆN NGAY LẬP TỨC ===
        endingText.gameObject.SetActive(true);
        endingText.alpha = 1;  // HIỆN LUÔN, KHÔNG FADE IN
        
        // Giữ nguyên 2 giây
        yield return new WaitForSeconds(2f);
        
        // === 2. FADE OUT ENDING TEXT (NHANH) ===
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / 0.3f;  // 0.3 giây là xong
            endingText.alpha = 1 - t;
            yield return null;
        }
        endingText.alpha = 0;
        endingText.gameObject.SetActive(false);
        
        // === 3. FADE OUT BLACK OVERLAY ===
        yield return FadeImage(blackOverlay, 1, 0, fadeDuration);
        
        // === 4. HIỆN CREDIT ===
        creditContainer.gameObject.SetActive(true);
        creditContainer.anchoredPosition = startPos;
        mainCanvas.alpha = 1;
        
        if (bgmSource != null && bgmCredit != null)
        {
            bgmSource.clip = bgmCredit;
            bgmSource.Play();
        }
        
        // === 5. CUỘN CREDIT ===
        isScrolling = true;
        yield return StartCoroutine(WaitForCreditComplete());
        
        isScrolling = false;
        hasEnded = true;
        
        // === 6. FADE OUT VÀ VỀ MENU ===
        yield return FadeCanvas(mainCanvas, 1, 0, fadeDuration);
        yield return new WaitForSeconds(endDelay);
        
        SceneManager.LoadScene("MainMenu");
    }
    
    IEnumerator WaitForCreditComplete()
    {
        Canvas.ForceUpdateCanvases();
        
        float textHeight = creditText.preferredHeight;
        float containerHeight = creditContainer.rect.height;
        float totalDistance = textHeight + containerHeight + 200f;
        float timeNeeded = totalDistance / scrollSpeed;
        float waitTime = timeNeeded + 3f;
        
        Debug.Log($"📊 Credit Height: {textHeight}, Container: {containerHeight}");
        Debug.Log($"⏱️ Cần chờ: {waitTime} giây");
        
        yield return new WaitForSeconds(waitTime);
    }
    
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
        SceneManager.LoadScene("MainMenu");
    }
    
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