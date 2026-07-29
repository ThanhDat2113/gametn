using UnityEngine;

public class HSRCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;        // Kéo Player vào đây

    [Header("Orbit Settings")]
    public float distance = 25f;    // Khoảng cách từ camera đến player
    public float rotationStep = 90f; // Góc xoay mỗi lần nhấn (mặc định 90 độ)

    [Header("Vertical Offset")]
    public float heightOffset = 3f; // Độ cao camera so với player

    [Header("Smoothing")]
    public float smoothTime = 0.15f; // Thời gian làm mịn di chuyển
    private float _currentYaw = 0f;
    private Vector3 _velocity = Vector3.zero;

    [Header("Collision Settings (Né vật cản)")]
    public LayerMask obstacleLayers;   // Layer của tường/nhà
    public float cameraRadius = 0.3f;  // Bán kính khối cầu quét tường
    public float minDistance = 2.0f;   // Khoảng cách tối thiểu khi ép sát tường
    public float playerPivotHeight = 1.0f; // Độ cao trọng tâm cơ thể player

    // Phím tắt (có thể đổi trong Inspector)
    public KeyCode keyRotateLeft = KeyCode.Z;
    public KeyCode keyRotateRight = KeyCode.X;

    void LateUpdate()
    {
        if (target == null) return;

        // ═══════════════════════════════════════════════
        // Xoay 90 độ mỗi lần nhấn phím Z hoặc X
        // ═══════════════════════════════════════════════
        if (Input.GetKeyDown(keyRotateLeft))
            _currentYaw -= rotationStep;

        if (Input.GetKeyDown(keyRotateRight))
            _currentYaw += rotationStep;

        // 1. Điểm gốc thực tế trên cơ thể nhân vật (ngay ngực/bụng)
        Vector3 playerPivot = target.position + Vector3.up * playerPivotHeight;

        // 2. Tính vị trí LÝ TƯỞNG của Camera ngoài thế giới (khi ở xa, không bị cản)
        Quaternion rot = Quaternion.Euler(0, _currentYaw, 0);
        Vector3 idealPos = target.position + new Vector3(0, heightOffset, 0) + rot * new Vector3(0, 0, -distance);

        // 3. Bắn tia XÉO từ người chơi ra vị trí lý tưởng của Cam (quét theo đường nhìn)
        Vector3 rayDir = idealPos - playerPivot;
        float maxRayLength = rayDir.magnitude;
        float appliedDistance = maxRayLength;

        // Quét khối cầu để chống sượng góc tường
        RaycastHit hit;
        if (Physics.SphereCast(playerPivot, cameraRadius, rayDir.normalized, out hit, maxRayLength, obstacleLayers))
        {
            // Nếu chạm tường, thu ngắn khoảng cách dọc theo đường chéo tầm nhìn
            appliedDistance = Mathf.Max(minDistance, hit.distance - 0.1f);
        }

        // 4. Tính toán vị trí mong muốn cuối cùng (Cam sẽ tự hạ độ cao khi lại gần)
        Vector3 desiredPos = playerPivot + rayDir.normalized * appliedDistance;

        // 5. Di chuyển mượt mà tới vị trí đó
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, smoothTime);

        // 6. ĐIỀU CHỈNH ĐIỂM NHÌN THÔNG MINH (QUAN TRỌNG)
        // Khi ở xa (t -> 1): Nhìn lên cao (heightOffset) để đẩy Player xuống góc dưới màn hình cho đẹp.
        // Khi ở gần tường (t -> 0): Hạ điểm nhìn về tâm người (playerPivotHeight) để ép cam cúi xuống, giữ Player ở giữa màn hình.
        float t = appliedDistance / maxRayLength;
        float dynamicLookHeight = Mathf.Lerp(playerPivotHeight, heightOffset, t);
        Vector3 currentLookAtTarget = target.position + Vector3.up * dynamicLookHeight;

        transform.LookAt(currentLookAtTarget);
    }

    /// <summary>
    /// Reset góc xoay về 0 (hướng Bắc).
    /// </summary>
    public void ResetYaw() => _currentYaw = 0f;

    /// <summary>
    /// Đặt góc xoay hiện tại (theo độ).
    /// </summary>
    public void SetYaw(float yaw) => _currentYaw = yaw;

    /// <summary>
    /// Lấy góc xoay hiện tại (độ).
    /// </summary>
    public float GetYaw() => _currentYaw;
}