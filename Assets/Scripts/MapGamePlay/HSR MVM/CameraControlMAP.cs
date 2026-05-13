using UnityEngine;

public class HSRCameraController : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;        // Kéo Player vào đây
    
    [Header("Positioning")]
    public float distance = 15f;    // Khoảng cách từ camera tới player
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Offset để nhìn vào thân/đầu nhân vật thay vì dưới chân
    
    [Header("Smoothing")]
    public float smoothTime = 0.2f;
    private Vector3 _currentVelocity = Vector3.zero;

    void LateUpdate()
    {
        if (!target) return;

        // 1. Lấy vị trí mục tiêu mà camera muốn nhìn vào (thường là người chơi + một chút chiều cao)
        Vector3 lookAtPos = target.position + targetOffset;

        // 2. Tính toán hướng "lùi lại" dựa trên Rotation hiện tại của Camera
        // transform.forward là hướng camera đang nhìn, nhân với -distance để lùi ra sau
        Vector3 desiredPosition = lookAtPos - (transform.forward * distance);

        // 3. Di chuyển mượt mà tới vị trí đó
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, smoothTime);
    }
}