using UnityEngine;

public class SceneBootstrapper : MonoBehaviour
{
    void Start()
    {
        // Kiểm tra xem có điểm Spawn nào được yêu cầu không
        if (PlayerPrefs.HasKey("NextSpawnPoint"))
        {
            string targetSpawnName = PlayerPrefs.GetString("NextSpawnPoint");
            GameObject spawnPoint = GameObject.Find(targetSpawnName);

            if (spawnPoint != null)
            {
                // Tìm nhân vật đã được giữ lại từ Scene cũ
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    // Dịch chuyển nhân vật về điểm Spawn mới
                    player.transform.position = spawnPoint.transform.position;
                    player.transform.rotation = spawnPoint.transform.rotation;
                    
                    Debug.Log("Đã xếp nhân vật vào điểm: " + targetSpawnName);
                }
            }
            // Xóa cache sau khi dùng xong
            PlayerPrefs.DeleteKey("NextSpawnPoint");
        }
    }
}