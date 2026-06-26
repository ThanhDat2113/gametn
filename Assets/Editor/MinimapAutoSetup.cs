#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Auto-setup tool cho Minimap System.
/// Menu: Tools → Quest Marker → Minimap → Auto Setup
///
/// Tool này tự động dựng toàn bộ hierarchy UI cho minimap:
///   Canvas (tìm có sẵn hoặc tạo mới)
///     └─ MinimapRoot (RectTransform, neo góc — bạn chỉnh vị trí sau)
///         ├─ MinimapMask (Image, có component Mask) — chứa:
///         │     ├─ MinimapRawImage (RawImage hiển thị camera/static texture)
///         │     └─ MarkerContainer (RectTransform rỗng — chứa MinimapMarkerUI instances)
///         └─ PlayerIcon (Image — icon player ở giữa minimap)
///   + GameObject "MinimapController" gắn component MinimapController, đã gán
///     đầy đủ reference tới các UI element trên.
///   + Prefab MinimapMarkerUI tại Resources/UI/MinimapMarkerUI.prefab
///
/// SAU KHI CHẠY, BẠN CẦN:
///   1. Chọn DisplayMode (RenderTextureCamera / StaticTexture) trên MinimapController.
///   2. Nếu dùng StaticTexture: chọn context menu "Capture Static Snapshot" trên
///      MinimapController trước, rồi save Texture2D ra Asset, gán lại vào field.
///   3. Chọn MaskShape (Circle/Square) và gán sprite mask tương ứng nếu chưa có.
///   4. Chỉnh mapScale theo ý muốn (default 300).
///   5. Gán minimapMarkerPrefab vào QuestMarkerManager (đã tự gán nếu chạy
///      Quest Marker Auto Setup trước đó).
/// </summary>
public static class MinimapAutoSetup
{
    private const string MarkerPrefabFolder = "Assets/Resources/UI";
    private const string MarkerPrefabPath   = MarkerPrefabFolder + "/MinimapMarkerUI.prefab";
    private const string ControllerName     = "MinimapController";

    [MenuItem("Tools/Quest Marker/Minimap/Auto Setup")]
    public static void RunMinimapAutoSetup()
    {
        Debug.Log("[MinimapAutoSetup] === Bắt đầu setup Minimap ===");

        Canvas canvas = EnsureCanvas();
        RectTransform minimapRoot = EnsureMinimapHierarchy(canvas, out RawImage rawImage,
            out Image maskImage, out RectTransform markerContainer, out RectTransform playerIcon);

        MinimapMarkerUI markerPrefab = EnsureMinimapMarkerPrefab();
        MinimapController controller = EnsureController();

        AssignControllerReferences(controller, rawImage, maskImage, markerContainer, playerIcon);
        AssignManagerMinimapPrefab(markerPrefab);

        EditorUtility.SetDirty(controller);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[MinimapAutoSetup] === Hoàn tất! ===");
        EditorUtility.DisplayDialog(
            "Minimap Auto Setup",
            "Setup minimap hoàn tất!\n\n" +
            $"• Controller: '{ControllerName}' trong scene\n" +
            $"• Marker Prefab: {MarkerPrefabPath}\n" +
            $"• UI Hierarchy: '{minimapRoot.name}' dưới Canvas '{canvas.name}'\n\n" +
            "Tiếp theo:\n" +
            "1. Chọn DisplayMode trên MinimapController (RenderTextureCamera/StaticTexture)\n" +
            "2. Chỉnh mapScale (default 300) theo ý muốn\n" +
            "3. Chọn MaskShape (Circle/Square) + gán sprite mask\n" +
            "4. Kéo MinimapRoot vào vị trí mong muốn trên màn hình (vd góc trên-trái)",
            "OK");
    }

    [MenuItem("Tools/Quest Marker/Minimap/Validate Setup")]
    public static void ValidateMinimapSetup()
    {
        bool ok = true;

        MinimapController controller = Object.FindObjectOfType<MinimapController>();
        if (controller == null)
        {
            Debug.LogError("[MinimapAutoSetup] ✗ Không tìm thấy MinimapController trong scene.");
            ok = false;
        }
        else
        {
            Debug.Log("[MinimapAutoSetup] ✓ MinimapController tồn tại trong scene.");
        }

        MinimapMarkerUI prefab = AssetDatabase.LoadAssetAtPath<MinimapMarkerUI>(MarkerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[MinimapAutoSetup] ✗ Không tìm thấy prefab tại {MarkerPrefabPath}.");
            ok = false;
        }
        else
        {
            Debug.Log($"[MinimapAutoSetup] ✓ Prefab tồn tại tại {MarkerPrefabPath}.");
        }

        QuestMarkerManager manager = Object.FindObjectOfType<QuestMarkerManager>();
        if (manager == null)
        {
            Debug.LogWarning("[MinimapAutoSetup] ⚠ Không tìm thấy QuestMarkerManager — " +
                              "minimap marker sẽ không tự spawn khi quest active. " +
                              "Hãy chạy Tools/Quest Marker/Auto Setup trước.");
        }
        else
        {
            Debug.Log("[MinimapAutoSetup] ✓ QuestMarkerManager tồn tại trong scene.");
        }

        Debug.Log(ok
            ? "[MinimapAutoSetup] === TẤT CẢ OK ==="
            : "[MinimapAutoSetup] === CÓ LỖI, xem log phía trên ===");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[MinimapAutoSetup] Dùng Canvas có sẵn: {canvas.name}");
            return canvas;
        }

        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("[MinimapAutoSetup] Không tìm thấy Canvas → đã tạo Canvas mới (Screen Space Overlay).");
        return canvas;
    }

    private static RectTransform EnsureMinimapHierarchy(
        Canvas canvas,
        out RawImage rawImage,
        out Image maskImage,
        out RectTransform markerContainer,
        out RectTransform playerIcon)
    {
        Transform existingRoot = canvas.transform.Find("MinimapRoot");
        if (existingRoot != null)
        {
            Debug.Log("[MinimapAutoSetup] MinimapRoot đã tồn tại, dùng lại hierarchy hiện có.");
            RectTransform root = existingRoot.GetComponent<RectTransform>();
            Transform maskT = existingRoot.Find("MinimapMask");
            maskImage = maskT != null ? maskT.GetComponent<Image>() : null;
            rawImage = maskT != null ? maskT.Find("MinimapRawImage")?.GetComponent<RawImage>() : null;
            markerContainer = maskT != null ? maskT.Find("MarkerContainer")?.GetComponent<RectTransform>() : null;
            playerIcon = existingRoot.Find("PlayerIcon")?.GetComponent<RectTransform>();
            return root;
        }

        const float size = 200f;

        // Root — neo góc trên-trái, bạn có thể kéo lại sau
        GameObject rootGO = new GameObject("MinimapRoot", typeof(RectTransform));
        rootGO.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = rootGO.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0f, 1f);
        rootRT.anchorMax = new Vector2(0f, 1f);
        rootRT.pivot = new Vector2(0f, 1f);
        rootRT.sizeDelta = new Vector2(size, size);
        rootRT.anchoredPosition = new Vector2(24f, -24f);

        // Mask (Image + Mask component) — bo khung minimap theo hình dạng
        GameObject maskGO = new GameObject("MinimapMask", typeof(RectTransform));
        maskGO.transform.SetParent(rootGO.transform, false);
        RectTransform maskRT = maskGO.GetComponent<RectTransform>();
        maskRT.anchorMin = Vector2.zero;
        maskRT.anchorMax = Vector2.one;
        maskRT.sizeDelta = Vector2.zero;

        maskImage = maskGO.AddComponent<Image>();
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        maskImage.sprite = knobSprite; // default = circle; MinimapController.ApplyMaskShape sẽ đổi theo enum
        maskImage.raycastTarget = false;

        Mask mask = maskGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // RawImage hiển thị texture (camera RT hoặc static)
        GameObject rawGO = new GameObject("MinimapRawImage", typeof(RectTransform));
        rawGO.transform.SetParent(maskGO.transform, false);
        RectTransform rawRT = rawGO.GetComponent<RectTransform>();
        rawRT.anchorMin = Vector2.zero;
        rawRT.anchorMax = Vector2.one;
        rawRT.sizeDelta = Vector2.zero;
        rawImage = rawGO.AddComponent<RawImage>();
        rawImage.raycastTarget = false;

        // Container chứa marker NPC — đặt trên cùng layer với RawImage, không bị mask con xoay nhầm
        GameObject containerGO = new GameObject("MarkerContainer", typeof(RectTransform));
        containerGO.transform.SetParent(maskGO.transform, false);
        markerContainer = containerGO.GetComponent<RectTransform>();
        markerContainer.anchorMin = new Vector2(0.5f, 0.5f);
        markerContainer.anchorMax = new Vector2(0.5f, 0.5f);
        markerContainer.pivot = new Vector2(0.5f, 0.5f);
        markerContainer.anchoredPosition = Vector2.zero;
        markerContainer.sizeDelta = Vector2.zero;

        // Player icon — nằm ngoài mask con (không bị crop), ở giữa minimap
        GameObject playerIconGO = new GameObject("PlayerIcon", typeof(RectTransform));
        playerIconGO.transform.SetParent(rootGO.transform, false);
        playerIcon = playerIconGO.GetComponent<RectTransform>();
        playerIcon.anchorMin = new Vector2(0.5f, 0.5f);
        playerIcon.anchorMax = new Vector2(0.5f, 0.5f);
        playerIcon.pivot = new Vector2(0.5f, 0.5f);
        playerIcon.anchoredPosition = Vector2.zero;
        playerIcon.sizeDelta = new Vector2(20f, 20f);
        Image playerImg = playerIconGO.AddComponent<Image>();
        Sprite arrowSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        playerImg.sprite = arrowSprite;
        playerImg.color = Color.cyan;
        playerImg.raycastTarget = false;

        Debug.Log("[MinimapAutoSetup] Đã tạo UI hierarchy minimap mới dưới Canvas.");
        return rootRT;
    }

    private static MinimapMarkerUI EnsureMinimapMarkerPrefab()
    {
        MinimapMarkerUI existing = AssetDatabase.LoadAssetAtPath<MinimapMarkerUI>(MarkerPrefabPath);
        if (existing != null)
        {
            Debug.Log($"[MinimapAutoSetup] Prefab marker minimap đã tồn tại: {MarkerPrefabPath}");
            return existing;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(MarkerPrefabFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");

        GameObject go = new GameObject("MinimapMarkerUI", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(16f, 16f);

        Image img = go.AddComponent<Image>();
        img.color = Color.yellow;
        img.raycastTarget = false;
        Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (defaultSprite != null) img.sprite = defaultSprite;

        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        MinimapMarkerUI markerUI = go.AddComponent<MinimapMarkerUI>();

        SerializedObject so = new SerializedObject(markerUI);
        SerializedProperty iconProp = so.FindProperty("markerIcon");
        if (iconProp != null) iconProp.objectReferenceValue = img;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(go, MarkerPrefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[MinimapAutoSetup] Đã tạo prefab marker minimap mới tại {MarkerPrefabPath}");
        return prefabAsset.GetComponent<MinimapMarkerUI>();
    }

    private static MinimapController EnsureController()
    {
        MinimapController controller = Object.FindObjectOfType<MinimapController>();
        if (controller != null)
        {
            Debug.Log($"[MinimapAutoSetup] Controller đã tồn tại trên '{controller.gameObject.name}'.");
            return controller;
        }

        GameObject controllerGO = new GameObject(ControllerName);
        controller = controllerGO.AddComponent<MinimapController>();
        Debug.Log($"[MinimapAutoSetup] Đã tạo GameObject '{ControllerName}' + gắn component.");
        return controller;
    }

    private static void AssignControllerReferences(
        MinimapController controller,
        RawImage rawImage,
        Image maskImage,
        RectTransform markerContainer,
        RectTransform playerIcon)
    {
        SerializedObject so = new SerializedObject(controller);

        SetIfEmpty(so, "minimapRawImage", rawImage);
        SetIfEmpty(so, "minimapMaskImage", maskImage);
        SetIfEmpty(so, "markerContainer", markerContainer);
        SetIfEmpty(so, "playerIcon", playerIcon);

        // Gán sẵn sprite mask tròn/vuông mặc định nếu trống, để MaskShape switch hoạt động ngay
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite uiSpriteSquare = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        SetIfEmpty(so, "circleMaskSprite", knobSprite);
        SetIfEmpty(so, "squareMaskSprite", uiSpriteSquare);

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[MinimapAutoSetup] Đã gán UI references cho MinimapController.");
    }

    private static void SetIfEmpty(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null && prop.objectReferenceValue == null)
            prop.objectReferenceValue = value;
    }

    private static void AssignManagerMinimapPrefab(MinimapMarkerUI markerPrefab)
    {
        QuestMarkerManager manager = Object.FindObjectOfType<QuestMarkerManager>();
        if (manager == null)
        {
            Debug.LogWarning("[MinimapAutoSetup] Không tìm thấy QuestMarkerManager trong scene — " +
                              "bỏ qua gán minimapMarkerPrefab. Chạy Tools/Quest Marker/Auto Setup trước, " +
                              "rồi chạy lại Minimap Auto Setup để tự gán.");
            return;
        }

        SerializedObject so = new SerializedObject(manager);
        SetIfEmpty(so, "minimapMarkerPrefab", markerPrefab);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);

        Debug.Log("[MinimapAutoSetup] Đã gán minimapMarkerPrefab cho QuestMarkerManager.");
    }
}
#endif
