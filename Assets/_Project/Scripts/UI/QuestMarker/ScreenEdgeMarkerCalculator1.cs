using UnityEngine;

/// <summary>
/// Static utility: tính toán vị trí VÀ hướng xoay (rotation) của marker trên màn hình.
///
/// CHIẾN LƯỢC CHO CAMERA 2.5D/ISOMETRIC (pitch lớn, nhìn xuống):
///   Chiếu vector "player → target" lên mặt phẳng ngang XZ, sau đó
///   dùng góc giữa vector đó và hướng "camera forward trên mặt phẳng XZ"
///   để xác định trái/phải (X) và trước/sau (Y trên màn hình).
///
///   Cách này hoạt động đúng cho mọi pitch camera vì không dùng
///   WorldToViewportPoint (bị méo khi pitch lớn hoặc target sau lưng).
/// </summary>
public static class ScreenEdgeMarkerCalculator
{
    /// <summary>Kiểm tra target có nằm trong view frustum không.</summary>
    public static bool IsInViewFrustum(Vector3 targetWorldPos, Camera mainCamera)
    {
        if (mainCamera == null) return false;
        Vector3 vp = mainCamera.WorldToViewportPoint(targetWorldPos);
        return vp.z > 0f
            && vp.x >= 0f && vp.x <= 1f
            && vp.y >= 0f && vp.y <= 1f;
    }

    /// <summary>
    /// Tính hướng screen-space 2D từ tâm màn hình đến target.
    ///
    /// Dùng camera forward/right đã được flatten xuống mặt phẳng XZ,
    /// phù hợp với camera 2.5D/isometric nhìn xuống với bất kỳ pitch nào.
    /// Xử lý đúng cả khi target ở sau lưng camera (vd player quay lưng về Vergil).
    /// </summary>
    public static Vector2 GetScreenDirection(Vector3 targetWorldPos, Camera mainCamera)
    {
        // Vector từ camera đến target trên mặt phẳng XZ (bỏ Y)
        Vector3 toTarget = targetWorldPos - mainCamera.transform.position;
        Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);

        // Forward của camera flatten xuống XZ (hướng camera nhìn về phía trước trên map)
        Vector3 camForwardFlat = mainCamera.transform.forward;
        camForwardFlat.y = 0f;

        // Right của camera flatten xuống XZ
        Vector3 camRightFlat = mainCamera.transform.right;
        camRightFlat.y = 0f;

        // Edge case: camera nhìn thẳng đứng (overhead 90°) → forward flat = zero
        // Fallback về world forward/right
        if (camForwardFlat.sqrMagnitude < 0.001f)
        {
            camForwardFlat = Vector3.forward;
            camRightFlat   = Vector3.right;
        }
        else
        {
            camForwardFlat.Normalize();
            camRightFlat.Normalize();
        }

        // Edge case: target thẳng đứng phía trên/dưới camera (flat = zero)
        // → không có thông tin hướng ngang, trả về Vector2.up (chỉ lên)
        if (toTargetFlat.sqrMagnitude < 0.001f)
            return Vector2.up;

        // Chiếu toTargetFlat lên right/forward của camera
        // → x: dương = target bên phải camera, âm = bên trái
        // → y: dương = target phía trước camera (lên trên màn hình), âm = phía sau
        float x = Vector3.Dot(toTargetFlat, camRightFlat);
        float y = Vector3.Dot(toTargetFlat, camForwardFlat);

        Vector2 dir = new Vector2(x, y);
        if (dir.sqrMagnitude < 0.0001f) return Vector2.up;
        return dir.normalized;
    }

    /// <summary>
    /// Trả về screen position 2D đã clamp về mép màn hình, theo đúng hướng tới target.
    /// </summary>
    public static Vector2 CalculateEdgeScreenPos(
        Vector3 targetWorldPos,
        Camera mainCamera,
        float padding = 20f)
    {
        if (mainCamera == null) return Vector2.zero;

        Vector2 center = new Vector2(mainCamera.pixelWidth * 0.5f, mainCamera.pixelHeight * 0.5f);
        Vector2 dir    = GetScreenDirection(targetWorldPos, mainCamera);

        float halfW = mainCamera.pixelWidth  * 0.5f - padding;
        float halfH = mainCamera.pixelHeight * 0.5f - padding;

        float scale = Mathf.Min(
            Mathf.Abs(dir.x) > 0.0001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue,
            Mathf.Abs(dir.y) > 0.0001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue);

        return center + dir * scale;
    }

    /// <summary>
    /// Tính góc quay (degrees) cho mũi tên hướng về target.
    /// Kết hợp với spriteAngleOffset trên QuestMarkerUI để căn đúng sprite.
    /// </summary>
    public static float CalculateArrowRotation(Vector3 targetWorldPos, Camera mainCamera)
    {
        if (mainCamera == null) return 0f;
        Vector2 dir = GetScreenDirection(targetWorldPos, mainCamera);
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }
}