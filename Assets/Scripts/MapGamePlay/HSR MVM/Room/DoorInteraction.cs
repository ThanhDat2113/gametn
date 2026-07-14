using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteraction : MonoBehaviour
{
    [Header("Cài đặt cửa")]
    public string targetSceneName; // Tên Scene Phòng (chỉ điền ở cửa ngoài Map)
    
    public Transform playerSpawnPointInside; // Vị trí Player vào phòng (kéo từ Scene Phòng)
    public Transform playerSpawnPointOutside; // Vị trí Player ra map (kéo từ Scene Map1)

    [Header("Map Root (Chỉ kéo ở cửa ngoài Map)")]
    public GameObject mapRootObject; // Kéo MapRoot từ Scene Map1 vào đây (cửa ngoài Map thôi)

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            // 1. Đang ở ngoài Map1 -> Vào phòng
            if (this.gameObject.scene.name == "Map1") 
            {
                // A. Tắt MapRoot
                if (mapRootObject != null) mapRootObject.SetActive(false);

                // B. Load Scene Phòng
                SceneManager.LoadScene(targetSceneName, LoadSceneMode.Additive);
                
                // C. Đăng ký sự kiện load xong để set vị trí Player
                SceneManager.sceneLoaded += OnRoomLoaded;
            }
            // 2. Đang ở trong Phòng -> Ra Map1
            else 
            {
                // A. QUAN TRỌNG: Tìm MapRoot thông qua Scene Map1 (Không cần Tag, không cần Find)
                Scene mapScene = SceneManager.GetSceneByName("Map1");
                if (mapScene.IsValid())
                {
                    // Lấy tất cả GameObject gốc trong Scene Map1
                    GameObject[] rootObjects = mapScene.GetRootGameObjects();
                    
                    // Duyệt để tìm thằng tên là MapRoot
                    foreach (GameObject obj in rootObjects)
                    {
                        if (obj.name == "MapRoot")
                        {
                            obj.SetActive(true); // Bật nó lên
                            break;
                        }
                    }
                }

                // B. Đưa Player về vị trí trước cửa ngoài Map
                if (playerSpawnPointOutside != null)
                {
                    other.transform.position = playerSpawnPointOutside.position;
                    other.transform.rotation = playerSpawnPointOutside.rotation;
                }

                // C. Gỡ Scene Phòng
                SceneManager.UnloadSceneAsync(this.gameObject.scene.name);
            }
        }
    }

    // Hàm chạy khi Scene Phòng vừa load xong
    private void OnRoomLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == targetSceneName)
        {
            // Đưa Player đến đúng vị trí spawn trong phòng
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null && playerSpawnPointInside != null)
            {
                playerObj.transform.position = playerSpawnPointInside.position;
                playerObj.transform.rotation = playerSpawnPointInside.rotation;
            }

            // Hủy đăng ký sự kiện (để nó không chạy lại các lần sau)
            SceneManager.sceneLoaded -= OnRoomLoaded;
        }
    }
}