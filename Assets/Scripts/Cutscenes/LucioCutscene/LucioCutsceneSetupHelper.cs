using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to quickly setup the Lucio cutscene in a scene
/// Optionally auto-creates all necessary GameObjects if they don't exist
/// </summary>
public class LucioCutsceneSetupHelper : MonoBehaviour
{
    [Header("Auto-Setup Options")]
    [SerializeField] private bool autoCreateCharacters = true;
    [SerializeField] private bool autoCreateCameraController = true;
    [SerializeField] private bool autoCreateBeamSystem = true;
    [SerializeField] private bool autoCreateQTEUI = true;

    [ContextMenu("Setup Lucio Cutscene")]
    public void SetupCutscene()
    {
        Debug.Log("Setting up Lucio Cutscene...");

        if (autoCreateCharacters)
            CreateCharacters();

        if (autoCreateCameraController)
            SetupCameraController();

        if (autoCreateBeamSystem)
            CreateBeamSystem();

        if (autoCreateQTEUI)
            CreateQTEUI();

        SetupCutsceneManager();

        Debug.Log("Lucio Cutscene setup complete! Configure references in Inspector as needed.");
    }

    private void CreateCharacters()
    {
        // Create Lucio (Blue cube on left)
        GameObject lucioObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lucioObj.name = "Lucio";
        lucioObj.transform.position = new Vector3(-3, 0, 0);
        lucioObj.transform.localScale = new Vector3(1, 1, 0.1f);
        Destroy(lucioObj.GetComponent<BoxCollider>());
        Destroy(lucioObj.GetComponent<Rigidbody>());

        SpriteRenderer lucioSprite = lucioObj.GetComponent<SpriteRenderer>();
        CutsceneCharacter lucioChar = lucioObj.AddComponent<CutsceneCharacter>();
        lucioObj.GetComponent<Renderer>().material.color = Color.blue;

        // Create Cedric (Red cube on right)
        GameObject cedricObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cedricObj.name = "Cedric";
        cedricObj.transform.position = new Vector3(3, 0, 0);
        cedricObj.transform.localScale = new Vector3(1, 1, 0.1f);
        Destroy(cedricObj.GetComponent<BoxCollider>());
        Destroy(cedricObj.GetComponent<Rigidbody>());

        cedricObj.GetComponent<Renderer>().material.color = Color.red;
        CutsceneCharacter cedricChar = cedricObj.AddComponent<CutsceneCharacter>();

        Debug.Log("Characters created: Lucio and Cedric");
    }

    private void SetupCameraController()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("No Main Camera found!");
            return;
        }

        if (mainCam.GetComponent<LucioCutsceneCameraController>() == null)
        {
            mainCam.gameObject.AddComponent<LucioCutsceneCameraController>();
            Debug.Log("Camera Controller added to Main Camera");
        }
    }

    private void CreateBeamSystem()
    {
        GameObject beamObj = new GameObject("BeamAttackSystem");
        beamObj.AddComponent<BeamAttackSystem>();
        Debug.Log("Beam Attack System created");
    }

    private void CreateQTEUI()
    {
        // Check if Canvas exists
        Canvas existingCanvas = Object.FindAnyObjectByType<Canvas>();
        Canvas targetCanvas = existingCanvas;

        if (targetCanvas == null)
        {
            GameObject canvasObj = new GameObject("QTECanvas");
            targetCanvas = canvasObj.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create QTE Panel
        GameObject panelObj = new GameObject("QTEPanel");
        panelObj.transform.SetParent(targetCanvas.transform);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        CanvasGroup panelCanvasGroup = panelObj.AddComponent<CanvasGroup>();
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0); // Transparent

        // Create Progress Bar Background
        GameObject progressBgObj = new GameObject("ProgressBarBg");
        progressBgObj.transform.SetParent(panelObj.transform);
        RectTransform progressBgRect = progressBgObj.AddComponent<RectTransform>();
        progressBgRect.anchoredPosition = new Vector2(0, -50);
        progressBgRect.sizeDelta = new Vector2(300, 50);
        Image progressBgImage = progressBgObj.AddComponent<Image>();
        progressBgImage.color = Color.gray;

        // Create Progress Bar
        GameObject progressBarObj = new GameObject("ProgressBar");
        progressBarObj.transform.SetParent(progressBgObj.transform);
        RectTransform progressBarRect = progressBarObj.AddComponent<RectTransform>();
        progressBarRect.anchorMin = new Vector2(0, 0.5f);
        progressBarRect.anchorMax = new Vector2(0, 0.5f);
        progressBarRect.offsetMin = Vector2.zero;
        progressBarRect.offsetMax = new Vector2(0, 0);
        progressBarRect.sizeDelta = new Vector2(300, 50);
        Image progressBar = progressBarObj.AddComponent<Image>();
        progressBar.color = Color.green;

        // Create Text
        GameObject textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(panelObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(600, 200);
        Text instructionText = textObj.AddComponent<Text>();
        instructionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        instructionText.text = "Hold Space!";
        instructionText.font.material.mainTexture.filterMode = FilterMode.Point;
        instructionText.alignment = TextAnchor.MiddleCenter;
        instructionText.fontSize = 40;
        instructionText.color = Color.white;

        // Add QTE System
        LucioCutsceneManager manager = Object.FindFirstObjectByType<LucioCutsceneManager>();
        if (manager != null)
        {
            QuickTimeEventSystem qteSystem = manager.GetComponent<QuickTimeEventSystem>();
            if (qteSystem == null)
            {
                qteSystem = manager.gameObject.AddComponent<QuickTimeEventSystem>();
            }
            qteSystem.SetUIReferences(panelCanvasGroup, instructionText, progressBar);
        }

        Debug.Log("QTE UI created");
    }

    private void SetupCutsceneManager()
    {
        LucioCutsceneManager manager = Object.FindFirstObjectByType<LucioCutsceneManager>();
        if (manager != null)
        {
            return; // Already exists
        }

        GameObject managerObj = new GameObject("LucioCutsceneManager");
        manager = managerObj.AddComponent<LucioCutsceneManager>();

        // Find and assign components
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            LucioCutsceneCameraController cameraCont = mainCam.GetComponent<LucioCutsceneCameraController>();
            if (cameraCont == null)
                cameraCont = mainCam.gameObject.AddComponent<LucioCutsceneCameraController>();
        }

        CutsceneCharacter[] characters = Object.FindObjectsByType<CutsceneCharacter>(FindObjectsSortMode.None);
        CutsceneCharacter lucio = System.Array.Find(characters, c => c.gameObject.name == "Lucio");
        CutsceneCharacter cedric = System.Array.Find(characters, c => c.gameObject.name == "Cedric");

        BeamAttackSystem beamSystem = Object.FindFirstObjectByType<BeamAttackSystem>();
        QuickTimeEventSystem qteSystem = Object.FindFirstObjectByType<QuickTimeEventSystem>();

        // Manually assign via reflection or through inspector
        Debug.Log("Cutscene Manager created - Please assign references in Inspector manually");
    }
}
