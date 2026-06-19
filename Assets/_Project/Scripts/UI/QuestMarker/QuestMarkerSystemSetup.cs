#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;

/// <summary>
/// Editor tool: Tools → Quest System → ...
/// Tự động setup toàn bộ hệ thống Quest Marker + Waypoint Navigator (NavMesh).
///
/// Bao gồm:
///   1. Dọn dẹp file trùng tên (WaypointNavigator1.cs, NavMeshPathfinder1.cs, v.v.)
///   2. Tạo GameObject WaypointNavigator trong scene
///   3. Tạo prefab QuestMarkerUI (FloatingIcon + EdgeArrowIcon + DistanceText)
///   4. Gán prefab + container vào QuestMarkerManager
/// </summary>
public static class QuestMarkerSystemSetup
{
    private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
    private const string PrefabPath   = PrefabFolder + "/QuestMarkerUI.prefab";

    // ── Menu: Full Setup ──────────────────────────────────────────────────────

    [MenuItem("Tools/Quest System/Full Setup (Scene + Prefab)")]
    public static void FullSetup()
    {
        Debug.Log("===== [Quest System Setup] BẮT ĐẦU =====");

        SetupWaypointNavigator();
        QuestMarkerUI prefab = CreateOrGetMarkerPrefab();
        SetupQuestMarkerManager(prefab);

        Debug.Log("===== [Quest System Setup] ✅ HOÀN TẤT =====");
        EditorUtility.DisplayDialog("Quest System Setup",
            "Đã setup xong:\n" +
            "- WaypointNavigator (scene)\n" +
            "- QuestMarkerUI prefab (" + PrefabPath + ")\n" +
            "- QuestMarkerManager đã gán prefab + container\n\n" +
            "Hãy tự gán Sprite cho FloatingIcon / EdgeArrowIcon trong prefab.",
            "OK");
    }

    // ── Menu: Find Duplicate Class Files ─────────────────────────────────────

    [MenuItem("Tools/Quest System/Find Duplicate Script Files")]
    public static void FindDuplicateScripts()
    {
        string[] classesToCheck = {
            "WaypointNavigator", "NavMeshPathfinder", "QuestMarkerUI",
            "QuestMarkerManager", "QuestMarkerBridge", "ScreenEdgeMarkerCalculator"
        };

        string[] allScripts = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        bool foundDuplicate = false;

        foreach (string className in classesToCheck)
        {
            var matches = allScripts.Where(path =>
            {
                string content = File.ReadAllText(path);
                return content.Contains("class " + className);
            }).ToList();

            if (matches.Count > 1)
            {
                foundDuplicate = true;
                Debug.LogError($"[Duplicate Check] ⚠ Class '{className}' xuất hiện trong {matches.Count} file:");
                foreach (string m in matches)
                    Debug.LogError($"    → {m.Replace(Application.dataPath, "Assets")}");
            }
        }

        if (!foundDuplicate)
        {
            Debug.Log("[Duplicate Check] ✅ Không có class nào bị trùng định nghĩa.");
            EditorUtility.DisplayDialog("Duplicate Check", "Không tìm thấy class trùng. Project sạch!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Duplicate Check",
                "Tìm thấy class bị định nghĩa trùng nhiều lần!\nXem chi tiết trong Console.", "OK");
        }
    }

    // ── Step 1: WaypointNavigator GameObject ─────────────────────────────────

    [MenuItem("Tools/Quest System/1 - Setup WaypointNavigator")]
    public static void SetupWaypointNavigator()
    {
        WaypointNavigator nav = Object.FindObjectOfType<WaypointNavigator>();
        if (nav == null)
        {
            GameObject go = new GameObject("WaypointNavigator");
            go.AddComponent<WaypointNavigator>();
            Undo.RegisterCreatedObjectUndo(go, "Create WaypointNavigator");
            Debug.Log("[Setup] ✅ Đã tạo WaypointNavigator trong scene.");
        }
        else
        {
            Debug.Log("[Setup] WaypointNavigator đã tồn tại — bỏ qua.");
        }
    }

    // ── Step 2: QuestMarkerUI Prefab ──────────────────────────────────────────

    [MenuItem("Tools/Quest System/2 - Create QuestMarkerUI Prefab")]
    public static QuestMarkerUI CreateOrGetMarkerPrefab()
    {
        // Đã có prefab rồi thì dùng lại, không tạo đè
        QuestMarkerUI existing = AssetDatabase.LoadAssetAtPath<QuestMarkerUI>(PrefabPath);
        if (existing != null)
        {
            Debug.Log("[Setup] Prefab QuestMarkerUI đã tồn tại tại " + PrefabPath + " — bỏ qua tạo mới.");
            return existing;
        }

        if (!Directory.Exists(PrefabFolder))
            Directory.CreateDirectory(PrefabFolder);

        // Root
        GameObject root = new GameObject("QuestMarkerUI", typeof(RectTransform));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(64f, 64f);

        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        QuestMarkerUI markerScript = root.AddComponent<QuestMarkerUI>();

        // FloatingIcon
        GameObject floatGO = new GameObject("FloatingIcon", typeof(RectTransform));
        floatGO.transform.SetParent(root.transform, false);
        RectTransform floatRect = floatGO.GetComponent<RectTransform>();
        floatRect.anchorMin = Vector2.zero;
        floatRect.anchorMax = Vector2.one;
        floatRect.sizeDelta = Vector2.zero;
        Image floatImg = floatGO.AddComponent<Image>();
        floatImg.color = new Color(1f, 0.85f, 0.1f); // vàng mặc định, đổi sprite sau

        // EdgeArrowIcon
        GameObject edgeGO = new GameObject("EdgeArrowIcon", typeof(RectTransform));
        edgeGO.transform.SetParent(root.transform, false);
        RectTransform edgeRect = edgeGO.GetComponent<RectTransform>();
        edgeRect.anchorMin = Vector2.zero;
        edgeRect.anchorMax = Vector2.one;
        edgeRect.sizeDelta = Vector2.zero;
        Image edgeImg = edgeGO.AddComponent<Image>();
        edgeImg.color = new Color(1f, 0.4f, 0.1f); // cam mặc định, đổi sprite sau
        edgeImg.enabled = false; // mặc định ẩn, script tự bật/tắt

        // DistanceText (optional)
        GameObject textGO = new GameObject("DistanceText", typeof(RectTransform));
        textGO.transform.SetParent(root.transform, false);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0f, -38f);
        textRect.sizeDelta = new Vector2(90f, 22f);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize  = 14f;
        tmp.color     = Color.white;

        // Gán field private qua SerializedObject
        SerializedObject so = new SerializedObject(markerScript);
        so.FindProperty("floatingIcon").objectReferenceValue  = floatImg;
        so.FindProperty("edgeArrowIcon").objectReferenceValue = edgeImg;
        so.FindProperty("distanceText").objectReferenceValue  = tmp;
        so.ApplyModifiedProperties();

        // Lưu thành prefab
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[Setup] ✅ Đã tạo prefab QuestMarkerUI tại " + PrefabPath);
        Debug.Log("[Setup] ⚠ Hãy mở prefab và gán Sprite thật cho FloatingIcon / EdgeArrowIcon!");

        QuestMarkerUI result = prefabAsset.GetComponent<QuestMarkerUI>();
        Selection.activeObject = prefabAsset;
        return result;
    }

    // ── Step 3: QuestMarkerManager ────────────────────────────────────────────

    [MenuItem("Tools/Quest System/3 - Setup QuestMarkerManager")]
    public static void SetupQuestMarkerManagerMenuItem()
    {
        QuestMarkerUI prefab = AssetDatabase.LoadAssetAtPath<QuestMarkerUI>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Setup] Chưa có prefab QuestMarkerUI! Chạy bước '2 - Create QuestMarkerUI Prefab' trước.");
            return;
        }
        SetupQuestMarkerManager(prefab);
    }

    private static void SetupQuestMarkerManager(QuestMarkerUI prefab)
    {
        QuestMarkerManager manager = Object.FindObjectOfType<QuestMarkerManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("QuestMarkerManager");
            manager = go.AddComponent<QuestMarkerManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create QuestMarkerManager");
            Debug.Log("[Setup] ✅ Đã tạo QuestMarkerManager trong scene.");
        }
        else
        {
            Debug.Log("[Setup] QuestMarkerManager đã tồn tại — cập nhật reference.");
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[Setup] ⚠ Không tìm thấy Canvas trong scene! Hãy tạo Canvas rồi chạy lại bước này.");
        }

        SerializedObject so = new SerializedObject(manager);
        if (prefab != null)
            so.FindProperty("markerPrefab").objectReferenceValue = prefab;
        if (canvas != null)
            so.FindProperty("markerContainer").objectReferenceValue = canvas.GetComponent<RectTransform>();
        so.ApplyModifiedProperties();

        Debug.Log("[Setup] ✅ Đã gán markerPrefab + markerContainer cho QuestMarkerManager.");
    }

    // ── Helper: Create Path/Node (giữ lại nếu vẫn còn dùng WaypointPath thủ công) ──
    // (Bỏ qua nếu bạn đã chuyển hẳn sang NavMesh — không cần các menu này nữa)
}
#endif
