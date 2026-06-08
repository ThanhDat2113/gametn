using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MapMenuManager : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject mainPanel;

    [Header("Sub Panels")]
    public GameObject characterPanel;
    public GameObject formationPanel;
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    public GameObject savePanel;
    public GameObject loadPanel;
    public GameObject quitPanel;

    [Header("Character Container trên Main Panel")]
    public Transform characterContainer;
    public GameObject characterSlotPrefab;

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
    public EquipmentPanel equipmentPanelManager;

    private RectTransform mainRect;
    private CanvasGroup mainCG;
    private AudioSource audioSource;

    private enum MenuState { Closed, Main, Character, Formation, Inventory, Equipment, Save, Load, Quit }
    private MenuState currentState = MenuState.Closed;
    private Coroutine currentAnim;

    private List<CharacterSlotUI> characterSlots = new List<CharacterSlotUI>();

    void Awake()
    {
        if (characterPanel != null) characterPanel.SetActive(false);
        if (formationPanel != null) formationPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
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
        equipmentPanel.SetActive(false);
        savePanel.SetActive(false);
        loadPanel.SetActive(false);
        quitPanel.SetActive(false);

        var formationMgr = FindFirstObjectByType<FormationManager>();
        if (formationMgr != null)
        {
            formationMgr.OnFormationChanged += RefreshCharacterContainer;
        }
    }

    private void OnDestroy()
    {
        var formationMgr = FindFirstObjectByType<FormationManager>();
        if (formationMgr != null)
        {
            formationMgr.OnFormationChanged -= RefreshCharacterContainer;
        }
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
            case MenuState.Equipment:
                if (equipmentPanelManager != null && equipmentPanelManager.TryGoBack())
                    return;
                else
                    GoBackToMainWithoutEffect();
                break;
            case MenuState.Quit:
                CloseQuitPanel();
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
        RefreshCharacterContainer();
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
        RefreshCharacterContainer();
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
            case MenuState.Equipment: equipmentPanel.SetActive(false); break;
            case MenuState.Save: savePanel.SetActive(false); break;
            case MenuState.Load: loadPanel.SetActive(false); break;
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

    public void QuitToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

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

    // Public methods for buttons
    public void OpenCharacterPanel() => OpenSubPanel(characterPanel, MenuState.Character);
    public void OpenFormationPanel() => OpenSubPanel(formationPanel, MenuState.Formation);
    public void OpenInventoryPanel() => OpenSubPanel(inventoryPanel, MenuState.Inventory);
    public void OpenEquipmentPanel() => OpenSubPanel(equipmentPanel, MenuState.Equipment);
    public void OpenSavePanel() => OpenSubPanel(savePanel, MenuState.Save);
    public void OpenLoadPanel() => OpenSubPanel(loadPanel, MenuState.Load);
    public void OpenQuit() => OpenQuitPanel();

    // ─── Character Container ─────────────────────────────────
    private void RefreshCharacterContainer()
    {
        foreach (var slot in characterSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        characterSlots.Clear();

        if (characterContainer == null || characterSlotPrefab == null)
        {
            Debug.LogWarning("[MapMenuManager] characterContainer hoặc characterSlotPrefab chưa được gán!");
            return;
        }

        var formationMgr = FindFirstObjectByType<FormationManager>();
        if (formationMgr == null) return;

        var formationData = formationMgr.GetCurrentFormationData();
        if (formationData == null || formationData.slots == null) return;

        var activeSlots = new List<(int gridSlot, FormationSlot slot)>();
        for (int i = 0; i < formationData.slots.Length; i++)
        {
            if (formationData.slots[i] != null && formationData.slots[i].data != null)
            {
                activeSlots.Add((i, formationData.slots[i]));
            }
        }
        activeSlots.Sort((a, b) => a.gridSlot.CompareTo(b.gridSlot));

        for (int idx = 0; idx < activeSlots.Count; idx++)
        {
            var slotData = activeSlots[idx];
            var character = slotData.slot.data;

            // Lấy dữ liệu từ PlayerProgression
            int level = 1;
            float expProgress = 0f;
            int currentExp = 0;
            int neededExp = 100;

            if (PlayerProgression.Instance != null)
            {
                level = PlayerProgression.Instance.GetLevel(character);
                expProgress = PlayerProgression.Instance.GetLevelProgress(character);
                currentExp = PlayerProgression.Instance.GetCurrentExp(character);
                neededExp = PlayerProgression.Instance.GetExpToNextLevel(character);
            }
            else
            {
                // Fallback
                level = slotData.slot.level;
                neededExp = 100;
            }

            GameObject slotGO = Instantiate(characterSlotPrefab, characterContainer);
            var slotUI = slotGO.GetComponent<CharacterSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(character, level, idx + 1, expProgress, currentExp, neededExp);
                characterSlots.Add(slotUI);
            }
            else
            {
                Debug.LogError("CharacterSlotPrefab thiếu component CharacterSlotUI!");
                Destroy(slotGO);
            }
        }
    }
}