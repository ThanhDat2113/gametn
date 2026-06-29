using UnityEngine;

public class HSRCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;        // Kéo Player vào đây

    [Header("Orbit Settings")]
    public float distance = 25f;    // Đang để 25 theo hình của bạn
    public float yawSpeed = 80f;    
    public KeyCode rotateLeft = KeyCode.None;
    public KeyCode rotateRight = KeyCode.None;

    [Header("Vertical Offset")]
    public float heightOffset = 3f; // Đang để 3 theo hình của bạn

    [Header("Smoothing")]
    public float smoothTime = 0.15f;
    private float _currentYaw = 0f;
    private Vector3 _velocity = Vector3.zero;

    [Header("Collision Settings (Né vật cản)")]
    public LayerMask obstacleLayers;   // Chọn Layer của Tường/Nhà
    public float cameraRadius = 0.3f;  // Bán kính khối cầu quét tường
    public float minDistance = 2.0f;   // Khoảng cách tối thiểu khi ép sát tường

    [Tooltip("Độ cao trọng tâm cơ thể Player (thường là 1 mét) để làm gốc quét tia")]
    public float playerPivotHeight = 1.0f;

    void LateUpdate()
    {
        if (target == null) return;

        // Xoay khi giữ nút (nếu có gán)
        if (rotateLeft != KeyCode.None && Input.GetKey(rotateLeft))
            _currentYaw -= yawSpeed * Time.deltaTime;
        if (rotateRight != KeyCode.None && Input.GetKey(rotateRight))
            _currentYaw += yawSpeed * Time.deltaTime;

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

    public void ResetYaw() => _currentYaw = 0f;
}