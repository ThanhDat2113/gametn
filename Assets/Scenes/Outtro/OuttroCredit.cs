// ============================================
// OuttroCreditManager.cs - FIXED (chạy hết credit)
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
    public RectTransform creditContainer;   // Scroll View
    public TextMeshProUGUI creditText;      // Text bên trong Content
    public TextMeshProUGUI endingText;
    public Image blackOverlay;
    
    [Header("=== CÀI ĐẶT ===")]
    public float scrollSpeed = 30f;         // GIẢM TỐC ĐỘ
    public float fadeDuration = 1.5f;
    public float endDelay = 3f;
    public float extraWaitTime = 5f;        // THÊM THỜI GIAN CHỜ DƯ
    
    [Header("=== AUDIO ===")]
    public AudioSource bgmSource;
    public AudioClip bgmCredit;
    
    private bool isScrolling = false;
    private Vector2 startPos;
    
    void Start()
    {
        // Lưu vị trí ban đầu của Content (KHÔNG phải container)
        startPos = creditContainer.anchoredPosition;
        
        mainCanvas.alpha = 0;
        blackOverlay.color = new Color(0, 0, 0, 1);
        endingText.gameObject.SetActive(true);
        endingText.alpha = 0;
        
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
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
    
    IEnumerator PlaySequence()
    {
        // === 1. GIỮ MÀN ĐEN ===
        yield return new WaitForSeconds(0.5f);
        
        // === 2. HIỆN ENDING TEXT ===
        if (endingText != null)
        {
            endingText.gameObject.SetActive(true);
            endingText.alpha = 0;
            
            string[] lines = endingText.text.Split('\n');
            endingText.text = "";
            
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line.Trim()))
                {
                    endingText.text += line + "\n";
                    yield return new WaitForSeconds(0.6f);
                }
            }
            
            yield return new WaitForSeconds(2f);
            
            // Fade out
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / 1f;
                endingText.alpha = 1 - t;
                yield return null;
            }
            endingText.gameObject.SetActive(false);
        }
        
        // === 3. MỞ MÀN ===
        yield return FadeImage(blackOverlay, 1, 0, fadeDuration);
        
        // === 4. HIỆN CREDIT ===
        // Reset vị trí
        creditContainer.anchoredPosition = startPos;
        mainCanvas.alpha = 1;
        
        if (bgmSource != null && bgmCredit != null)
        {
            bgmSource.clip = bgmCredit;
            bgmSource.Play();
        }
        
        // === 5. CUỘN CREDIT - CHỜ CHO ĐẾN KHI HẾT ===
        isScrolling = true;
        
        // ĐỢI CREDIT CHẠY HẾT (QUAN TRỌNG!)
        yield return StartCoroutine(WaitForCreditComplete());
        
        isScrolling = false;
        
        // === 6. FADE OUT VÀ VỀ MENU ===
        yield return FadeCanvas(mainCanvas, 1, 0, fadeDuration);
        yield return new WaitForSeconds(endDelay);
        
        SceneManager.LoadScene("MainMenu");
    }
    
    // ============================================
    // HÀM ĐỢI CREDIT CHẠY HẾT (CỐT LÕI)
    // ============================================
    IEnumerator WaitForCreditComplete()
    {
        // Lấy chiều cao THỰC TẾ của text
        float textHeight = creditText.preferredHeight;
        
        // Lấy chiều cao của container (viewport)
        float containerHeight = creditContainer.rect.height;
        
        // Tính tổng quãng đường cần cuộn
        // textHeight + containerHeight = cuộn từ dưới lên trên hết
        float totalDistance = textHeight + containerHeight + 200; // thêm 200 để dư
        
        // Tính thời gian cần
        float timeNeeded = totalDistance / scrollSpeed;
        
        // Cộng thêm thời gian dư
        float waitTime = timeNeeded + extraWaitTime;
        
        Debug.Log($"📊 Credit Height: {textHeight}, Container: {containerHeight}");
        Debug.Log($"⏱️ Cần chờ: {waitTime} giây");
        
        yield return new WaitForSeconds(waitTime);
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