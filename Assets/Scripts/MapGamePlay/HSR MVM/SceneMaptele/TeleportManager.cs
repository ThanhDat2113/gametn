using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// Hệ thống quản lý teleport toàn cầu - xử lý tất cả logic chuyển map
/// </summary>
public class TeleportManager : MonoBehaviour
{
    [Serializable]
    public class TeleportData
    {
        public string targetScene;
        public string spawnPointName;
        public Vector3 positionOffset = Vector3.zero;
        public bool useCustomPosition = false;
        public Vector3 customPosition;
    }

    private static TeleportManager _instance;
    public static TeleportManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("TeleportManager");
                _instance = obj.AddComponent<TeleportManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private TeleportData currentTeleportData;
    private Action<bool> onTeleportComplete;
    private float retryInterval = 0.1f;
    private float maxRetryTime = 5f;

    [SerializeField] private bool debugMode = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Yêu cầu teleport đến một scene và spawn point
    /// </summary>
    public void RequestTeleport(string sceneName, string spawnPointName, Vector3 positionOffset = default, Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            LogError("Scene name không được để trống!");
            onComplete?.Invoke(false);
            return;
        }

        currentTeleportData = new TeleportData
        {
            targetScene = sceneName,
            spawnPointName = spawnPointName,
            positionOffset = positionOffset,
            useCustomPosition = false
        };

        onTeleportComplete = onComplete;
        PerformTeleport();
    }

    /// <summary>
    /// Yêu cầu teleport đến vị trí tùy chỉnh
    /// </summary>
    public void RequestTeleportToPosition(string sceneName, Vector3 targetPosition, Quaternion targetRotation = default, Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            LogError("Scene name không được để trống!");
            onComplete?.Invoke(false);
            return;
        }

        currentTeleportData = new TeleportData
        {
            targetScene = sceneName,
            useCustomPosition = true,
            customPosition = targetPosition
        };

        onTeleportComplete = onComplete;
        PerformTeleport();
    }

    private void PerformTeleport()
    {
        if (currentTeleportData == null)
        {
            LogError("Teleport data không hợp lệ!");
            return;
        }

        try
        {
            GameObject player = GameObject.FindWithTag("Player");
            GameObject mainCam = GameObject.FindWithTag("MainCamera");

            if (player == null)
            {
                LogError("Không tìm thấy Player!");
                onTeleportComplete?.Invoke(false);
                return;
            }

            // Tách nhân vật và camera khỏi parent để tránh bị xóa ké
            player.transform.SetParent(null);
            if (mainCam != null)
                mainCam.transform.SetParent(null);

            // Giữ lại các object qua scene
            DontDestroyOnLoad(player);
            if (mainCam != null)
                DontDestroyOnLoad(mainCam);

            // Lưu dữ liệu teleport vào PlayerPrefs
            PlayerPrefs.SetString("Teleport_Scene", currentTeleportData.targetScene);
            PlayerPrefs.SetString("Teleport_SpawnPoint", currentTeleportData.spawnPointName ?? "");
            PlayerPrefs.SetInt("Teleport_UseCustomPos", currentTeleportData.useCustomPosition ? 1 : 0);
            
            if (currentTeleportData.useCustomPosition)
            {
                PlayerPrefs.SetFloat("Teleport_PosX", currentTeleportData.customPosition.x);
                PlayerPrefs.SetFloat("Teleport_PosY", currentTeleportData.customPosition.y);
                PlayerPrefs.SetFloat("Teleport_PosZ", currentTeleportData.customPosition.z);
            }

            PlayerPrefs.SetFloat("Teleport_OffsetX", currentTeleportData.positionOffset.x);
            PlayerPrefs.SetFloat("Teleport_OffsetY", currentTeleportData.positionOffset.y);
            PlayerPrefs.SetFloat("Teleport_OffsetZ", currentTeleportData.positionOffset.z);
            PlayerPrefs.Save();

            Log($"🚀 Teleporting đến: {currentTeleportData.targetScene} | Spawn: {currentTeleportData.spawnPointName}");

            if (Application.CanStreamedLevelBeLoaded(currentTeleportData.targetScene))
            {
                SceneManager.LoadScene(currentTeleportData.targetScene);
            }
            else
            {
                LogError($"Scene '{currentTeleportData.targetScene}' không tồn tại hoặc không được load được!");
                onTeleportComplete?.Invoke(false);
            }
        }
        catch (Exception ex)
        {
            LogError($"Lỗi khi teleport: {ex.Message}");
            onTeleportComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Lấy dữ liệu teleport hiện tại và xóa cache
    /// </summary>
    public TeleportData GetAndClearTeleportData()
    {
        var data = new TeleportData();

        if (PlayerPrefs.HasKey("Teleport_Scene"))
        {
            data.targetScene = PlayerPrefs.GetString("Teleport_Scene");
            data.spawnPointName = PlayerPrefs.GetString("Teleport_SpawnPoint");
            data.useCustomPosition = PlayerPrefs.GetInt("Teleport_UseCustomPos", 0) == 1;

            data.positionOffset = new Vector3(
                PlayerPrefs.GetFloat("Teleport_OffsetX", 0f),
                PlayerPrefs.GetFloat("Teleport_OffsetY", 0f),
                PlayerPrefs.GetFloat("Teleport_OffsetZ", 0f)
            );

            if (data.useCustomPosition)
            {
                data.customPosition = new Vector3(
                    PlayerPrefs.GetFloat("Teleport_PosX", 0f),
                    PlayerPrefs.GetFloat("Teleport_PosY", 0f),
                    PlayerPrefs.GetFloat("Teleport_PosZ", 0f)
                );
            }

            // Xóa cache sau khi lấy
            ClearTeleportCache();
            return data;
        }

        return null;
    }

    private void ClearTeleportCache()
    {
        PlayerPrefs.DeleteKey("Teleport_Scene");
        PlayerPrefs.DeleteKey("Teleport_SpawnPoint");
        PlayerPrefs.DeleteKey("Teleport_UseCustomPos");
        PlayerPrefs.DeleteKey("Teleport_PosX");
        PlayerPrefs.DeleteKey("Teleport_PosY");
        PlayerPrefs.DeleteKey("Teleport_PosZ");
        PlayerPrefs.DeleteKey("Teleport_OffsetX");
        PlayerPrefs.DeleteKey("Teleport_OffsetY");
        PlayerPrefs.DeleteKey("Teleport_OffsetZ");
        PlayerPrefs.Save();
    }

    private void Log(string message)
    {
        if (debugMode)
            Debug.Log($"[TeleportManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[TeleportManager] ❌ {message}");
    }
}
