using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton quản lý chuyển đổi giữa các map (additive).
/// Đặt trong Persistent Scene.
/// </summary>
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

    // Cache player
    private GameObject player;
    private List<MonoBehaviour> playerScripts = new List<MonoBehaviour>();

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
            Debug.LogError("[SceneTransition] Không tìm thấy Player! Hãy đảm bảo có GameObject với tag 'Player' trong Persistent Scene.");
            yield break;
        }

        // Lưu tất cả MonoBehaviour trên player (để tạm thời disable)
        playerScripts.Clear();
        playerScripts.AddRange(player.GetComponents<MonoBehaviour>());

        // Load map đầu tiên
        if (string.IsNullOrEmpty(currentMapName))
        {
            yield return TransitionCoroutine(initialMapName, initialSpawnID, null);
        }
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

        Debug.Log($"[SceneTransition] Bắt đầu chuyển từ '{currentMapName}' -> '{newMapName}'");

        // 1. Fade to black
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();
        else
            yield return new WaitForSeconds(fadeDuration);

        // 2. Unload map cũ
        if (!string.IsNullOrEmpty(currentMapName))
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentMapName);
            yield return unload;
            Debug.Log($"[SceneTransition] Unloaded map: {currentMapName}");
        }

        // 3. Load map mới
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
        Debug.Log($"[SceneTransition] Active scene: {newMapName}");

        // 4. Đưa player đến spawn point (sau khi scene đã active và các Awake/Start đã chạy)
        Vector3 targetPosition = Vector3.zero;
        Quaternion targetRotation = Quaternion.identity;
        bool spawnFound = FindSpawnPoint(newScene, spawnPointID, out targetPosition, out targetRotation);

        if (!spawnFound)
        {
            // Thử tìm spawn mặc định
            spawnFound = FindSpawnPoint(newScene, null, out targetPosition, out targetRotation);
        }

        if (spawnFound)
        {
            Debug.Log($"[SceneTransition] Spawn point tìm thấy tại {targetPosition}");
            // Teleport ngay lập tức
            TeleportPlayerTo(targetPosition, targetRotation);
        }
        else
        {
            Debug.LogError($"[SceneTransition] KHÔNG TÌM THẤY SPAWN POINT trong scene {newMapName}!");
        }

        // 5. Cập nhật currentMapName
        currentMapName = newMapName;

        // 6. Fade from black
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeFromBlack();
        else
            yield return new WaitForSeconds(fadeDuration);

        // 7. Chờ 2 frame để tất cả các script khác update
        yield return null;
        yield return null;

        // 8. Kiểm tra vị trí cuối cùng
        if (player != null)
        {
            Vector3 finalPos = player.transform.position;
            Debug.Log($"[SceneTransition] Vị trí Player cuối cùng: {finalPos} (dự kiến: {targetPosition})");
            if (Vector3.Distance(finalPos, targetPosition) > 0.5f)
            {
                Debug.LogWarning($"[SceneTransition] Player vẫn ở sai vị trí! Đang teleport lần cuối...");
                TeleportPlayerTo(targetPosition, targetRotation);
            }
        }

        isTransitioning = false;
        onComplete?.Invoke();
        Debug.Log($"[SceneTransition] ✅ Đã chuyển sang map: {newMapName}");
    }

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

        // 1. Tạm thời vô hiệu hóa tất cả script trên player (trừ Transform)
        List<MonoBehaviour> scripts = new List<MonoBehaviour>(player.GetComponents<MonoBehaviour>());
        foreach (var script in scripts)
        {
            if (script != null && script.enabled)
            {
                script.enabled = false;
            }
        }

        // 2. Xử lý CharacterController
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

        // 3. Xử lý Rigidbody
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

        // 4. Đặt vị trí
        player.transform.position = position;
        player.transform.rotation = rotation;

        // 5. Đồng bộ vật lý ngay lập tức
        Physics.SyncTransforms();

        // 6. Khôi phục CharacterController
        if (cc != null && ccWasEnabled)
        {
            cc.enabled = true;
            cc.Move(Vector3.zero); // Force update
        }

        // 7. Khôi phục Rigidbody
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

        // 8. Bật lại các script (trừ các script quan trọng có thể được bật lại sau)
        //     THAY VÌ BẬT LẠI TẤT CẢ, chỉ bật lại những script cần thiết (ví dụ: PlayerController)
        //     Nhưng đơn giản hơn, bật lại tất cả sau 1 frame.
        StartCoroutine(EnablePlayerScriptsDelayed(scripts));

        Debug.Log($"[SceneTransition] Player đặt tại {position}");
    }

    private IEnumerator EnablePlayerScriptsDelayed(List<MonoBehaviour> scripts)
    {
        // Chờ 2 frame để các hệ thống khác ổn định
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