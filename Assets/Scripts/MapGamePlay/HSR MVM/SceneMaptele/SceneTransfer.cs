using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransfer : MonoBehaviour
{
    [Header("Cấu hình chuyển map")]
    [SerializeField] private string sceneNameToLoad;
    [SerializeField] private string spawnPointName;

    private void OnTriggerEnter(Collider other)
    {
        bool isPlayerTag = other.CompareTag("Player");
        bool hasCharacterController = other.GetComponent<CharacterController>() != null;

        if (isPlayerTag || hasCharacterController)
        {
            GameObject player = other.gameObject;
            GameObject mainCam = GameObject.FindWithTag("MainCamera");

            // MẸO CHÍ MẠNG: Ép nhân vật và camera thoát khỏi mọi Group cha để không bị xóa ké
            player.transform.SetParent(null);
            if (mainCam != null) mainCam.transform.SetParent(null);

            // Giữ lại sang map mới
            DontDestroyOnLoad(player);
            if (mainCam != null) DontDestroyOnLoad(mainCam);

            PlayerPrefs.SetString("NextSpawnPoint", spawnPointName);

            if (Application.CanStreamedLevelBeLoaded(sceneNameToLoad))
            {
                SceneManager.LoadScene(sceneNameToLoad);
            }
        }
    }
}