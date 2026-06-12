using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string combatSceneName = "CombatScene";
    [SerializeField] private string loadingSceneName = "LoadingScene";

    [Header("Settings")]
    [Tooltip("Số frame tối đa xử lý load mỗi frame. Càng thấp càng ít giật nhưng load lâu hơn.")]
    public int maxFrameMilliseconds = 50;

    private bool isCombatLoaded = false;

    public static GameObject MapRoot { get; set; }

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void LoadCombatScene()
    {
        if (Instance != null && !Instance.isCombatLoaded)
            Instance.StartCoroutine(Instance.LoadAdditive());
    }

    private IEnumerator LoadAdditive()
    {
        // Bắt đầu load combat scene ở chế độ nền
        AsyncOperation async = SceneManager.LoadSceneAsync(combatSceneName, LoadSceneMode.Additive);

        // Ngăn scene tự động active cho đến khi load xong
        async.allowSceneActivation = false;

        // Hiển thị loading indicator trong quá trình load
        // (nếu có FadeController, giữ màn đen)
        Debug.Log("[SceneLoaderManager] Loading combat scene additively...");

        // Load với yield và theo dõi progress
        while (async.progress < 0.9f)
        {
            // Chia nhỏ thời gian xử lý để không block main thread
            if (Time.deltaTime * 1000f > maxFrameMilliseconds)
                yield return null;
            else
                yield return new WaitForEndOfFrame();
        }

        // Load gần xong (progress >= 0.9), cho phép activation
        async.allowSceneActivation = true;

        // Chờ activation hoàn tất
        while (!async.isDone)
            yield return null;

        isCombatLoaded = true;
        Scene combatScene = SceneManager.GetSceneByName(combatSceneName);
        SceneManager.SetActiveScene(combatScene);
        Debug.Log("[SceneLoaderManager] Combat scene loaded and set active.");
    }

    public static void UnloadCombatScene()
    {
        if (Instance != null && Instance.isCombatLoaded)
            Instance.StartCoroutine(Instance.UnloadAdditive());
    }

    private IEnumerator UnloadAdditive()
    {
        // Giữ màn đen trong khi unload
        Debug.Log("[SceneLoaderManager] Unloading combat scene...");

        AsyncOperation async = SceneManager.UnloadSceneAsync(combatSceneName);

        // Chia nhỏ frame để tránh giật
        while (!async.isDone)
        {
            if (Time.deltaTime * 1000f > maxFrameMilliseconds)
                yield return null;
            else
                yield return new WaitForEndOfFrame();
        }

        isCombatLoaded = false;

        // Kích hoạt lại map
        if (MapRoot != null)
        {
            MapRoot.SetActive(true);
            Debug.Log("[SceneLoaderManager] MapRoot activated.");
        }
        else
        {
            Debug.LogWarning("[SceneLoaderManager] MapRoot reference is null, trying to find by name...");
            Scene mapScene = SceneManager.GetSceneAt(0);
            GameObject[] roots = mapScene.GetRootGameObjects();
            foreach (var obj in roots)
            {
                if (obj.name == "MapRoot")
                {
                    obj.SetActive(true);
                    break;
                }
            }
        }

        // Fade in
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeFromBlack();
    }

    public static void ReloadCombatScene()
    {
        if (Instance != null && Instance.isCombatLoaded)
            Instance.StartCoroutine(Instance.ReloadAdditive());
    }

    private IEnumerator ReloadAdditive()
    {
        // Unload current combat scene
        AsyncOperation unload = SceneManager.UnloadSceneAsync(combatSceneName);
        while (!unload.isDone)
        {
            if (Time.deltaTime * 1000f > maxFrameMilliseconds)
                yield return null;
            else
                yield return new WaitForEndOfFrame();
        }

        isCombatLoaded = false;

        // Load lại với async
        AsyncOperation load = SceneManager.LoadSceneAsync(combatSceneName, LoadSceneMode.Additive);
        load.allowSceneActivation = false;

        while (load.progress < 0.9f)
        {
            if (Time.deltaTime * 1000f > maxFrameMilliseconds)
                yield return null;
            else
                yield return new WaitForEndOfFrame();
        }

        load.allowSceneActivation = true;
        while (!load.isDone)
            yield return null;

        isCombatLoaded = true;
        Scene combatScene = SceneManager.GetSceneByName(combatSceneName);
        SceneManager.SetActiveScene(combatScene);
        Debug.Log("[SceneLoaderManager] Combat scene reloaded.");
    }
}
