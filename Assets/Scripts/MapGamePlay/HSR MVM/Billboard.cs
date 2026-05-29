using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera currentCamera;

    void LateUpdate()
    {
        currentCamera = Camera.main;

        if (currentCamera == null) return;

        transform.LookAt(
            transform.position + currentCamera.transform.rotation * Vector3.forward,
            currentCamera.transform.rotation * Vector3.up
        );
    }
}