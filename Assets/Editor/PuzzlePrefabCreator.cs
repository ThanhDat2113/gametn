using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility: Tự động tạo 6 puzzle UI prefabs cho 6 loại puzzle.
/// Vào Tools → Puzzle Quest → Create Prefabs để chạy.
/// </summary>
public class PuzzlePrefabCreator : EditorWindow
{
    private string prefabPath = "Assets/_Project/Prefabs/UI/Puzzle";

    [MenuItem("Tools/Puzzle Quest/Create Prefabs")]
    public static void CreateAllPrefabs()
    {
        var window = GetWindow<PuzzlePrefabCreator>();
        window.titleContent = new GUIContent("Create Puzzle Prefabs");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Puzzle Prefab Generator", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox("Tạo 6 UI Canvas prefabs cho 6 loại puzzle.", MessageType.Info);
        GUILayout.Space(5);

        GUILayout.Label("Batch 1:", EditorStyles.boldLabel);
        if (GUILayout.Button("1. SymbolSequenceCanvas", GUILayout.Height(25))) CreateSymbolSequencePrefab();
        if (GUILayout.Button("2. RiddleGateCanvas", GUILayout.Height(25))) CreateRiddleGatePrefab();
        if (GUILayout.Button("3. MemoryGroveCanvas", GUILayout.Height(25))) CreateMemoryGrovePrefab();

        GUILayout.Space(5);
        GUILayout.Label("Batch 2:", EditorStyles.boldLabel);
        if (GUILayout.Button("4. SlidePuzzleCanvas", GUILayout.Height(25))) BuildSlidePuzzlePrefab();
    if (GUILayout.Button("5. SpirePuzzleCanvas", GUILayout.Height(25))) BuildSpirePuzzlePrefab();
        if (GUILayout.Button("6. FlowPuzzleCanvas", GUILayout.Height(25))) BuildFlowPuzzlePrefab();

        GUILayout.Space(10);
        if (GUILayout.Button("Create ALL 6 Prefabs", GUILayout.Height(40)))
        {
            CreateSymbolSequencePrefab();
            CreateRiddleGatePrefab();
            CreateMemoryGrovePrefab();
            BuildSlidePuzzlePrefab();
            BuildSpirePuzzlePrefab();
            BuildFlowPuzzlePrefab();
            Debug.Log("[PuzzlePrefabCreator] ✅ All 6 prefabs created!");
        }
    }

    private void EnsureFolderExists()
    {
        string fullPath = Application.dataPath.Replace("/Assets", "") + "/" + prefabPath;
        if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
    }

    // ──────── Helpers ────────

    private GameObject CreateBaseCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        bg.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        bg.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
        bg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        return go;
    }

    private void CreateTitleText(Transform parent, string text, Vector2 pos)
    {
        var txt = CreateUIText(parent, "TitleText", text, 30, Color.white, pos);
        txt.fontStyle = FontStyle.Bold;
    }

    private Text CreateUIText(Transform parent, string name, string content, int fontSize, Color color, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize; text.color = color; text.alignment = TextAnchor.MiddleCenter;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 40); rect.anchoredPosition = anchoredPos;
        return text;
    }

    private GameObject CreateButton(Transform parent, string name, string label, int fontSize, float w = 80, float h = 80)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var btn = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        btn.targetGraphic = img;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.text = label; txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;
        txtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        txtGo.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        return go;
    }

    private Button CreateCloseButton(Transform parent)
    {
        var go = new GameObject("CloseButton");
        go.transform.SetParent(parent, false);
        var btn = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.5f, 0.15f, 0.15f);
        btn.targetGraphic = img;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 36);
        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -280);
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.text = "✕ Thoát"; txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;
        txtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 36);
        txtGo.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        return btn;
    }

    // ──────── BATCH 1 ────────

    private void CreateSymbolSequencePrefab()
    {
        var canvas = CreateBaseCanvas("SymbolSequenceCanvas");
        var p = canvas.AddComponent<SymbolSequencePuzzle>();
        CreateTitleText(canvas.transform, "🧠 THẦN ĐIỆN KÝ ỨC", new Vector2(0, 320));
        p.instructionText = CreateUIText(canvas.transform, "InstructionText", "Ghi nhớ thứ tự...", 22, Color.white, new Vector2(0, 230));
        var grid = new GameObject("GridPanel"); grid.transform.SetParent(canvas.transform, false);
        var gl = grid.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(80, 80); gl.spacing = new Vector2(12, 12);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3; gl.childAlignment = TextAnchor.MiddleCenter;
        grid.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 300); grid.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        p.gridLayout = gl;
        string[] syms = { "🔷", "🔶", "⬟", "⬡", "◆", "◇", "★", "●", "▲" };
        p.symbolButtons = new Button[9];
        for (int i = 0; i < 9; i++) p.symbolButtons[i] = CreateButton(grid.transform, $"S{i}", syms[i], 30).GetComponent<Button>();
        p.progressText = CreateUIText(canvas.transform, "ProgressText", "Vòng 1: 3/7", 18, Color.yellow, new Vector2(0, -190));
        p.mistakeText = CreateUIText(canvas.transform, "MistakeText", "Sai: 0/3", 18, Color.red, new Vector2(0, -220));
        p.closeButton = CreateCloseButton(canvas.transform);
        SavePrefab(canvas, "SymbolSequenceCanvas");
    }

    private void CreateRiddleGatePrefab()
    {
        var canvas = CreateBaseCanvas("RiddleGateCanvas");
        var p = canvas.AddComponent<RiddleGatePuzzle>();
        CreateTitleText(canvas.transform, "🏛️ CÁNH CỔNG TRI THỨC", new Vector2(0, 300));
        var r = CreateUIText(canvas.transform, "RiddleText", "\"...\"", 22, Color.white, new Vector2(0, 80));
        r.fontStyle = FontStyle.Italic; r.alignment = TextAnchor.MiddleCenter; r.horizontalOverflow = HorizontalWrapMode.Wrap;
        r.rectTransform.sizeDelta = new Vector2(600, 150); p.riddleText = r;
        var ap = new GameObject("AnswersPanel"); ap.transform.SetParent(canvas.transform, false);
        var vl = ap.AddComponent<VerticalLayoutGroup>(); vl.spacing = 10; vl.childAlignment = TextAnchor.MiddleCenter;
        ap.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 250); ap.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);
        string[] labs = { "A", "B", "C", "D" }; p.answerButtons = new Button[4];
        for (int i = 0; i < 4; i++) { var b = CreateButton(ap.transform, $"Ans_{labs[i]}", $"{labs[i]}. ...", 22, 450, 50); p.answerButtons[i] = b.GetComponent<Button>(); }
        p.progressText = CreateUIText(canvas.transform, "ProgressText", "Câu 1/3", 18, Color.yellow, new Vector2(-200, -220));
        p.attemptText = CreateUIText(canvas.transform, "AttemptText", "Sai: 0/3", 18, Color.red, new Vector2(200, -220));
        p.closeButton = CreateCloseButton(canvas.transform);
        SavePrefab(canvas, "RiddleGateCanvas");
    }

    private void CreateMemoryGrovePrefab()
    {
        var canvas = CreateBaseCanvas("MemoryGroveCanvas");
        var p = canvas.AddComponent<MemoryGrovePuzzle>();
        CreateTitleText(canvas.transform, "🌳 KHU RỪNG KÝ ỨC", new Vector2(0, 290));
        var grid = new GameObject("CardGrid"); grid.transform.SetParent(canvas.transform, false);
        var gl = grid.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(90, 110); gl.spacing = new Vector2(8, 8);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4; gl.childAlignment = TextAnchor.MiddleCenter;
        grid.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 350); grid.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
        p.cardGrid = gl; p.cardButtons = new Button[12]; p.cardImages = new Image[12];
        for (int i = 0; i < 12; i++)
        {
            var b = new GameObject($"C{i}"); b.transform.SetParent(grid.transform, false);
            var btn = b.AddComponent<Button>(); var img = b.AddComponent<Image>(); img.color = new Color(0.35f, 0.25f, 0.15f);
            b.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 110); btn.targetGraphic = img; p.cardButtons[i] = btn;
            var po = new GameObject("Portrait"); po.transform.SetParent(b.transform, false);
            var pi = po.AddComponent<Image>(); pi.color = Color.white;
            po.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 70); po.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 5);
            po.SetActive(false); p.cardImages[i] = pi;
        }
        p.matchCountText = CreateUIText(canvas.transform, "MatchText", "Cặp: 0/6", 18, Color.green, new Vector2(-150, -200));
        p.mismatchCountText = CreateUIText(canvas.transform, "MismatchText", "Sai: 0/10", 18, Color.red, new Vector2(150, -200));
        p.closeButton = CreateCloseButton(canvas.transform);

        var lp = new GameObject("LorePopup"); lp.transform.SetParent(canvas.transform, false); lp.SetActive(false);
        lp.AddComponent<Image>().color = new Color(0, 0, 0, 0.9f);
        lp.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 250); lp.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        p.lorePopup = lp;
        p.loreCharacterName = CreateUIText(lp.transform, "LoreName", "NV", 24, Color.yellow, new Vector2(0, 60));
        var ld = CreateUIText(lp.transform, "LoreText", "Lore...", 18, Color.white, new Vector2(0, -20));
        ld.alignment = TextAnchor.MiddleCenter; ld.horizontalOverflow = HorizontalWrapMode.Wrap; ld.rectTransform.sizeDelta = new Vector2(350, 120);
        p.loreText = ld;
        var lc = new GameObject("LoreClose"); lc.transform.SetParent(lp.transform, false);
        var lcb = lc.AddComponent<Button>(); lc.AddComponent<Image>().color = new Color(0.15f, 0.4f, 0.15f);
        var lct = new GameObject("Text"); lct.transform.SetParent(lc.transform, false);
        var lctc = lct.AddComponent<Text>(); lctc.text = "Tiếp tục"; lctc.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lctc.fontSize = 20; lctc.alignment = TextAnchor.MiddleCenter; lctc.color = Color.white;
        lct.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 40); lct.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        lc.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 40); lc.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
        p.loreCloseButton = lcb;
        SavePrefab(canvas, "MemoryGroveCanvas");
    }

    // ──────── BATCH 2 ────────

    private void BuildSlidePuzzlePrefab()
    {
        var canvas = CreateBaseCanvas("SlidePuzzleCanvas");
        var p = canvas.AddComponent<SlidePuzzle>();
        CreateTitleText(canvas.transform, "🧩 XẾP HÌNH TRƯỢT", new Vector2(0, 310));
        p.instructionText = CreateUIText(canvas.transform, "InstructionText", "Sắp xếp 1-8", 22, Color.white, new Vector2(0, 240));

        var grid = new GameObject("GridPanel"); grid.transform.SetParent(canvas.transform, false);
        var gl = grid.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(70, 70); gl.spacing = new Vector2(8, 8);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3; gl.childAlignment = TextAnchor.MiddleCenter;
        grid.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 240); grid.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
        p.gridLayout = gl; p.tileButtons = new Button[9];
        for (int i = 0; i < 9; i++) p.tileButtons[i] = CreateButton(grid.transform, $"T{i}", $"{i + 1}", 28).GetComponent<Button>();
        p.moveCountText = CreateUIText(canvas.transform, "MoveCount", "Bước: 0", 18, Color.yellow, new Vector2(0, -180));
        p.closeButton = CreateCloseButton(canvas.transform);
        SavePrefab(canvas, "SlidePuzzleCanvas");
    }

    private void BuildSpirePuzzlePrefab()
    {
        var canvas = CreateBaseCanvas("SpirePuzzleCanvas");
        var p = canvas.AddComponent<SpirePuzzle>();
        CreateTitleText(canvas.transform, "🗼 THÁP HUYỀN THOẠI", new Vector2(0, 300));
        p.instructionText = CreateUIText(canvas.transform, "InstructionText", "Chọn cọc nguồn", 22, Color.white, new Vector2(0, 230));

        // 3 peg containers + buttons
        p.pegButtons = new Button[3];
        p.pegContainers = new Transform[3];
        string[] pegLabels = { "A", "B", "C" };
        float[] pegX = { -150f, 0f, 150f };

        for (int i = 0; i < 3; i++)
        {
            // Peg container — THÊM RectTransform
            var container = new GameObject($"Peg{i}Container");
            var containerRect = container.AddComponent<RectTransform>();
            container.transform.SetParent(canvas.transform, false);
            containerRect.sizeDelta = new Vector2(100, 250);
            containerRect.anchoredPosition = new Vector2(pegX[i], 20);
            p.pegContainers[i] = container.transform;

            // Base line
            var baseLine = new GameObject("Base");
            baseLine.transform.SetParent(container.transform, false);
            var baseImg = baseLine.AddComponent<Image>();
            baseImg.color = new Color(0.3f, 0.3f, 0.3f);
            baseLine.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 6);
            baseLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -105);

            // Peg button
            var btn = CreateButton(canvas.transform, $"Peg_{pegLabels[i]}", pegLabels[i], 24, 80, 40);
            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(pegX[i], -170);
            p.pegButtons[i] = btn.GetComponent<Button>();
        }

        p.moveCountText = CreateUIText(canvas.transform, "MoveCount", "Bước: 0", 18, Color.yellow, new Vector2(0, -210));
        p.closeButton = CreateCloseButton(canvas.transform);
        SavePrefab(canvas, "SpirePuzzleCanvas");
    }

    private void BuildFlowPuzzlePrefab()
    {
        var canvas = CreateBaseCanvas("FlowPuzzleCanvas");
        var p = canvas.AddComponent<FlowPuzzle>();
        CreateTitleText(canvas.transform, "🌈 NỐI MÀU", new Vector2(0, 320));
        p.instructionText = CreateUIText(canvas.transform, "InstructionText", "Chọn điểm đầu để nối", 22, Color.white, new Vector2(0, 250));

        var grid = new GameObject("GridPanel"); grid.transform.SetParent(canvas.transform, false);
        var gl = grid.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(50, 50); gl.spacing = new Vector2(4, 4);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 5; gl.childAlignment = TextAnchor.MiddleCenter;
        grid.GetComponent<RectTransform>().sizeDelta = new Vector2(270, 270); grid.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
        p.gridLayout = gl; p.cellButtons = new Button[25];
        for (int i = 0; i < 25; i++) p.cellButtons[i] = CreateButton(grid.transform, $"F{i}", "", 14, 50, 50).GetComponent<Button>();
        p.progressText = CreateUIText(canvas.transform, "ProgressText", "Đã nối: 0/5", 18, Color.green, new Vector2(0, -200));
        p.closeButton = CreateCloseButton(canvas.transform);
        SavePrefab(canvas, "FlowPuzzleCanvas");
    }

    private void SavePrefab(GameObject go, string name)
    {
        EnsureFolderExists();
        string path = prefabPath + "/" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go); AssetDatabase.Refresh();
        Debug.Log($"[PuzzlePrefabCreator] ✅ Saved: {path}");
    }
}