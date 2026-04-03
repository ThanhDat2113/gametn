using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class DialogueBoxUI : MonoBehaviour
{
    [Header("References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public Image backgroundImage;
    public TextMeshProUGUI continueIndicator;
    
    [Header("Portrait Slots")]
    public Image portraitLeft;
    public Image portraitCenter;
    public Image portraitRight;
    
    [Header("Portrait Settings")]
    public float fixedHeight = 180f;
    public float bottomOffset = 20f;
    public Vector2 leftPortraitPos = new Vector2(-400, 0);
    public Vector2 centerPortraitPos = new Vector2(0, 0);
    public Vector2 rightPortraitPos = new Vector2(400, 0);
    
    [Header("Typing Effect")]
    public float defaultTextSpeed = 0.05f;
    public AudioClip typingSound;
    public AudioSource audioSource;
    
    [Header("Animation")]
    public float fadeDuration = 0.2f;
    
    [Header("Input Settings")]
    public bool useLeftMouse = true;
    public bool useSpace = true;
    public bool useEnter = true;
    
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string fullText;
    private Action onLineComplete;
    private bool isWaitingForInput = false;
    
    void Awake()
    {
        SetupPortraitSlot(portraitLeft, leftPortraitPos);
        SetupPortraitSlot(portraitCenter, centerPortraitPos);
        SetupPortraitSlot(portraitRight, rightPortraitPos);
        
        if (continueIndicator != null)
            continueIndicator.gameObject.SetActive(false);
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
    
    private void SetupPortraitSlot(Image portrait, Vector2 position)
    {
        if (portrait == null) return;
        
        RectTransform rect = portrait.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(position.x, bottomOffset);
            rect.sizeDelta = new Vector2(fixedHeight, fixedHeight);
        }
        
        portrait.preserveAspect = false;
        portrait.color = Color.white;
        portrait.gameObject.SetActive(false);
    }
    
    private void ResizePortraitToFit(Image portrait, Sprite sprite)
    {
        if (portrait == null || sprite == null) return;
        
        RectTransform rect = portrait.GetComponent<RectTransform>();
        if (rect == null) return;
        
        float aspectRatio = sprite.rect.width / sprite.rect.height;
        float newWidth = fixedHeight * aspectRatio;
        rect.sizeDelta = new Vector2(newWidth, fixedHeight);
    }
    
    void Update()
    {
        if (!dialoguePanel.activeSelf) return;
        
        if (CanContinue())
        {
            OnContinueClick();
        }
    }
    
    private bool CanContinue()
    {
        if (!isWaitingForInput) return false;
        
        if (useLeftMouse && Input.GetMouseButtonDown(0))
            return true;
        
        if (useSpace && Input.GetKeyDown(KeyCode.Space))
            return true;
        
        if (useEnter && Input.GetKeyDown(KeyCode.Return))
            return true;
        
        return false;
    }
    
    public void Show()
    {
        dialoguePanel.SetActive(true);
        StartCoroutine(FadeIn());
    }
    
    public void Hide()
    {
        StartCoroutine(FadeOut(() => dialoguePanel.SetActive(false)));
    }
    
    public void DisplayLine(DialogueLine line, Action onComplete)
    {
        onLineComplete = onComplete;
        isWaitingForInput = false;
        
        if (continueIndicator != null)
            continueIndicator.gameObject.SetActive(false);
        
        UpdatePortraits(line);
        
        if (nameText != null && line.character != null)
        {
            nameText.text = line.character.characterName;
            nameText.color = line.character.nameColor;
        }
        
        if (backgroundImage != null && line.backgroundSprite != null)
        {
            backgroundImage.sprite = line.backgroundSprite;
        }
        
        if (line.voiceClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(line.voiceClip);
        }
        
        fullText = line.text;
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line.text, line.textSpeed, line.textSound));
    }
    
    private void UpdatePortraits(DialogueLine line)
    {
        // Ẩn tất cả
        if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
        if (portraitCenter != null) portraitCenter.gameObject.SetActive(false);
        if (portraitRight != null) portraitRight.gameObject.SetActive(false);
        
        // Nhân vật chính
        if (line.character != null)
        {
            string emotion = string.IsNullOrEmpty(line.emotionKey) ? "normal" : line.emotionKey;
            Sprite mainSprite = line.character.GetPortrait(emotion);
            
            if (mainSprite != null)
            {
                Image targetImage = GetPortraitImage(line.position);
                if (targetImage != null)
                {
                    ResizePortraitToFit(targetImage, mainSprite);
                    targetImage.sprite = mainSprite;
                    targetImage.gameObject.SetActive(true);
                }
            }
        }
        
        // Nhân vật phụ
        if (line.otherCharacters != null)
        {
            foreach (var other in line.otherCharacters)
            {
                if (other.character != null)
                {
                    string emotion = string.IsNullOrEmpty(other.emotionKey) ? "normal" : other.emotionKey;
                    Sprite otherSprite = other.character.GetPortrait(emotion);
                    
                    if (otherSprite != null)
                    {
                        Image targetImage = GetPortraitImage(other.position);
                        if (targetImage != null)
                        {
                            ResizePortraitToFit(targetImage, otherSprite);
                            targetImage.sprite = otherSprite;
                            targetImage.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
    }
    
    private Image GetPortraitImage(CharacterPosition position)
    {
        switch (position)
        {
            case CharacterPosition.Left: return portraitLeft;
            case CharacterPosition.Center: return portraitCenter;
            case CharacterPosition.Right: return portraitRight;
            default: return portraitCenter;
        }
    }
    
    private IEnumerator TypeText(string text, float speed, AudioClip textSound)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            
            if (textSound != null && audioSource != null && c != ' ' && c != '\n')
                audioSource.PlayOneShot(textSound);
            
            yield return new WaitForSeconds(speed);
        }
        
        isTyping = false;
        typingCoroutine = null;
        isWaitingForInput = true;
        
        if (continueIndicator != null)
        {
            continueIndicator.gameObject.SetActive(true);
        }
    }
    
    private void OnContinueClick()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            dialogueText.text = fullText;
            isTyping = false;
            isWaitingForInput = true;
            
            if (continueIndicator != null)
                continueIndicator.gameObject.SetActive(true);
        }
        else if (isWaitingForInput)
        {
            isWaitingForInput = false;
            if (continueIndicator != null)
                continueIndicator.gameObject.SetActive(false);
            onLineComplete?.Invoke();
        }
    }
    
    private IEnumerator FadeIn()
    {
        CanvasGroup cg = dialoguePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = dialoguePanel.AddComponent<CanvasGroup>();
        cg.alpha = 0;
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = 1;
    }
    
    private IEnumerator FadeOut(Action onComplete)
    {
        CanvasGroup cg = dialoguePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = dialoguePanel.AddComponent<CanvasGroup>();
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = 0;
        onComplete?.Invoke();
    }
}