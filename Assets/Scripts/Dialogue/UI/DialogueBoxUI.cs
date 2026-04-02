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
    
    [Header("Portrait Slots - Kéo trực tiếp 3 Image vào")]
    public Image portraitLeft;
    public Image portraitCenter;
    public Image portraitRight;
    
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
        // Ẩn tất cả portrait ban đầu
        if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
        if (portraitCenter != null) portraitCenter.gameObject.SetActive(false);
        if (portraitRight != null) portraitRight.gameObject.SetActive(false);
        
        if (continueIndicator != null)
        {
            continueIndicator.gameObject.SetActive(false);
        }
        
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
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
        
        // Cập nhật portrait
        UpdatePortraits(line);
        
        // Set name
        if (nameText != null && line.character != null)
        {
            nameText.text = line.character.characterName;
            nameText.color = line.character.nameColor;
        }
        
        // Set background
        if (backgroundImage != null && line.backgroundSprite != null)
        {
            backgroundImage.sprite = line.backgroundSprite;
        }
        
        // Audio
        if (line.voiceClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(line.voiceClip);
        }
        
        // Typing
        fullText = line.text;
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(line.text, line.textSpeed, line.textSound));
    }
    
    private void UpdatePortraits(DialogueLine line)
    {
        // Ẩn tất cả trước
        if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
        if (portraitCenter != null) portraitCenter.gameObject.SetActive(false);
        if (portraitRight != null) portraitRight.gameObject.SetActive(false);
        
        // Lấy sprite cho nhân vật chính
        if (line.character != null)
        {
            Sprite mainSprite = line.character.GetPortrait(line.emotion);
            if (mainSprite != null)
            {
                Image targetImage = GetPortraitImage(line.position);
                if (targetImage != null)
                {
                    targetImage.sprite = mainSprite;
                    targetImage.gameObject.SetActive(true);
                }
            }
        }
        
        // Lấy sprite cho các nhân vật phụ
        if (line.otherCharacters != null)
        {
            foreach (var other in line.otherCharacters)
            {
                if (other.character != null)
                {
                    Sprite otherSprite = other.character.GetPortrait(other.emotion);
                    if (otherSprite != null)
                    {
                        Image targetImage = GetPortraitImage(other.position);
                        if (targetImage != null)
                        {
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
            case CharacterPosition.Left:
                return portraitLeft;
            case CharacterPosition.Center:
                return portraitCenter;
            case CharacterPosition.Right:
                return portraitRight;
            default:
                return portraitCenter;
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