#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Auto-setup tool cho WoodQuiz (Klotski) Puzzle.
/// Menu: Tools -> Puzzle Quest -> WoodQuiz Auto Setup
///
/// Tool nay tu dong:
///   1. Tao WoodQuizCanvas.prefab (UI Canvas + grid + block template + text + buttons)
///   2. Tao PuzzleData asset voi layout Klotski mau
///   3. Tao QuestData asset
///   4. Tao PuzzleTrigger GameObject trong scene (BoxCollider + prompt [E])
///   5. Them quest vao QuestManager.questChain
///
/// Layout mau: Master do doc 1x2, A/B block 1x1, co loi giai.
/// </summary>
public class WoodQuizAutoSetup : EditorWindow
{
    private const string PrefabFolder = "Assets/_Project/Prefabs/UI/Puzzle";
    private const string PrefabPath = PrefabFolder + "/WoodQuizCanvas.prefab";
    private const string PuzzleDataFolder = "Assets/_Project/Data/Puzzle";
    private const string QuestDataFolder = "Assets/_Project/Data/Quest";

    private string puzzleID = "wood_quiz_01";
    private string puzzleName = "Khoi Go Thoat Hop";
    private string questID = "quest_wood_quiz_01";
    private string questName = "Thu thach Khoi Go";
    private string stepDescription = "Keo khoi go do ra khoi hop";
    private Vector3 triggerPosition = Vector3.zero;
    private string triggerName = "Trigger_WoodQuiz";

    // Layout mau 4x5 (co loi giai):
    // ####
    // #.M#   M = master do doc 1x2 (o 2,1 va 2,2)
    // #.M#
    // #AB#   A = block 1x1 (1,3), B = block 1x1 (2,3)
    // #.G#   G = loi thoat (2,4)
    // Cach giai: keo A xuong -> keo B sang trai -> keo M xuong
    private static readonly string[] DefaultBoardLayout = new string[]
    {
        "####",
        "#.M#",
        "#.M#",
        "#AB#",
        "#.G#"
    };

    [MenuItem("Tools/Puzzle Quest/WoodQuiz Auto Setup")]
    public static void ShowWindow()
    {
        var w = GetWindow<WoodQuizAutoSetup>();
        w.titleContent = new GUIContent("WoodQuiz Auto Setup");
        w.minSize = new Vector2(420, 400);
        w.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("WoodQuiz (Klotski) Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Tu dong tao: Prefab + PuzzleData + QuestData + Trigger trong scene.\n" +
            "Chay 1 click la xong.",
            MessageType.Info);
        EditorGUILayout.Space(10);

        GUILayout.Label("Puzzle Info", EditorStyles.boldLabel);
        puzzleID = EditorGUILayout.TextField("Puzzle ID", puzzleID);
        puzzleName = EditorGUILayout.TextField("Puzzle Name", puzzleName);
        EditorGUILayout.Space(5);

        GUILayout.Label("Quest Info", EditorStyles.boldLabel);
        questID = EditorGUILayout.TextField("Quest ID", questID);
        questName = EditorGUILayout.TextField("Quest Name", questName);
        stepDescription = EditorGUILayout.TextField("Step Desc", stepDescription);
        EditorGUILayout.Space(5);

        GUILayout.Label("Scene Trigger", EditorStyles.boldLabel);
        triggerName = EditorGUILayout.TextField("Trigger Name", triggerName);
        triggerPosition = EditorGUILayout.Vector3Field("Position", triggerPosition);
        EditorGUILayout.Space(10);

        GUILayout.Label("Board Layout (4x5):", EditorStyles.boldLabel);
        foreach (string row in DefaultBoardLayout)
        {
            GUILayout.Label("  " + row);
        }
        GUILayout.Label("  M=do doc 1x2 | A/B=block 1x1 | G=goal | Cach giai: A xuong -> B trai -> M xuong", EditorStyles.miniLabel);
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Auto Setup (Tao tat ca)", GUILayout.Height(40)))
        {
            RunAutoSetup();
        }
        EditorGUILayout.Space(5);
        if (GUILayout.Button("Validate Setup", GUILayout.Height(25)))
        {
            ValidateSetup();
        }
    }

    private void RunAutoSetup()
    {
        Debug.Log("[WoodQuizAutoSetup] === Bat dau auto setup ===");
        try
        {
            GameObject prefab = EnsurePrefab();
            PuzzleData puzzleData = CreatePuzzleDataAsset();
            QuestData questData = CreateQuestDataAsset();
            CreateTriggerInScene(puzzleData, prefab);
            AddQuestToManager(questData);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WoodQuizAutoSetup] === Hoan tat! ===");
            EditorUtility.DisplayDialog(
                "WoodQuiz Auto Setup",
                "Setup hoan tat!\n\n" +
                "Prefab: " + PrefabPath + "\n" +
                "PuzzleData: " + PuzzleDataFolder + "/" + puzzleID + ".asset\n" +
                "QuestData: " + QuestDataFolder + "/" + questID + ".asset\n" +
                "Trigger: " + triggerName + " trong scene\n\n" +
                "Layout:\n" +
                "  ####\n" +
                "  #.M#\n" +
                "  #.M#\n" +
                "  #AB#\n" +
                "  #.G#\n\n" +
                "M = master do doc 1x2 | A/B = block 1x1 | G = goal\n" +
                "Cach giai: A xuong -> B trai -> M xuong\n" +
                "Keo block -> truot lien tuc den khi cham vat can.",
                "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[WoodQuizAutoSetup] Loi: " + e.Message + "\n" + e.StackTrace);
            EditorUtility.DisplayDialog("WoodQuiz Auto Setup", "Loi:\n" + e.Message, "OK");
        }
    }

    private GameObject EnsurePrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            Debug.Log("[WoodQuizAutoSetup] Prefab da ton tai: " + PrefabPath);
            return existing;
        }

        EnsureFolder(PrefabFolder);

        var canvas = new GameObject("WoodQuizCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var cv = canvas.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var bg = new GameObject("Background");
        bg.transform.SetParent(canvas.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.85f);
        bg.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
        bg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        var puzzle = canvas.AddComponent<WoodQuizPuzzle>();

        var title = CreateText(canvas.transform, "TitleText", "KHOI GO THOAT HOP", 30, Color.white, new Vector2(0, 320));
        title.fontStyle = FontStyle.Bold;

        puzzle.instructionText = CreateText(canvas.transform, "InstructionText",
            "Keo khoi go do ra loi thoat (G) -> truot lien tuc", 22, Color.white, new Vector2(0, 250));

        var grid = new GameObject("GridPanel");
        grid.transform.SetParent(canvas.transform, false);
        var gl = grid.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(70, 70);
        gl.spacing = Vector2.zero;
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 4;
        gl.childAlignment = TextAnchor.MiddleCenter;
        grid.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 350);
        grid.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
        puzzle.gridLayout = gl;

        var blockCont = new GameObject("BlockContainer");
        blockCont.transform.SetParent(canvas.transform, false);
        var blockRect = blockCont.AddComponent<RectTransform>();
        blockRect.sizeDelta = new Vector2(280, 350);
        blockRect.anchoredPosition = new Vector2(0, 20);
        puzzle.blockContainer = blockRect;

        var blockTemplate = new GameObject("BlockPrefab_Template", typeof(Image), typeof(CanvasGroup));
        blockTemplate.transform.SetParent(blockCont.transform, false);
        blockTemplate.AddComponent<WoodQuizBlockDrag>();
        var cg = blockTemplate.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;
        blockTemplate.GetComponent<Image>().raycastTarget = true;
        blockTemplate.GetComponent<RectTransform>().sizeDelta = new Vector2(66, 66);
        blockTemplate.SetActive(false);
        puzzle.blockPrefab = blockTemplate;

        puzzle.moveCountText = CreateText(canvas.transform, "MoveCount", "Buoc: 0/50", 18,
            Color.yellow, new Vector2(-100, -200));

        var resetBtn = CreateButton(canvas.transform, "ResetButton", "Lam lai", 18, 120, 36,
            new Vector2(60, -200));
        puzzle.resetButton = resetBtn.GetComponent<Button>();

        var closeBtn = CreateButton(canvas.transform, "CloseButton", "Thoat", 18, 120, 36,
            new Vector2(200, -200));
        puzzle.closeButton = closeBtn.GetComponent<Button>();

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath);
        DestroyImmediate(canvas);
        AssetDatabase.Refresh();

        Debug.Log("[WoodQuizAutoSetup] Da tao prefab: " + PrefabPath);
        return prefabAsset;
    }

    private PuzzleData CreatePuzzleDataAsset()
    {
        EnsureFolder(PuzzleDataFolder);
        string path = PuzzleDataFolder + "/" + puzzleID + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<PuzzleData>(path);
        if (existing != null)
        {
            Debug.Log("[WoodQuizAutoSetup] PuzzleData da ton tai: " + path);
            return existing;
        }

        var data = ScriptableObject.CreateInstance<PuzzleData>();
        data.puzzleID = puzzleID;
        data.puzzleName = puzzleName;
        data.puzzleType = QuestStepType.WoodQuiz;
        data.allowedAttempts = 3;
        data.woodQuizConfig = new WoodQuizConfig
        {
            gridWidth = 4,
            gridHeight = 5,
            boardLayout = new string[]
            {
                "####",
                "#.M#",
                "#.M#",
                "#AB#",
                "#.G#"
            },
            maxMoves = 50,
            emptyColor = new Color(0.12f, 0.08f, 0.05f, 1f),
            wallColor = new Color(0.25f, 0.18f, 0.12f, 1f),
            woodLightColor = new Color(0.72f, 0.52f, 0.30f, 1f),
            woodDarkColor = new Color(0.55f, 0.38f, 0.20f, 1f),
            masterColor = new Color(0.85f, 0.25f, 0.20f, 1f),
            goalColor = new Color(0.20f, 0.75f, 0.35f, 1f)
        };

        AssetDatabase.CreateAsset(data, path);
        Debug.Log("[WoodQuizAutoSetup] Da tao PuzzleData: " + path);
        return AssetDatabase.LoadAssetAtPath<PuzzleData>(path);
    }

    private QuestData CreateQuestDataAsset()
    {
        EnsureFolder(QuestDataFolder);
        string path = QuestDataFolder + "/" + questID + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<QuestData>(path);
        if (existing != null)
        {
            Debug.Log("[WoodQuizAutoSetup] QuestData da ton tai: " + path);
            return existing;
        }

        var data = ScriptableObject.CreateInstance<QuestData>();
        data.questId = questID;
        data.questName = questName;
        data.steps = new QuestStep[]
        {
            new QuestStep
            {
                stepId = "step_" + puzzleID,
                type = QuestStepType.WoodQuiz,
                targetId = puzzleID,
                description = stepDescription,
                isCompleted = false
            }
        };
        data.rewards = new QuestReward[0];

        AssetDatabase.CreateAsset(data, path);
        Debug.Log("[WoodQuizAutoSetup] Da tao QuestData: " + path);
        return AssetDatabase.LoadAssetAtPath<QuestData>(path);
    }

    private void CreateTriggerInScene(PuzzleData puzzleData, GameObject prefab)
    {
        var existing = GameObject.Find(triggerName);
        if (existing != null)
        {
            Debug.Log("[WoodQuizAutoSetup] Trigger " + triggerName + " da ton tai.");
            return;
        }

        var go = new GameObject(triggerName);
        go.transform.position = triggerPosition;

        var trigger = go.AddComponent<PuzzleTrigger>();
        trigger.puzzleData = puzzleData;
        trigger.puzzleUIPrefab = prefab;
        trigger.interactKey = KeyCode.E;

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2, 2, 2);

        var prompt = new GameObject("InteractionPrompt");
        prompt.transform.SetParent(go.transform, false);
        prompt.transform.localPosition = new Vector3(0, 2.5f, 0);
        var c = prompt.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        c.sortingOrder = 100;
        prompt.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10;

        var tgo = new GameObject("PromptText");
        tgo.transform.SetParent(prompt.transform, false);
        var txt = tgo.AddComponent<Text>();
        txt.text = "[E]";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 40;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontStyle = FontStyle.Bold;
        tgo.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 1);
        tgo.GetComponent<RectTransform>().localPosition = Vector3.zero;
        var outline = tgo.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2, 2);

        trigger.interactionPrompt = prompt;
        Selection.activeGameObject = go;
        Debug.Log("[WoodQuizAutoSetup] Da tao trigger " + triggerName);
    }

    private void AddQuestToManager(QuestData questData)
    {
        var mgr = FindFirstObjectByType<QuestManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[WoodQuizAutoSetup] Khong tim thay QuestManager.");
            return;
        }

        if (mgr.questChain != null)
        {
            foreach (var q in mgr.questChain)
            {
                if (q != null && q.questId == questData.questId)
                {
                    Debug.Log("[WoodQuizAutoSetup] Quest da co trong chain.");
                    return;
                }
            }
        }

        int len = (mgr.questChain != null) ? mgr.questChain.Length : 0;
        System.Array.Resize(ref mgr.questChain, len + 1);
        mgr.questChain[len] = questData;
        if (len == 0) mgr.questTemplate = questData;
        EditorUtility.SetDirty(mgr);
        Debug.Log("[WoodQuizAutoSetup] Da them " + questData.questName + " vao QuestManager");
    }

    private void ValidateSetup()
    {
        bool ok = true;
        string report = "";

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { report += "X Prefab KHONG tim thay\n"; ok = false; }
        else report += "V Prefab OK\n";

        var puzzleData = AssetDatabase.LoadAssetAtPath<PuzzleData>(PuzzleDataFolder + "/" + puzzleID + ".asset");
        if (puzzleData == null) { report += "X PuzzleData KHONG tim thay\n"; ok = false; }
        else report += "V PuzzleData OK\n";

        var questData = AssetDatabase.LoadAssetAtPath<QuestData>(QuestDataFolder + "/" + questID + ".asset");
        if (questData == null) { report += "X QuestData KHONG tim thay\n"; ok = false; }
        else report += "V QuestData OK\n";

        var trigger = GameObject.Find(triggerName);
        if (trigger == null) { report += "X Trigger KHONG tim thay trong scene\n"; ok = false; }
        else report += "V Trigger OK\n";

        var mgr = FindFirstObjectByType<QuestManager>();
        if (mgr == null) { report += "W QuestManager KHONG tim thay trong scene\n"; }
        else report += "V QuestManager OK\n";

        Debug.Log("[WoodQuizAutoSetup] Validate:\n" + report);
        EditorUtility.DisplayDialog("WoodQuiz Validate",
            (ok ? "V Tat ca OK!\n\n" : "X Co loi!\n\n") + report, "OK");
    }

    private void EnsureFolder(string path)
    {
        string full = Application.dataPath.Replace("/Assets", "") + "/" + path;
        if (!Directory.Exists(full))
        {
            Directory.CreateDirectory(full);
            AssetDatabase.Refresh();
        }
    }

    private Text CreateText(Transform parent, string name, string content,
        int fontSize, Color color, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 40);
        rect.anchoredPosition = pos;
        return text;
    }

    private GameObject CreateButton(Transform parent, string name, string label,
        int fontSize, float w, float h, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var btn = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.2f, 0.15f, 1f);
        btn.targetGraphic = img;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = pos;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.sizeDelta = new Vector2(w, h);
        txtRect.anchoredPosition = Vector2.zero;
        return go;
    }
}
#endif
