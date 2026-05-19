using UnityEngine;
using System.Collections;

public class MapMenuManager : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject mainPanel;
    [Header("Sub Panels")]
    public GameObject characterPanel;
    public GameObject formationPanel;
    public GameObject inventoryPanel;

    [Header("Animation Settings (chỉ cho main panel)")]
    public float animationDuration = 0.5f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float startScale = 2.5f;
    public bool useFade = true;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    [Range(0f, 1f)]
    public float volume = 0.7f;

    [Header("Sub Panel Managers (kéo vào)")]
    public CharacterPanelManager characterPanelManager; // Gán trong Inspector

    private RectTransform mainRect;
    private CanvasGroup mainCG;
    private AudioSource audioSource;

    private enum MenuState { Closed, Main, Character, Formation, Inventory }
    private MenuState currentState = MenuState.Closed;
    private Coroutine currentAnim;

    void Start()
    {
        mainRect = mainPanel.GetComponent<RectTransform>();
        if (useFade)
        {
            mainCG = mainPanel.GetComponent<CanvasGroup>();
            if (mainCG == null) mainCG = mainPanel.AddComponent<CanvasGroup>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        mainRect.localScale = Vector3.one * startScale;
        if (useFade && mainCG != null) mainCG.alpha = 0f;

        mainPanel.SetActive(false);
        characterPanel.SetActive(false);
        formationPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        
        // Đảm bảo characterPanel tắt (dự phòng)
        if (characterPanel != null) characterPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            HandleBackAction();
        }
    }

    void HandleBackAction()
    {
        switch (currentState)
        {
            case MenuState.Closed:
                PlaySound(openSound);
                OpenMainPanelWithEffect();
                break;
            case MenuState.Main:
                PlaySound(closeSound);
                CloseMainPanelWithEffect();
                break;
            case MenuState.Character:
                // Ưu tiên cho CharacterPanelManager xử lý
                if (characterPanelManager != null && characterPanelManager.TryGoBack())
                    return; // Đã xử lý xong (đóng info panel), không làm gì thêm
                else
                    GoBackToMainWithoutEffect(); // Quay về main panel
                break;
            default: // Formation, Inventory (có thể thêm tương tự sau)
                GoBackToMainWithoutEffect();
                break;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }

    void OpenMainPanelWithEffect()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        mainPanel.SetActive(true);
        mainRect.localScale = Vector3.one * startScale;
        if (useFade && mainCG != null) mainCG.alpha = 0f;
        currentAnim = StartCoroutine(AnimateMainPanel(startScale, 1f, 0f, 1f, false));
        currentState = MenuState.Main;
    }

    void CloseMainPanelWithEffect()
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateMainPanel(1f, startScale, 1f, 0f, true));
    }

    void GoBackToMainWithoutEffect()
    {
        CloseCurrentSubPanel();
        mainPanel.SetActive(true);
        mainRect.localScale = Vector3.one;
        if (useFade && mainCG != null) mainCG.alpha = 1f;
        currentState = MenuState.Main;
    }

    void CloseCurrentSubPanel()
    {
        switch (currentState)
        {
            case MenuState.Character:
                characterPanel.SetActive(false);
                break;
            case MenuState.Formation:
                formationPanel.SetActive(false);
                break;
            case MenuState.Inventory:
                inventoryPanel.SetActive(false);
                break;
        }
    }

    void OpenSubPanel(GameObject panel, MenuState nextState)
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        if (currentState == MenuState.Main)
            mainPanel.SetActive(false);
        else
            CloseCurrentSubPanel();
        panel.SetActive(true);
        currentState = nextState;
    }

    IEnumerator AnimateMainPanel(float startScaleVal, float endScaleVal, float startAlpha, float endAlpha, bool deactivateOnEnd)
    {
        float elapsed = 0f;
        Vector3 startScaleVec = Vector3.one * startScaleVal;
        Vector3 endScaleVec = Vector3.one * endScaleVal;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float curveT = zoomCurve.Evaluate(t);
            mainRect.localScale = Vector3.Lerp(startScaleVec, endScaleVec, curveT);
            if (useFade && mainCG != null)
                mainCG.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        mainRect.localScale = endScaleVec;
        if (useFade && mainCG != null) mainCG.alpha = endAlpha;
        if (deactivateOnEnd)
        {
            mainPanel.SetActive(false);
            currentState = MenuState.Closed;
        }
        currentAnim = null;
    }

    public void OpenCharacterPanel()
    {
        OpenSubPanel(characterPanel, MenuState.Character);
    }

    public void OpenFormationPanel()
    {
        OpenSubPanel(formationPanel, MenuState.Formation);
    }

    public void OpenInventoryPanel()
    {
        OpenSubPanel(inventoryPanel, MenuState.Inventory);
    }
}