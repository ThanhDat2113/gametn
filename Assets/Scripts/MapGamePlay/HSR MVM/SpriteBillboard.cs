using UnityEngine;

/// <summary>
/// Billboard cho sprite 2D, luôn hướng về camera.
/// Hỗ trợ flip sprite theo hướng di chuyển dựa trên camera-relative.
/// </summary>
public class SpriteBillboard : MonoBehaviour
{
    [Header("Camera")]
    public Transform targetCamera; // Nếu để trống, tự tìm Camera.main

    [Header("Settings")]
    [Tooltip("Tự động flip sprite theo hướng di chuyển")]
    public bool flipOnMove = true;

    [Tooltip("Xoay sprite theo camera (Billboard)")]
    public bool enableBillboard = true;

    private Vector3 _initialScale;
    private bool _initialScaleSaved = false;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main?.transform;

        if (targetCamera == null)
        {
            Debug.LogWarning("[SpriteBillboard] Không tìm thấy Camera.main! Billboard sẽ không hoạt động.");
        }

        _initialScale = transform.localScale;
        _initialScaleSaved = true;
    }

    void LateUpdate()
    {
        if (!enableBillboard) return;
        if (targetCamera == null) return;

        // Billboard: sprite luôn hướng về camera (chỉ xoay theo trục Y)
        transform.LookAt(targetCamera.position);
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    }

    /// <summary>
    /// Flip sprite theo hướng di chuyển (camera-relative)
    /// </summary>
    public void FlipBasedOnMovement(Vector3 worldMoveDirection)
    {
        if (!flipOnMove) return;
        if (worldMoveDirection.sqrMagnitude < 0.001f) return;
        if (targetCamera == null) return;

        // Chuyển hướng di chuyển từ world space sang camera local space
        Vector3 localDir = targetCamera.InverseTransformDirection(worldMoveDirection);

        if (Mathf.Abs(localDir.x) > 0.01f)
        {
            float absX = Mathf.Abs(_initialScale.x);
            float flipX = Mathf.Sign(localDir.x) * absX;
            transform.localScale = new Vector3(flipX, _initialScale.y, _initialScale.z);
        }
    }

    /// <summary>
    /// Đặt hướng nhìn của sprite (true = phải, false = trái)
    /// </summary>
    public void SetFacingDirection(bool facingRight)
    {
        if (!_initialScaleSaved) return;
        float absX = Mathf.Abs(_initialScale.x);
        transform.localScale = new Vector3(facingRight ? absX : -absX, _initialScale.y, _initialScale.z);
    }

    /// <summary>
    /// Kiểm tra sprite đang nhìn về bên phải hay không
    /// </summary>
    public bool IsFacingRight()
    {
        if (!_initialScaleSaved) return true;
        return transform.localScale.x > 0;
    }

    /// <summary>
    /// Reset scale về giá trị ban đầu (dùng khi cần)
    /// </summary>
    public void ResetScale()
    {
        if (_initialScaleSaved)
            transform.localScale = _initialScale;
    }
}