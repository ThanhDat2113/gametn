#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

/// <summary>
/// Auto-setup tool cho Quest Marker System.
/// Menu: Tools → Quest Marker → Auto Setup
///
/// Tool này tự động:
///   1. Tạo (hoặc tìm) GameObject "QuestMarkerManager" trong scene + gắn component.
///   2. Tạo prefab QuestMarkerUI (Image + CanvasGroup) tại Resources/UI/QuestMarkerUI
///      nếu chưa tồn tại.
///   3. Tìm Canvas trong scene (hoặc tạo mới nếu không có) và gán vào markerContainer.
///   4. Gán markerPrefab cho QuestMarkerManager qua SerializedObject
///      (vì field là private, không thể gán trực tiếp từ ngoài).
///
/// Sau khi chạy, chỉ cần đảm bảo các NPC có DialogueTrigger + QuestMarkerBridge
/// gắn sẵn — hệ thống sẽ tự hoạt động, không cần setup tay thêm gì.
/// </summary>
public static class QuestMarkerAutoSetup
{
    private const string PrefabFolder = "Assets/Resources/UI";
    private const string PrefabPath   = PrefabFolder + "/QuestMarkerUI.prefab";
    private const string ManagerName  = "QuestMarkerManager";

    [MenuItem("Tools/Quest Marker/Auto Setup")]
    public static void RunAutoSetup()
    {
        Debug.Log("[QuestMarkerAutoSetup] === Bắt đầu auto setup ===");

        QuestMarkerUI prefab = EnsureMarkerPrefab();
        Canvas canvas        = EnsureCanvas();
        QuestMarkerManager manager = EnsureManager();

        AssignManagerReferences(manager, prefab, canvas);

        EditorUtility.SetDirty(manager);
        if (!Application.isPlaying)
            EditorSceneManager_MarkDirty();

        Debug.Log("[QuestMarkerAutoSetup] === Hoàn tất! Hệ thống đã sẵn sàng. ===");
        EditorUtility.DisplayDialog(
            "Quest Marker Auto Setup",
            "Setup hoàn tất!\n\n" +
            $"• Manager: '{ManagerName}' trong scene\n" +
            $"• Prefab: {PrefabPath}\n" +
            $"• Canvas: {(canvas != null ? canvas.name : "KHÔNG TÌM THẤY — tự tạo mới")}\n\n" +
            "Chỉ cần gắn QuestMarkerBridge lên các NPC có DialogueTrigger là xong.",
            "OK");
    }

    [MenuItem("Tools/Quest Marker/Validate Setup")]
    public static void ValidateSetup()
    {
        QuestMarkerManager manager = Object.FindObjectOfType<QuestMarkerManager>();
        bool ok = true;

        if (manager == null)
        {
            Debug.LogError("[QuestMarkerAutoSetup] ✗ Không tìm thấy QuestMarkerManager trong scene.");
            ok = false;
        }
        else
        {
            Debug.Log("[QuestMarkerAutoSetup] ✓ QuestMarkerManager tồn tại trong scene.");
        }

        QuestMarkerUI prefab = AssetDatabase.LoadAssetAtPath<QuestMarkerUI>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[QuestMarkerAutoSetup] ✗ Không tìm thấy prefab tại {PrefabPath}.");
            ok = false;
        }
        else
        {
            Debug.Log($"[QuestMarkerAutoSetup] ✓ Prefab tồn tại tại {PrefabPath}.");
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[QuestMarkerAutoSetup] ✗ Không tìm thấy Canvas nào trong scene.");
            ok = false;
        }
        else
        {
            Debug.Log($"[QuestMarkerAutoSetup] ✓ Canvas '{canvas.name}' tồn tại trong scene.");
        }

        QuestMarkerBridge[] bridges = Object.FindObjectsOfType<QuestMarkerBridge>();
        Debug.Log($"[QuestMarkerAutoSetup] ℹ Tìm thấy {bridges.Length} QuestMarkerBridge trong scene.");

        Debug.Log(ok
            ? "[QuestMarkerAutoSetup] === TẤT CẢ OK ==="
            : "[QuestMarkerAutoSetup] === CÓ LỖI, xem log phía trên ===");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static QuestMarkerUI EnsureMarkerPrefab()
    {
        QuestMarkerUI existing = AssetDatabase.LoadAssetAtPath<QuestMarkerUI>(PrefabPath);
        if (existing != null)
        {
            Debug.Log($"[QuestMarkerAutoSetup] Prefab đã tồn tại: {PrefabPath}");
            return existing;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");

        // Tạo GameObject tạm trong scene để build prefab
        GameObject go = new GameObject("QuestMarkerUI", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48f, 48f);

        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        // Sprite mặc định: dùng built-in UI sprite tròn nếu có
        Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (defaultSprite != null) img.sprite = defaultSprite;

        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        QuestMarkerUI markerUI = go.AddComponent<QuestMarkerUI>();

        // Gán arrowIcon field (private) qua SerializedObject
        SerializedObject so = new SerializedObject(markerUI);
        SerializedProperty arrowProp = so.FindProperty("arrowIcon");
        if (arrowProp != null) arrowProp.objectReferenceValue = img;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[QuestMarkerAutoSetup] Đã tạo prefab mới tại {PrefabPath}");
        return prefabAsset.GetComponent<QuestMarkerUI>();
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[QuestMarkerAutoSetup] Dùng Canvas có sẵn: {canvas.name}");
            return canvas;
        }

        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        Debug.Log("[QuestMarkerAutoSetup] Không tìm thấy Canvas nào → đã tạo Canvas mới (Screen Space Overlay).");

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[QuestMarkerAutoSetup] Đã tạo EventSystem.");
        }

        return canvas;
    }

    private static QuestMarkerManager EnsureManager()
    {
        QuestMarkerManager manager = Object.FindObjectOfType<QuestMarkerManager>();
        if (manager != null)
        {
            Debug.Log($"[QuestMarkerAutoSetup] Manager đã tồn tại trên '{manager.gameObject.name}'.");
            return manager;
        }

        GameObject managerGO = new GameObject(ManagerName);
        manager = managerGO.AddComponent<QuestMarkerManager>();
        Debug.Log($"[QuestMarkerAutoSetup] Đã tạo GameObject '{ManagerName}' + gắn component.");
        return manager;
    }

    private static void AssignManagerReferences(QuestMarkerManager manager, QuestMarkerUI prefab, Canvas canvas)
    {
        SerializedObject so = new SerializedObject(manager);

        SerializedProperty prefabProp = so.FindProperty("markerPrefab");
        if (prefabProp != null && prefabProp.objectReferenceValue == null)
            prefabProp.objectReferenceValue = prefab;

        SerializedProperty containerProp = so.FindProperty("markerContainer");
        if (containerProp != null && containerProp.objectReferenceValue == null && canvas != null)
            containerProp.objectReferenceValue = canvas.GetComponent<RectTransform>();

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[QuestMarkerAutoSetup] Đã gán markerPrefab + markerContainer cho Manager.");
    }

    private static void EditorSceneManager_MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
#endif
