#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Tự động dựng UI + gắn component MapTutorialManager vào scene đang mở.
/// Không cần kéo-thả tay từng field trong Inspector.
///
/// CÁCH DÙNG:
///   Menu Tutorial → Setup Map Tutorial
///
/// Script sẽ:
///   1. Tìm (hoặc tạo mới) 1 Canvas gốc trong scene hiện tại — ưu tiên Canvas có sẵn
///      tên chứa "HUD" hoặc "Canvas", nếu không có sẽ tạo Canvas mới (Screen Space - Overlay).
///   2. Tạo GameObject "MapTutorialManager" + component, DontDestroyOnLoad (script tự lo lúc Play).
///   3. Tạo hierarchy UI: MapTutorialPanel (dimmed background) → SlideImage, CaptionText,
///      PageText, PrevButton, NextButton.
///   4. Tạo HelpButton (icon "?") neo góc trên-phải HUD.
///   5. Gán toàn bộ reference vào MapTutorialManager qua SerializedObject.
///   6. Panel mặc định SetActive(false) — script tự bật lúc chạy nếu autoPlayFirstTime.
///
/// SAU KHI CHẠY: bạn chỉ cần điền Sprite cho từng ảnh trong danh sách "Slides" ở Inspector,
/// và chỉnh sửa màu sắc/kích thước UI theo ý muốn — mọi wiring đã xong.
/// </summary>
public static class MapTutorialEditorSetup
{
    [MenuItem("Tutorial/Setup Map Tutorial")]
    public static void SetupMapTutorial()
    {
        // ── 1. Tìm hoặc tạo Canvas gốc ───────────────────────────────────────
        Canvas hudCanvas = FindOrCreateHudCanvas();

        // ── 2. Tạo GameObject MapTutorialManager ────────────────────────────
        GameObject managerGO = GameObject.Find("MapTutorialManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("MapTutorialManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create MapTutorialManager");
        }

        MapTutorialManager manager = managerGO.GetComponent<MapTutorialManager>();
        if (manager == null)
            manager = Undo.AddComponent<MapTutorialManager>(managerGO);

        // ── 3. Tạo Panel slideshow ───────────────────────────────────────────
        GameObject panel = CreatePanel(hudCanvas.transform);
        Image slideImage = CreateSlideImage(panel.transform);
        TMP_Text captionText = CreateCaptionText(panel.transform);
        TMP_Text pageText = CreatePageText(panel.transform);
        Button prevButton = CreateNavButton(panel.transform, "PrevButton", "< Trước", TextAnchor.MiddleLeft, isLeft: true);
        Button nextButton = CreateNavButton(panel.transform, "NextButton", "Tiếp >", TextAnchor.MiddleRight, isLeft: false);

        panel.SetActive(false);

        // ── 4. Tạo Help Button trên HUD ──────────────────────────────────────
        Button helpButton = CreateHelpButton(hudCanvas.transform);

        // ── 5. Gán reference vào MapTutorialManager qua SerializedObject ────
        var so = new SerializedObject(manager);
        so.FindProperty("slideshowPanel").objectReferenceValue = panel;
        so.FindProperty("slideImage").objectReferenceValue = slideImage;
        so.FindProperty("slideCaptionText").objectReferenceValue = captionText;
        so.FindProperty("slidePageText").objectReferenceValue = pageText;
        so.FindProperty("prevButton").objectReferenceValue = prevButton;
        so.FindProperty("nextButton").objectReferenceValue = nextButton;
        so.FindProperty("helpButton").objectReferenceValue = helpButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = managerGO;
        EditorSceneManager_MarkDirty();

        Debug.Log("[MapTutorialEditorSetup] Setup xong! Vào Inspector của MapTutorialManager, " +
                   "điền Sprite cho từng slide trong danh sách 'Slides', rồi chỉnh sửa UI (màu/kích thước) tuỳ ý.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EditorSceneManager_MarkDirty()
    {
        var stage = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(stage);
    }

    private static Canvas FindOrCreateHudCanvas()
    {
        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay &&
                (c.name.IndexOf("HUD", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 c.name.IndexOf("Canvas", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return c;
            }
        }

        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create HUD Canvas");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (canvasGO.GetComponent<UnityEngine.EventSystems.EventSystem>() == null &&
            Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        }

        return canvas;
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("MapTutorialPanel", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create MapTutorialPanel");
        panel.transform.SetParent(parent, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f); // dimmed background che toàn màn hình

        return panel;
    }

    private static Image CreateSlideImage(Transform parent)
    {
        GameObject go = new GameObject("SlideImage", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900, 550);
        rt.anchoredPosition = new Vector2(0, 40);

        var img = go.GetComponent<Image>();
        img.preserveAspect = true;
        img.color = Color.white;

        return img;
    }

    private static TMP_Text CreateCaptionText(Transform parent)
    {
        GameObject go = new GameObject("CaptionText", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(900, 80);
        rt.anchoredPosition = new Vector2(0, 160);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "Chú thích hướng dẫn";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28;
        tmp.color = Color.white;

        return tmp;
    }

    private static TMP_Text CreatePageText(Transform parent)
    {
        GameObject go = new GameObject("PageText", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(200, 40);
        rt.anchoredPosition = new Vector2(0, 100);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "1 / 1";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 22;
        tmp.color = new Color(1f, 1f, 1f, 0.7f);

        return tmp;
    }

    private static Button CreateNavButton(Transform parent, string name, string label, TextAnchor anchor, bool isLeft)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(isLeft ? 0f : 1f, 0f);
        rt.anchorMax = new Vector2(isLeft ? 0f : 1f, 0f);
        rt.pivot = new Vector2(isLeft ? 0f : 1f, 0f);
        rt.sizeDelta = new Vector2(180, 60);
        rt.anchoredPosition = new Vector2(isLeft ? 60 : -60, 60);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 24;
        tmp.color = Color.white;

        return go.GetComponent<Button>();
    }

    private static Button CreateHelpButton(Transform parent)
    {
        GameObject existing = GameObject.Find("HelpButton");
        if (existing != null) return existing.GetComponent<Button>();

        GameObject go = new GameObject("HelpButton", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create HelpButton");
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(64, 64);
        rt.anchoredPosition = new Vector2(-30, -30);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "?";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;

        return go.GetComponent<Button>();
    }
}
#endif
