using UnityEngine;

public class HSRCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;        // Kéo Player vào đây

    [Header("Orbit Settings")]
    public float distance = 10f;
    public float yawSpeed = 80f;    // Tốc độ xoay ngang (độ/giây)
    public KeyCode rotateLeft = KeyCode.Q;
    public KeyCode rotateRight = KeyCode.E;

    [Header("Vertical Offset")]
    public float heightOffset = 1.5f; // Độ cao so với target

    [Header("Smoothing")]
    public float smoothTime = 0.2f;
    private float _currentYaw = 0f;
    private Vector3 _velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // Xoay khi giữ Q/E
        if (Input.GetKey(rotateLeft))
            _currentYaw -= yawSpeed * Time.deltaTime;
        if (Input.GetKey(rotateRight))
            _currentYaw += yawSpeed * Time.deltaTime;

        // Tính vị trí camera: xoay vector (0,0,-distance) quanh trục Y
        Quaternion rot = Quaternion.Euler(0, _currentYaw, 0);
        Vector3 desiredPos = target.position + new Vector3(0, heightOffset, 0) + rot * new Vector3(0, 0, -distance);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * heightOffset);
    }

    public void ResetYaw() => _currentYaw = 0f;
}