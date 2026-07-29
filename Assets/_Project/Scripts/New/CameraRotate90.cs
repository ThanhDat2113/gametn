using UnityEngine;

/// <summary>
/// Xoay camera 90 độ sang trái/phải khi nhấn Z/X.
/// Gắn script này lên Camera.
/// </summary>
public class CameraRotate90 : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Tốc độ xoay (độ/giây). Để 0 là xoay tức thì.")]
    public float rotationSpeed = 180f; // độ/giây

    [Tooltip("Nếu true, xoay mượt (lerp). Nếu false, xoay tức thì.")]
    public bool smoothRotation = true;

    [Header("Keys")]
    public KeyCode rotateLeftKey = KeyCode.Z;
    public KeyCode rotateRightKey = KeyCode.X;

    private float targetYRotation = 0f;
    private Quaternion targetRotation;

    private void Start()
    {
        // Lưu góc xoay hiện tại làm mục tiêu ban đầu
        targetYRotation = transform.eulerAngles.y;
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        // Xoay trái 90 độ
        if (Input.GetKeyDown(rotateLeftKey))
        {
            targetYRotation -= 90f;
            // Đảm bảo góc trong khoảng 0-360 để tránh tích lũy
            targetYRotation = NormalizeAngle(targetYRotation);
            if (!smoothRotation)
                ApplyRotationImmediate();
        }

        // Xoay phải 90 độ
        if (Input.GetKeyDown(rotateRightKey))
        {
            targetYRotation += 90f;
            targetYRotation = NormalizeAngle(targetYRotation);
            if (!smoothRotation)
                ApplyRotationImmediate();
        }

        // Nếu có smooth, cập nhật mỗi frame
        if (smoothRotation)
        {
            Quaternion currentRot = transform.rotation;
            Quaternion desiredRot = Quaternion.Euler(transform.eulerAngles.x, targetYRotation, transform.eulerAngles.z);
            transform.rotation = Quaternion.RotateTowards(currentRot, desiredRot, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Áp dụng xoay tức thì về góc mục tiêu.
    /// </summary>
    private void ApplyRotationImmediate()
    {
        Vector3 euler = transform.eulerAngles;
        euler.y = targetYRotation;
        transform.eulerAngles = euler;
    }

    /// <summary>
    /// Đưa góc về khoảng 0-360 độ.
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }
}