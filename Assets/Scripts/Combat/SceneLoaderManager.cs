using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance { get; private set; }

    [SerializeField] private string combatSceneName = "CombatScene";
    private bool isCombatLoaded = false;

    // Lưu tham chiếu đến MapRoot (sẽ được gán từ MapEnemy)
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
        AsyncOperation async = SceneManager.LoadSceneAsync(combatSceneName, LoadSceneMode.Additive);
        while (!async.isDone)
            yield return null;

        isCombatLoaded = true;
        Scene combatScene = SceneManager.GetSceneByName(combatSceneName);
        
        // ✅ QUAN TRỌNG: set combat scene làm active scene
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

        // ✅ Bật lại MapRoot dùng reference đã lưu
        if (MapRoot != null)
        {
            MapRoot.SetActive(true);
            Debug.Log("[SceneLoaderManager] MapRoot activated.");
        }
        else
        {
            Debug.LogWarning("[SceneLoaderManager] MapRoot reference is null, trying to find by name...");
            // Fallback: tìm trong scene gốc (index 0)
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

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeFromBlack();
    }
}