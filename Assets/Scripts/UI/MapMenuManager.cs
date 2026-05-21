using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // thêm để load scene

public class MapMenuManager : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject mainPanel;
    [Header("Sub Panels")]
    public GameObject characterPanel;
    public GameObject formationPanel;
    public GameObject inventoryPanel;
    public GameObject savePanel;
    public GameObject loadPanel;
    public GameObject quitPanel;  // Panel nhỏ xác nhận thoát

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

    [Header("Sub Panel Managers")]
    public CharacterPanelManager characterPanelManager;

    private RectTransform mainRect;
    private CanvasGroup mainCG;
    private AudioSource audioSource;

    private enum MenuState { Closed, Main, Character, Formation, Inventory, Save, Load, Quit }
    private MenuState currentState = MenuState.Closed;
    private Coroutine currentAnim;

    void Awake()
    {
        if (characterPanel != null) characterPanel.SetActive(false);
        if (formationPanel != null) formationPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (savePanel != null) savePanel.SetActive(false);
        if (loadPanel != null) loadPanel.SetActive(false);
        if (quitPanel != null) quitPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
    }

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
        savePanel.SetActive(false);
        loadPanel.SetActive(false);
        quitPanel.SetActive(false);
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
                if (characterPanelManager != null && characterPanelManager.TryGoBack())
                    return;
                else
                    GoBackToMainWithoutEffect();
                break;
            case MenuState.Quit:
                CloseQuitPanel(); // Chỉ đóng quit panel, không ẩn main
                break;
            default:
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
        CloseCurrentSubPanel(); // sẽ đóng các panel con bình thường (trừ quit)
        mainPanel.SetActive(true);
        mainRect.localScale = Vector3.one;
        if (useFade && mainCG != null) mainCG.alpha = 1f;
        currentState = MenuState.Main;
    }

    void CloseCurrentSubPanel()
    {
        switch (currentState)
        {
            case MenuState.Character: characterPanel.SetActive(false); break;
            case MenuState.Formation: formationPanel.SetActive(false); break;
            case MenuState.Inventory: inventoryPanel.SetActive(false); break;
            case MenuState.Save: savePanel.SetActive(false); break;
            case MenuState.Load: loadPanel.SetActive(false); break;
            // Không đóng quit panel ở đây vì nó được xử lý riêng
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
        Canvas.ForceUpdateCanvases();
        if (panel.GetComponent<RectTransform>() != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
    }

    // Quit panel đặc biệt: không ẩn main panel
    void OpenQuitPanel()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(true);
            currentState = MenuState.Quit;
        }
    }

    void CloseQuitPanel()
    {
        if (quitPanel != null)
            quitPanel.SetActive(false);
        currentState = MenuState.Main;
    }

    // Hàm gọi khi chọn Yes (xác nhận quit)
    public void QuitToMainMenu()
    {
        // Load scene main menu (tên scene của bạn)
        SceneManager.LoadScene("Main Menu");
    }

    // Hàm gọi khi chọn No (hủy)
    public void CancelQuit()
    {
        CloseQuitPanel();
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

    // Các hàm public để gọi từ button
    public void OpenCharacterPanel() => OpenSubPanel(characterPanel, MenuState.Character);
    public void OpenFormationPanel() => OpenSubPanel(formationPanel, MenuState.Formation);
    public void OpenInventoryPanel() => OpenSubPanel(inventoryPanel, MenuState.Inventory);
    public void OpenSavePanel() => OpenSubPanel(savePanel, MenuState.Save);
    public void OpenLoadPanel() => OpenSubPanel(loadPanel, MenuState.Load);
    public void OpenQuit() => OpenQuitPanel(); // gán cho nút Quit
}