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

    // ─── LOAD MAP LẦN ĐẦU (có Loading UI) ────────────────────────

    private IEnumerator LoadFirstMap(string newMapName, string spawnPointID)
    {
        isTransitioning = true;

        // ✅ Hiển thị loading UI
        if (LoadingUIManager.Instance != null)
            LoadingUIManager.Instance.ShowLoading("Đang tải game...", true);

        Debug.Log($"[SceneTransition] Load map đầu tiên: '{newMapName}'");

        AsyncOperation load = SceneManager.LoadSceneAsync(newMapName, LoadSceneMode.Additive);
        yield return load;

        Scene newScene = SceneManager.GetSceneByName(newMapName);
        if (!newScene.IsValid())
        {
            Debug.LogError($"[SceneTransition] Không tìm thấy scene: {newMapName}");
            isTransitioning = false;
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

        yield return null;
        yield return null;

        // ✅ Ẩn loading UI (CutsceneIntro sẽ tự ẩn khi bắt đầu)
        if (LoadingUIManager.Instance != null)
            LoadingUIManager.Instance.HideLoading();

        isTransitioning = false;
        Debug.Log($"[SceneTransition] ✅ Đã load map: {newMapName}");
    }

    // ─── CHUYỂN MAP (có thể tùy chọn loading UI) ──────────────────

    public void TransitionToMap(string mapName, string spawnPointID = null, Action onComplete = null)
    {
        TransitionToMap(mapName, spawnPointID, false, onComplete);
    }

    /// <summary>
    /// Chuyển đến map khác.
    /// </summary>
    /// <param name="mapName">Tên map cần chuyển</param>
    /// <param name="spawnPointID">ID spawn point (tùy chọn)</param>
    /// <param name="useLoadingUI">True: hiển thị Loading UI; False: chỉ dùng fade (mặc định cho Portal)</param>
    /// <param name="onComplete">Callback khi hoàn thành</param>
    public void TransitionToMap(string mapName, string spawnPointID, bool useLoadingUI, Action onComplete = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneTransition] Đang chuyển map, bỏ qua yêu cầu đến '{mapName}'.");
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

        StartCoroutine(TransitionCoroutine(mapName, spawnPointID, useLoadingUI, onComplete));
    }

    private IEnumerator TransitionCoroutine(string newMapName, string spawnPointID, bool useLoadingUI, Action onComplete)
    {
        isTransitioning = true;

        Debug.Log($"[SceneTransition] Bắt đầu chuyển từ '{currentMapName}' -> '{newMapName}'");

        // ✅ Nếu dùng Loading UI thì hiển thị
        if (useLoadingUI && LoadingUIManager.Instance != null)
            LoadingUIManager.Instance.ShowLoading($"Đang tải {newMapName}...", true);

        // 1. Fade to black
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();
        else
            yield return new WaitForSeconds(fadeDuration);

        // 2. Kiểm tra xem map mới đã load chưa
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
            yield return load;

            newScene = SceneManager.GetSceneByName(newMapName);
            if (!newScene.IsValid())
            {
                Debug.LogError($"[SceneTransition] Không tìm thấy scene: {newMapName}");
                isTransitioning = false;
                if (useLoadingUI && LoadingUIManager.Instance != null)
                    LoadingUIManager.Instance.HideLoading();
                yield break;
            }
            loadedMaps.Add(newMapName);
        }

        // 3. Unload/Deactivate map cũ
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

        // 4. Spawn point
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

        // 5. Fade from black
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeFromBlack();
        else
            yield return new WaitForSeconds(fadeDuration);

        // 6. Chờ 2 frame để ổn định
        yield return null;
        yield return null;

        // 7. Kiểm tra vị trí cuối cùng
        if (player != null)
        {
            Vector3 finalPos = player.transform.position;
            if (Vector3.Distance(finalPos, targetPosition) > 0.5f)
            {
                Debug.LogWarning($"[SceneTransition] Player vẫn ở sai vị trí! Đang teleport lần cuối...");
                TeleportPlayerTo(targetPosition, targetRotation);
            }
        }

        // ✅ Nếu dùng Loading UI thì ẩn đi
        if (useLoadingUI && LoadingUIManager.Instance != null)
            LoadingUIManager.Instance.HideLoading();

        isTransitioning = false;
        onComplete?.Invoke();
        Debug.Log($"[SceneTransition] ✅ Đã chuyển sang map: {newMapName}");
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

        Debug.Log($"[SceneTransition] Teleport player đến {position}");

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

        if (player != null)
        {
            HSRPlayerController hsr = player.GetComponent<HSRPlayerController>();
            if (hsr != null && !hsr.enabled)
            {
                hsr.enabled = true;
                Debug.Log("[SceneTransition] HSRPlayerController đã được bật lại.");
            }
        }

        Debug.Log("[SceneTransition] Player scripts re-enabled.");
    }

    public string GetCurrentMapName() => currentMapName;
}