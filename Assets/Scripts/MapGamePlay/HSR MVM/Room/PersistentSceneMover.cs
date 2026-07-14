using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomTransitionManager : MonoBehaviour
{
    public static RoomTransitionManager Instance;

    [Header("Camera Player (Trong PersistentScene)")]
    public GameObject playerCamera; // Kéo Camera_Player vào đây

    [Header("Player Transform")]
    public Transform playerTransform; // Kéo GameObject Player vào đây

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Hàm được gọi từ Cửa Map khi muốn vào nhà
    public void EnterRoom(string roomSceneName, Transform doorSpawnPoint)
    {
        SceneManager.sceneLoaded += OnRoomLoaded_Enter; // Đăng ký sự kiện
        SceneManager.LoadScene(roomSceneName, LoadSceneMode.Additive); // Load phòng

        // Lưu tạm vị trí cửa để dùng sau
        tempSpawnPoint = doorSpawnPoint;
    }

    private Transform tempSpawnPoint;

    // Tự động chạy ngay khi Scene Phòng load xong
    private void OnRoomLoaded_Enter(Scene scene, LoadSceneMode mode)
    {
        // Chỉ thực hiện nếu Scene vừa load là Phòng
        if (scene.name != "Map1") 
        {
            // 1. Tắt Camera Player ngoài Map
            if (playerCamera != null) playerCamera.SetActive(false);

            // 2. Tìm Camera Phòng để bật (Vì nó mới được load)
            GameObject roomCamObj = GameObject.Find("Camera_Room");
            if (roomCamObj != null) roomCamObj.SetActive(true);

            // 3. Dời Player đến đúng vị trí trước cửa (dùng tempSpawnPoint)
            if (playerTransform != null && tempSpawnPoint != null)
            {
                playerTransform.position = tempSpawnPoint.position;
                playerTransform.rotation = tempSpawnPoint.rotation;
            }

            // Hủy đăng ký sự kiện để không chạy lại
            SceneManager.sceneLoaded -= OnRoomLoaded_Enter;
        }
    }

    // Hàm gọi từ Cửa Phòng khi muốn ra ngoài
    public void ExitRoom(string currentRoomName)
    {
        // 1. Bật lại Camera Player ngoài Map
        if (playerCamera != null) playerCamera.SetActive(true);

        // 2. Dời Player đến vị trí trước cửa Map (Việc này giao cho DoorInteraction ở Phòng xử lý)
        // 3. Gỡ Scene Phòng
        SceneManager.UnloadSceneAsync(currentRoomName);
    }
}