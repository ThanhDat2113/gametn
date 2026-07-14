using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    [Header("Camera Player (Kéo từ PersistentScene vào đây)")]
    public GameObject playerCamera; // Kéo GameObject Camera_Player vào đây

    private void Start()
    {
        // Khi Scene Phòng vừa load xong:
        // Tắt Camera Player ngoài Map
        if (playerCamera != null) playerCamera.SetActive(false);

        // Camera Room vẫn đang Bật (vì trong Inspector nó đã được tích enabled)
        // Bạn có thể tùy chỉnh thêm nếu cần
    }

    // Hàm gọi khi Player bấm E ra khỏi nhà (Gọi thủ công trong Inspector)
    public void OnExitRoom()
    {
        // Bật lại Camera Player ngoài Map
        if (playerCamera != null) playerCamera.SetActive(true);
    }
}