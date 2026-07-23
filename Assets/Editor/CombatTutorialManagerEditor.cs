using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Custom Inspector cho CombatTutorialManager — thêm nút "Build Slideshow UI"
/// để tự động dựng toàn bộ UI cho tutorialMode = ImageSlideshow (Panel, Image,
/// Caption, Page text, nút Prev/Next) và gán thẳng vào các field private tương ứng.
///
/// KHÔNG cần tạo tay UI trong scene — bấm nút, xong.
/// Panel được tạo sẽ tắt sẵn (SetActive(false)), giống hành vi Start() của
/// CombatTutorialManager (ẩn cho tới khi PlayTutorial() gọi PlaySlideshow()).
/// </summary>
[CustomEditor(typeof(CombatTutorialManager))]
public class CombatTutorialManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Slideshow Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tự động tạo UI cho chế độ ImageSlideshow (Panel + Image + Caption + " +
            "Page text + nút Prev/Next) và gán vào các field bên trên. Nếu đã có " +
            "slideshowPanel, sẽ hỏi trước khi tạo lại (huỷ cái cũ).",
            MessageType.Info);

        if (GUILayout.Button("Build Slideshow UI"))
        {
            var manager = (CombatTutorialManager)target;
            CombatTutorialSlideshowBuilder.Build(manager);
        }
    }
}

/// <summary>
/// Logic dựng UI slideshow trong Editor. Tách riêng khỏi CombatTutorialManagerEditor
/// để có thể gọi lại từ chỗ khác (vd menu item) nếu cần sau này.
/// </summary>
public static class CombatTutorialSlideshowBuilder
{
    private const string PanelName = "SlideshowPanel";

    public static void Build(CombatTutorialManager manager)
    {
        var so = new SerializedObject(manager);

        var spPanel   = so.FindProperty("slideshowPanel");
        var spImage   = so.FindProperty("slideImage");
        var spCaption = so.FindProperty("slideCaptionText");
        var spPrev    = so.FindProperty("prevButton");
        var spNext    = so.FindProperty("nextSlideButton");
        var spPage    = so.FindProperty("slidePageText");

        if (spPanel == null)
        {
            Debug.LogError("[CombatTutorialSlideshowBuilder] Không tìm thấy field 'slideshowPanel' " +
                            "trên CombatTutorialManager — có thể script đã đổi tên field.");
            return;
        }

        if (spPanel.objectReferenceValue != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "Slideshow UI đã tồn tại",
                "slideshowPanel đã được gán sẵn. Bạn có muốn xoá UI cũ và tạo lại từ đầu không?",
                "Tạo lại", "Huỷ");

            if (!rebuild) return;

            var oldPanel = spPanel.objectReferenceValue as GameObject;
            if (oldPanel != null)
                Undo.DestroyObjectImmediate(oldPanel);
        }

        Canvas canvas = FindOrCreateCanvas();

        GameObject panel    = CreatePanel(canvas.transform);
        GameObject slideImg = CreateSlideImage(panel.transform);
        GameObject caption  = CreateLabel(panel.transform, "CaptionText",
                                           new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.27f),
                                           28, "Chú thích slide");
        GameObject page     = CreateLabel(panel.transform, "PageText",
                                           new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.1f),
                                           22, "1 / 1");
        GameObject prevBtn  = CreateNavButton(panel.transform, "PrevButton", "< Trước",
                                               new Vector2(0.04f, 0.02f), new Vector2(0.22f, 0.12f));
        GameObject nextBtn  = CreateNavButton(panel.transform, "NextButton", "Tiếp >",
                                               new Vector2(0.78f, 0.02f), new Vector2(0.96f, 0.12f));

        spPanel.objectReferenceValue   = panel;
        spImage.objectReferenceValue   = slideImg.GetComponent<Image>();
        spCaption.objectReferenceValue = caption.GetComponent<TMP_Text>();
        spPage.objectReferenceValue    = page.GetComponent<TMP_Text>();
        spPrev.objectReferenceValue    = prevBtn.GetComponent<Button>();
        spNext.objectReferenceValue    = nextBtn.GetComponent<Button>();

        so.ApplyModifiedProperties();

        // Ẩn sẵn — CombatTutorialManager.Start() cũng SetActive(false) mỗi lần load scene,
        // nhưng tắt luôn ở đây để không bị lộ trong scene view/game view lúc chưa Play.
        panel.SetActive(false);

        EditorUtility.SetDirty(manager);
        Selection.activeGameObject = panel;

        Debug.Log("[CombatTutorialSlideshowBuilder] Đã tạo xong UI slideshow và gán vào " +
                   manager.gameObject.name + ".");
    }

    private static Canvas FindOrCreateCanvas()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas;

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            Debug.LogWarning("[CombatTutorialSlideshowBuilder] Scene chưa có EventSystem — " +
                              "nút Prev/Next sẽ không nhận click. Vào GameObject > UI > Event System " +
                              "để Unity tự tạo với Input Module phù hợp (Input Manager cũ hoặc Input System mới).");
        }

        return canvas;
    }

    private static GameObject CreatePanel(Transform parent)
    {
        var go = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create Slideshow Panel");
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        return go;
    }

    private static GameObject CreateSlideImage(Transform parent)
    {
        var go = new GameObject("SlideImage", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create Slide Image");
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.28f);
        rt.anchorMax = new Vector2(0.9f, 0.85f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.preserveAspect = true;
        img.color = Color.white;

        return go;
    }

    private static GameObject CreateLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                           int fontSize, string defaultText)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.text = defaultText;
        text.raycastTarget = false;

        return go;
    }

    private static GameObject CreateNavButton(Transform parent, string name, string label,
                                               Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);

        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 24;
        labelText.color = Color.white;
        labelText.text = label;
        labelText.raycastTarget = false;

        return go;
    }
}
