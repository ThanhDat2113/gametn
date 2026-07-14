using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteraction : MonoBehaviour
{
    [Header("Cài đặt cửa")]
    public string targetSceneName; // Tên Scene Phòng khi đi vào

    [Header("Camera (Chỉ kéo vào nếu đây là cửa Map -> Phòng)")]
    public Camera destinationCamera; // Kéo Camera Phòng vào đây (nếu là cửa ngoài Map)
    
    [Header("Tên Camera Map (Quan trọng)")]
    public string mapCameraName = "Camera_Map"; // Đặt đúng tên Camera Map của bạn ở đây

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            // 1. ĐANG Ở NGOÀI MAP -> BƯỚC VÀO NHÀ
            if (this.gameObject.scene.name == "Map1") 
            {
                if (destinationCamera != null)
                {
                    GlobalCameraSwitcher.Instance.SwitchToCamera(destinationCamera);
                }
                SceneManager.LoadScene(targetSceneName, LoadSceneMode.Additive);
            }
            // 2. ĐANG Ở TRONG NHÀ -> BƯỚC RA NGOÀI MAP
            else 
            {
                // Tìm Camera Map bằng tên chính xác (lấy từ ô Inspector để an toàn)
                GameObject mapCamObj = GameObject.Find(mapCameraName);
                
                if (mapCamObj != null)
                {
                    Camera mapCam = mapCamObj.GetComponent<Camera>();
                    if (mapCam != null)
                    {
                        GlobalCameraSwitcher.Instance.SwitchToCamera(mapCam);
                    }
                }

                // Gỡ bỏ Scene phòng ra khỏi bộ nhớ
                SceneManager.UnloadSceneAsync(this.gameObject.scene.name);
            }
        }
    }
}