using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance { get; private set; }

    [SerializeField] private string combatSceneName = "CombatScene";
    private bool isCombatLoaded = false;

    public static GameObject MapRoot { get; set; }
    public static GameObject PersistentContainer { get; set; }

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
        AsyncOperation async = SceneManager.LoadSceneAsync(combatSceneName, LoadSceneMode.Additive);
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
        AsyncOperation async = SceneManager.UnloadSceneAsync(combatSceneName);
        while (!async.isDone)
            yield return null;

        isCombatLoaded = false;

        // Hiện lại map và persistent container
        if (MapRoot != null)
        {
            MapRoot.SetActive(true);
            Debug.Log("[SceneLoaderManager] MapRoot activated.");
        }

        if (PersistentContainer != null)
        {
            PersistentContainer.SetActive(true);
            Debug.Log("[SceneLoaderManager] PersistentContainer activated.");
        }

        // 🔥 Bật lại movement cho player
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.EnableMovement();
            Debug.Log("[SceneLoaderManager] Player movement enabled.");
        }

        // 🔥 Reset camera yaw về hướng gốc khi quay về map
        // (tránh bug: camera giữ nguyên góc xoay sau khi vào combat rồi nói chuyện NPC)
        var cameraController = Object.FindFirstObjectByType<HSRCameraController>();
        if (cameraController != null)
        {
            cameraController.ResetYaw();
            Debug.Log("[SceneLoaderManager] Camera yaw reset về 0.");
        }

        // Quay lại active scene là map hiện tại
        if (SceneTransitionManager.Instance != null)
        {
            string mapName = SceneTransitionManager.Instance.GetCurrentMapName();
            if (!string.IsNullOrEmpty(mapName))
            {
                Scene mapScene = SceneManager.GetSceneByName(mapName);
                if (mapScene.IsValid())
                    SceneManager.SetActiveScene(mapScene);
            }
        }

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeFromBlack();

        Debug.Log("[SceneLoaderManager] Combat scene unloaded.");
    }

    public static void ReloadCombatScene()
    {
        if (Instance != null && Instance.isCombatLoaded)
            Instance.StartCoroutine(Instance.ReloadAdditive());
    }

    private IEnumerator ReloadAdditive()
    {
        AsyncOperation unload = SceneManager.UnloadSceneAsync(combatSceneName);
        while (!unload.isDone)
            yield return null;

        isCombatLoaded = false;

        AsyncOperation load = SceneManager.LoadSceneAsync(combatSceneName, LoadSceneMode.Additive);
        while (!load.isDone)
            yield return null;

        isCombatLoaded = true;
        Scene combatScene = SceneManager.GetSceneByName(combatSceneName);
        SceneManager.SetActiveScene(combatScene);
        Debug.Log("[SceneLoaderManager] Combat scene reloaded.");
    }
}