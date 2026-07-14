using UnityEngine;

public class GlobalCameraSwitcher : MonoBehaviour
{
    public static GlobalCameraSwitcher Instance;
    public Camera mainCamera; // Kéo Main Camera trong BootScene vào đây

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SwitchToCamera(Camera newCamera)
    {
        // Copy vị trí và góc quay từ Camera mới (từ Map hoặc từ Phòng) cho Main Camera
        mainCamera.transform.position = newCamera.transform.position;
        mainCamera.transform.rotation = newCamera.transform.rotation;
        mainCamera.orthographicSize = newCamera.orthographicSize;
    }
}