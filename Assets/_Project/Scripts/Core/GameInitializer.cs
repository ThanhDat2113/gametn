using UnityEngine;

/// <summary>
/// Tự động tạo các singleton cần thiết khi game khởi động.
/// Gắn script này vào GameObject bất kỳ trong scene đầu tiên (MainMenu, Intro, Map...).
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Auto-Create Singletons")]
    [Tooltip("Nếu bật, sẽ tự động tạo PlayerProgression nếu chưa tồn tại")]
    public bool createPlayerProgression = true;

    private void Awake()
    {
        // 1. PlayerProgression
        if (createPlayerProgression && PlayerProgression.Instance == null)
        {
            var go = new GameObject("PlayerProgression");
            go.AddComponent<PlayerProgression>();
            DontDestroyOnLoad(go);
            Debug.Log("[GameInitializer] Đã tự động tạo PlayerProgression singleton.");
        }

        // 2. SceneLoaderManager (cần cho load/unload combat scene)
        if (SceneLoaderManager.Instance == null)
        {
            var go = new GameObject("SceneLoaderManager");
            go.AddComponent<SceneLoaderManager>();
            DontDestroyOnLoad(go);
            Debug.Log("[GameInitializer] Đã tự động tạo SceneLoaderManager singleton.");
        }
    }
}