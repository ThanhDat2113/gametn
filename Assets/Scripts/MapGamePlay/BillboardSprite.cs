using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Xoay mặt Sprite đối diện thẳng với Camera
        transform.forward = mainCamera.transform.forward;
        
        // Hoặc sử dụng dòng dưới nếu muốn Sprite không bị nghiêng ngả trục X/Z
        // transform.rotation = Quaternion.Euler(0f, mainCamera.transform.rotation.eulerAngles.y, 0f);
    }
}