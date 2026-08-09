using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Startup Settings")]
    public string initialMapName = "Map1";
    public string initialSpawnID = "Spawn_Start";

    [Header("Transition Settings")]
    public float fadeDuration = 0.5f;

    private string currentMapName;
    private bool isTransitioning;

    private GameObject player;
    private List<MonoBehaviour> playerScripts = new List<MonoBehaviour>();

    private HashSet<string> loadedMaps = new HashSet<string>();

    // ✅ Flag để kiểm tra cutscene đã bắt đầu chưa
    private bool _cutsceneStarted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[SceneTransition] Không tìm thấy Player!");
            yield break;
        }

        playerScripts.Clear();
        playerScripts.AddRange(player.GetComponents<MonoBehaviour>());

        if (string.IsNullOrEmpty(currentMapName))
        {
            yield return LoadFirstMap(initialMapName, initialSpawnID);
        }
    }

    private IEnumerator LoadFirstMap(string newMapName, string spawnPointID)
    {
        isTransitioning = true;
        _cutsceneStarted = false;

        // ✅ Hiện Loading UI trước khi load map
        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Show(0f);
        else
            Debug.LogError("[SceneTransition] LoadingUI.Instance is null!");

        Debug.Log($"[SceneTransition] Load map đầu tiên: '{newMapName}'");

        AsyncOperation load = SceneManager.LoadSceneAsync(newMapName, LoadSceneMode.Additive);
        while (!load.isDone)
        {
            float progress = Mathf.Clamp01(load.progress / 0.9f);
            if (LoadingUI.Instance != null)
                LoadingUI.Instance.UpdateProgress(progress);
            yield return null;
        }

        Scene newScene = SceneManager.GetSceneByName(newMapName);
        if (!newScene.IsValid())
        {
            Debug.LogError($"[SceneTransition] Không tìm thấy scene: {newMapName}");
            isTransitioning = false;
            if (LoadingUI.Instance != null)
                LoadingUI.Instance.Hide();
            yield break;
        }
        SceneManager.SetActiveScene(newScene);

        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;
        if (!FindSpawnPoint(newScene, spawnPointID, out targetPosition, out targetRotation))
            FindSpawnPoint(newScene, null, out targetPosition, out targetRotation);

        TeleportPlayerTo(targetPosition, targetRotation);
        currentMapName = newMapName;
        loadedMaps.Add(newMapName);

        // ✅ Chờ 2 frame để các script trong map khởi tạo
        yield return null;
        yield return null;

        // ✅ Kiểm tra có CutsceneIntro không
        CutsceneIntro cutscene = FindObjectOfType<CutsceneIntro>();
        if (cutscene != null)
        {
            Debug.Log("[SceneTransition] Tìm thấy CutsceneIntro, chờ cutscene bắt đầu...");
            // Chờ cutscene gọi OnCutsceneStarted()
            yield return new WaitUntil(() => _cutsceneStarted);
            Debug.Log("[SceneTransition] Cutscene đã bắt đầu, ẩn Loading UI.");
            if (LoadingUI.Instance != null)
                LoadingUI.Instance.Hide();
        }
        else
        {
            Debug.Log("[SceneTransition] Không có CutsceneIntro, ẩn Loading UI ngay.");
            if (LoadingUI.Instance != null)
                LoadingUI.Instance.Hide();
        }

        isTransitioning = false;
        Debug.Log($"[SceneTransition] ✅ Đã load map: {newMapName}");
    }

    public void TransitionToMap(string mapName, string spawnPointID = null, Action onComplete = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransition] Đang chuyển map, bỏ qua yêu cầu.");
            return;
        }
        if (string.IsNullOrEmpty(mapName))
        {
            Debug.LogError("[SceneTransition] mapName rỗng!");
            return;
        }
        if (mapName == currentMapName)
        {
            Debug.Log($"[SceneTransition] Đã ở map {mapName}, bỏ qua.");
            return;
        }

        StartCoroutine(TransitionCoroutine(mapName, spawnPointID, onComplete));
    }

    private IEnumerator TransitionCoroutine(string newMapName, string spawnPointID, Action onComplete)
    {
        isTransitioning = true;
        _cutsceneStarted = false;

        // ✅ Hiện Loading UI
        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Show(0f);

        Debug.Log($"[SceneTransition] Bắt đầu chuyển từ '{currentMapName}' -> '{newMapName}'");

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();
        else
            yield return new WaitForSeconds(fadeDuration);

        bool mapAlreadyLoaded = loadedMaps.Contains(newMapName);
        Scene newScene = SceneManager.GetSceneByName(newMapName);

        if (mapAlreadyLoaded && newScene.IsValid())
        {
            Debug.Log($"[SceneTransition] Map '{newMapName}' đã load trước đó. Active lại...");
            foreach (var root in newScene.GetRootGameObjects())
            {
                root.SetActive(true);
            }
        }
        else
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(newMapName, LoadSceneMode.Additive);
            while (!load.isDone)
            {
                float progress = Mathf.Clamp01(load.progress / 0.9f);
                if (LoadingUI.Instance != null)
                    LoadingUI.Instance.UpdateProgress(progress);
                yield return null;
            }

            newScene = SceneManager.GetSceneByName(newMapName);
            if (!newScene.IsValid())
            {
                Debug.LogError($"[SceneTransition] Không tìm thấy scene: {newMapName}");
                isTransitioning = false;
                if (LoadingUI.Instance != null)
                    LoadingUI.Instance.Hide();
                yield break;
            }
            loadedMaps.Add(newMapName);
        }

        // Deactivate map cũ
        if (!string.IsNullOrEmpty(currentMapName))
        {
            Scene oldScene = SceneManager.GetSceneByName(currentMapName);
            if (oldScene.IsValid())
            {
                foreach (var root in oldScene.GetRootGameObjects())
                {
                    if (!root.CompareTag("Persistent") && root.name != "PersistentContainer")
                    {
                        root.SetActive(false);
                    }
                }
                Debug.Log($"[SceneTransition] Deactivated old map: {currentMapName}");
            }
        }

        SceneManager.SetActiveScene(newScene);
        Debug.Log($"[SceneTransition] Active scene: {newMapName}");

        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;
        bool spawnFound = FindSpawnPoint(newScene, spawnPointID, out targetPosition, out targetRotation);

        if (!spawnFound)
        {
            spawnFound = FindSpawnPoint(newScene, null, out targetPosition, out targetRotation);
        }

        if (spawnFound)
        {
            Debug.Log($"[SceneTransition] Spawn point tìm thấy tại {targetPosition}");
            TeleportPlayerTo(targetPosition, targetRotation);
        }
        else
        {
            Debug.LogError($"[SceneTransition] KHÔNG TÌM THẤY SPAWN POINT trong scene {newMapName}!");
        }

        currentMapName = newMapName;

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeFromBlack();
        else
            yield return new WaitForSeconds(fadeDuration);

        yield return null;
        yield return null;

        if (player != null)
        {
            Vector3 finalPos = player.transform.position;
            if (Vector3.Distance(finalPos, targetPosition) > 0.5f)
            {
                Debug.LogWarning($"[SceneTransition] Player vẫn ở sai vị trí! Đang teleport lần cuối...");
                TeleportPlayerTo(targetPosition, targetRotation);
            }
        }

        // ✅ Kiểm tra CutsceneIntro
        CutsceneIntro cutscene = FindObjectOfType<CutsceneIntro>();
        if (cutscene != null)
        {
            Debug.Log("[SceneTransition] Tìm thấy CutsceneIntro, chờ cutscene bắt đầu...");
            yield return new WaitUntil(() => _cutsceneStarted);
            Debug.Log("[SceneTransition] Cutscene đã bắt đầu, ẩn Loading UI.");
            if (LoadingUI.Instance != null)
                LoadingUI.Instance.Hide();
        }
        else
        {
            Debug.Log("[SceneTransition] Không có CutsceneIntro, ẩn Loading UI ngay.");
            if (LoadingUI.Instance != null)
                LoadingUI.Instance.Hide();
        }

        isTransitioning = false;
        onComplete?.Invoke();
        Debug.Log($"[SceneTransition] ✅ Đã chuyển sang map: {newMapName}");
    }

    // ─── Public method để CutsceneIntro báo hiệu bắt đầu ──────────

    public void OnCutsceneStarted()
    {
        _cutsceneStarted = true;
        Debug.Log("[SceneTransition] Cutscene đã bắt đầu (được gọi từ CutsceneIntro).");
        // Ẩn Loading UI ngay lập tức
        if (LoadingUI.Instance != null)
            LoadingUI.Instance.Hide();
    }

    // ─── Helper Methods ──────────────────────────────────────────────

    private bool FindSpawnPoint(Scene scene, string spawnID, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        SpawnPoint[] allSpawns = UnityEngine.Object.FindObjectsOfType<SpawnPoint>();
        SpawnPoint targetSpawn = null;

        if (!string.IsNullOrEmpty(spawnID))
        {
            foreach (var sp in allSpawns)
            {
                if (sp.gameObject.scene == scene && sp.spawnID == spawnID)
                {
                    targetSpawn = sp;
                    break;
                }
            }
            if (targetSpawn != null)
            {
                Debug.Log($"[SceneTransition] Tìm thấy SpawnPoint ID '{spawnID}' tại {targetSpawn.transform.position}");
            }
            else
            {
                Debug.LogWarning($"[SceneTransition] Không tìm thấy SpawnPoint ID '{spawnID}'. Tìm spawn mặc định.");
            }
        }

        if (targetSpawn == null)
        {
            foreach (var sp in allSpawns)
            {
                if (sp.gameObject.scene == scene)
                {
                    targetSpawn = sp;
                    Debug.Log($"[SceneTransition] Dùng spawn mặc định '{targetSpawn.spawnID}' tại {targetSpawn.transform.position}");
                    break;
                }
            }
        }

        if (targetSpawn == null)
        {
            Debug.LogWarning($"[SceneTransition] Không tìm thấy SpawnPoint nào trong scene {scene.name}!");
            return false;
        }

        position = targetSpawn.transform.position;
        rotation = targetSpawn.transform.rotation;
        return true;
    }

    private void TeleportPlayerTo(Vector3 position, Quaternion rotation)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[SceneTransition] Không tìm thấy Player!");
                return;
            }
        }

        Debug.Log($"[SceneTransition] Bắt đầu teleport player đến {position}");

        List<MonoBehaviour> scripts = new List<MonoBehaviour>(player.GetComponents<MonoBehaviour>());
        foreach (var script in scripts)
        {
            if (script != null && script.enabled)
            {
                script.enabled = false;
            }
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        bool ccWasEnabled = false;
        if (cc != null)
        {
            ccWasEnabled = cc.enabled;
            if (cc.enabled)
            {
                cc.enabled = false;
            }
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        bool rbWasKinematic = false;
        bool rbWasGravity = false;
        if (rb != null)
        {
            rbWasKinematic = rb.isKinematic;
            rbWasGravity = rb.useGravity;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.position = position;
        player.transform.rotation = rotation;

        Physics.SyncTransforms();

        if (cc != null && ccWasEnabled)
        {
            cc.enabled = true;
            cc.Move(Vector3.zero);
        }

        if (rb != null)
        {
            rb.isKinematic = rbWasKinematic;
            rb.useGravity = rbWasGravity;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        StartCoroutine(EnablePlayerScriptsDelayed(scripts));
        Debug.Log($"[SceneTransition] Player đặt tại {position}");
    }

    private IEnumerator EnablePlayerScriptsDelayed(List<MonoBehaviour> scripts)
    {
        yield return null;
        yield return null;

        foreach (var script in scripts)
        {
            if (script != null && !script.enabled)
            {
                script.enabled = true;
            }
        }
        Debug.Log("[SceneTransition] Player scripts re-enabled.");
    }

    public string GetCurrentMapName() => currentMapName;
}