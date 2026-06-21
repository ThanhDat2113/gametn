using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Gắn trên GameObject trong Boot Scene.
/// Tự động tạo các singleton và load Persistent Scene, sau đó unload Boot Scene.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Persistent Scene")]
    [Tooltip("Tên scene chứa các đối tượng tồn tại xuyên suốt game")]
    public string persistentSceneName = "PersistentScene";

    [Header("Auto-create Singletons (nếu chưa có)")]
    public bool createPlayerProgression = true;
    public bool createSceneLoaderManager = true;
    public bool createAudioManager = true;

    private void Awake()
    {
        // Tạo các singleton cần thiết (nếu chưa có)
        if (createPlayerProgression && PlayerProgression.Instance == null)
        {
            var go = new GameObject("PlayerProgression");
            go.AddComponent<PlayerProgression>();
            DontDestroyOnLoad(go);
            Debug.Log("[GameInitializer] Tạo PlayerProgression singleton.");
        }

        if (createSceneLoaderManager && SceneLoaderManager.Instance == null)
        {
            var go = new GameObject("SceneLoaderManager");
            go.AddComponent<SceneLoaderManager>();
            DontDestroyOnLoad(go);
            Debug.Log("[GameInitializer] Tạo SceneLoaderManager singleton.");
        }

        if (createAudioManager && AudioManager.Instance == null)
        {
            // AudioManager tự tạo nên chỉ cần gọi Instance để khởi tạo
            var audio = AudioManager.Instance;
            Debug.Log("[GameInitializer] Tạo AudioManager singleton.");
        }

        // Load Persistent Scene (additive)
        if (!SceneManager.GetSceneByName(persistentSceneName).IsValid())
        {
            SceneManager.LoadSceneAsync(persistentSceneName, LoadSceneMode.Additive);
            Debug.Log($"[GameInitializer] Load Persistent Scene: {persistentSceneName}");
        }

        // Unload Boot Scene sau khi load xong
        StartCoroutine(UnloadBootScene());
    }

    private IEnumerator UnloadBootScene()
    {
        // Chờ một vài frame để scene load
        yield return new WaitForSeconds(0.2f);
        AsyncOperation unload = SceneManager.UnloadSceneAsync(gameObject.scene);
        yield return unload;
        Debug.Log("[GameInitializer] Boot Scene unloaded.");
    }
}