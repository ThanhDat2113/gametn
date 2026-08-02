using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class PuzzleSetupWizard : EditorWindow
{
    private enum WizardStep { BasicInfo, PuzzleData, QuestData, SceneSetup, Done }
    private WizardStep currentStep = WizardStep.BasicInfo;

    private string puzzleID = "";
    private string puzzleName = "";
    private QuestStepType puzzleType = QuestStepType.SymbolSequence;
    private int allowedAttempts = 3;

    private int symbolStartLength = 3;
    private int symbolMaxLength = 7;
    private int riddleCount = 3;
    private string[] riddles = new string[3];
    private string[] correctAnswers = new string[3];
    private string[] wrongA = new string[3];
    private string[] wrongB = new string[3];
    private string[] wrongC = new string[3];
    private int requiredCorrect = 1;
    private int memoryCols = 4;
    private int memoryRows = 3;
    private bool showLore = true;
    private int slideGridSize = 3;
    private int slideMaxMoves = 100;
    private int pipeGridSize = 4;
    private int pipeMaxMoves = 30;
    private int flowGridSize = 5;
    private int spireDiskCount = 4;
    private int spireMaxMoves = 50;

    private bool createQuest = true;
    private string questId = "";
    private string questName = "";
    private string stepDescription = "";

    private bool createTriggerInScene = true;
    private string triggerObjectName = "";
    private Vector3 triggerPosition = Vector3.zero;
    private bool addToQuestManager = true;

    private string puzzleDataPath = "Assets/_Project/Data/Puzzle";
    private string questDataPath = "Assets/_Project/Data/Quest";
    private string prefabPath = "Assets/_Project/Prefabs/UI/Puzzle";

    private string resultLog = "";
    private bool hasError = false;

    [MenuItem("Tools/Puzzle Quest/Setup Wizard")]
    public static void ShowWindow()
    {
        var w = GetWindow<PuzzleSetupWizard>();
        w.titleContent = new GUIContent("Puzzle Wizard");
        w.minSize = new Vector2(500, 600);
        w.Show();
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(puzzleID))
            puzzleID = "puzzle_" + System.DateTime.Now.Ticks % 10000;
        triggerObjectName = "Trigger_" + puzzleID;
        questId = "quest_" + puzzleID;
    }

    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(10);
        switch (currentStep)
        {
            case WizardStep.BasicInfo: DrawStep1_BasicInfo(); break;
            case WizardStep.PuzzleData: DrawStep2_PuzzleData(); break;
            case WizardStep.QuestData: DrawStep3_QuestData(); break;
            case WizardStep.SceneSetup: DrawStep4_SceneSetup(); break;
            case WizardStep.Done: DrawStep5_Done(); break;
        }
        EditorGUILayout.Space(10);
        DrawNavigation();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < 5; i++)
        {
            bool isActive = (WizardStep)i == currentStep;
            GUI.color = isActive ? Color.green : Color.white;
            if (GUILayout.Button("" + (i + 1), GUILayout.Width(30), GUILayout.Height(30)))
                if (i <= (int)currentStep + 1) currentStep = (WizardStep)i;
            GUI.color = Color.white;
            if (i < 4) EditorGUILayout.LabelField("->", GUILayout.Width(20));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStep1_BasicInfo()
    {
        EditorGUILayout.LabelField("Thong Tin Co Ban", EditorStyles.boldLabel);
        puzzleID = EditorGUILayout.TextField("Puzzle ID", puzzleID);
        puzzleName = EditorGUILayout.TextField("Puzzle Name", puzzleName);
        puzzleType = (QuestStepType)EditorGUILayout.EnumPopup("Puzzle Type", puzzleType);
        allowedAttempts = EditorGUILayout.IntField("Allowed Attempts", allowedAttempts);
    }

    private void DrawStep2_PuzzleData()
    {
        EditorGUILayout.LabelField("Cau Hinh " + puzzleType, EditorStyles.boldLabel);
        if (puzzleType == QuestStepType.WoodQuiz)
        {
            EditorGUILayout.HelpBox("WoodQuiz (Klotski) layout (co loi giai):\n" +
                "  ####\n" +
                "  #.M#\n" +
                "  #.M#\n" +
                "  #AB#\n" +
                "  #.G#\n" +
                "M=do doc 1x2 | A/B=1x1 | G=goal\n" +
                "Cach giai: A xuong -> B trai -> M xuong", MessageType.Info);
        }
    }

    private void DrawStep3_QuestData()
    {
        EditorGUILayout.LabelField("Quest Configuration", EditorStyles.boldLabel);
        createQuest = EditorGUILayout.Toggle("Create Quest Data", createQuest);
        if (createQuest)
        {
            questId = EditorGUILayout.TextField("Quest ID", questId);
            questName = EditorGUILayout.TextField("Quest Name", questName);
            stepDescription = EditorGUILayout.TextField("Step Desc", stepDescription);
            addToQuestManager = EditorGUILayout.Toggle("Add to QuestManager", addToQuestManager);
        }
    }

    private void DrawStep4_SceneSetup()
    {
        EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);
        createTriggerInScene = EditorGUILayout.Toggle("Create Trigger", createTriggerInScene);
        if (createTriggerInScene)
        {
            triggerObjectName = EditorGUILayout.TextField("Object Name", triggerObjectName);
            triggerPosition = EditorGUILayout.Vector3Field("Position", triggerPosition);
        }
    }

    private void DrawStep5_Done()
    {
        if (hasError) EditorGUILayout.HelpBox("Co loi! Xem log.", MessageType.Error);
        else EditorGUILayout.HelpBox("Hoan thanh!", MessageType.Info);
        EditorGUILayout.TextArea(resultLog, GUILayout.Height(200));
        if (GUILayout.Button("Start Over")) { currentStep = WizardStep.BasicInfo; resultLog = ""; hasError = false; }
    }

    private void DrawNavigation()
    {
        EditorGUILayout.BeginHorizontal();
        if (currentStep > WizardStep.BasicInfo)
            if (GUILayout.Button("<- Previous", GUILayout.Height(30))) currentStep--;
        GUILayout.FlexibleSpace();
        if (currentStep < WizardStep.Done)
        {
            if (currentStep == WizardStep.SceneSetup)
            {
                GUI.color = Color.green;
                if (GUILayout.Button("Generate!", GUILayout.Height(30), GUILayout.Width(150))) GenerateAll();
                GUI.color = Color.white;
            }
            else
            {
                if (GUILayout.Button("Next ->", GUILayout.Height(30), GUILayout.Width(120))) currentStep++;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void GenerateAll()
    {
        hasError = false; resultLog = "";
        EditorApplication.delayCall += () =>
        {
            try
            {
                var puzzleData = CreatePuzzleDataAsset();
                QuestData questData = null;
                if (createQuest) questData = CreateQuestDataAsset();

                var prefab = GetExistingPrefab();
                if (createTriggerInScene && prefab != null)
                    CreateTriggerInScene(puzzleData, prefab);
                if (createQuest && questData != null && addToQuestManager)
                    AddQuestToManager(questData);

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                currentStep = WizardStep.Done;
            }
            catch (System.Exception e)
            {
                hasError = true; resultLog += "\n" + e.Message;
                currentStep = WizardStep.Done;
            }
        };
    }

    private PuzzleData CreatePuzzleDataAsset()
    {
        EnsureFolder(puzzleDataPath);
        var data = ScriptableObject.CreateInstance<PuzzleData>();
        data.puzzleID = puzzleID;
        data.puzzleName = puzzleName;
        data.puzzleType = puzzleType;
        data.allowedAttempts = allowedAttempts;

        if (puzzleType == QuestStepType.WoodQuiz)
        {
            data.woodQuizConfig = new WoodQuizConfig
            {
                gridWidth = 4, gridHeight = 5,
                boardLayout = new string[] { "####", "#.M#", "#.M#", "#AB#", "#.G#" },
                maxMoves = 50
            };
        }

        string path = puzzleDataPath + "/" + puzzleID + ".asset";
        AssetDatabase.CreateAsset(data, path);
        resultLog += "PuzzleData: " + path + "\n";
        return AssetDatabase.LoadAssetAtPath<PuzzleData>(path);
    }

    private QuestData CreateQuestDataAsset()
    {
        EnsureFolder(questDataPath);
        var data = ScriptableObject.CreateInstance<QuestData>();
        data.questId = questId;
        data.questName = questName;
        data.steps = new QuestStep[]
        {
            new QuestStep
            {
                stepId = "step_" + puzzleID,
                type = puzzleType,
                targetId = puzzleID,
                description = stepDescription,
                isCompleted = false
            }
        };
        data.rewards = new QuestReward[0];
        string path = questDataPath + "/" + questId + ".asset";
        AssetDatabase.CreateAsset(data, path);
        resultLog += "QuestData: " + path + "\n";
        return AssetDatabase.LoadAssetAtPath<QuestData>(path);
    }

    private GameObject GetExistingPrefab()
    {
        string name = "WoodQuizCanvas";
        string path = prefabPath + "/" + name + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null) resultLog += "Dung prefab: " + path + "\n";
        else resultLog += "Khong tim thay: " + path + "\n";
        return prefab;
    }

    private void CreateTriggerInScene(PuzzleData data, GameObject prefab)
    {
        var go = new GameObject(triggerObjectName);
        go.transform.position = triggerPosition;
        var trigger = go.AddComponent<PuzzleTrigger>();
        trigger.puzzleData = data;
        trigger.puzzleUIPrefab = prefab;
        trigger.interactKey = KeyCode.E;
        go.AddComponent<BoxCollider>().isTrigger = true;
        go.GetComponent<BoxCollider>().size = new Vector3(2, 2, 2);
        resultLog += "Trigger: " + triggerObjectName + "\n";
    }

    private void AddQuestToManager(QuestData questData)
    {
        var mgr = FindFirstObjectByType<QuestManager>();
        if (mgr == null) { resultLog += "Khong tim thay QuestManager.\n"; return; }
        int len = (mgr.questChain != null) ? mgr.questChain.Length : 0;
        System.Array.Resize(ref mgr.questChain, len + 1);
        mgr.questChain[len] = questData;
        if (len == 0) mgr.questTemplate = questData;
        EditorUtility.SetDirty(mgr);
        resultLog += "Added " + questData.questName + " to QuestManager\n";
    }

    private void EnsureFolder(string path)
    {
        string full = Application.dataPath.Replace("/Assets", "") + "/" + path;
        if (!Directory.Exists(full)) Directory.CreateDirectory(full);
    }
}
