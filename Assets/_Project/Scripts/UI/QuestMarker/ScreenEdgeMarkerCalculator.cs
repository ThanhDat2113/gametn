using UnityEngine;

/// <summary>
/// Static utility: tính toán vị trí và góc marker trên mép màn hình.
/// Hoạt động đúng với camera 2.5D góc nghiêng (isometric/semi-fixed).
/// </summary>
public static class ScreenEdgeMarkerCalculator
{
    /// <summary>
    /// Tính hướng screen-space từ player đến NPC.
    ///
    /// Cách tiếp cận:
    ///   1. Bỏ trục Y (map 3D, player và NPC cùng mặt phẳng XZ)
    ///   2. Dịch chuyển NPC về hệ tọa độ gốc player (playerPos làm gốc)
    ///   3. Chiếu vector XZ đó qua camera → ra hướng screen-space đúng
    /// </summary>
    public static Vector2 GetScreenDirection(
        Vector3 playerWorldPos,
        Vector3 targetWorldPos,
        Camera mainCamera)
    {
        if (mainCamera == null) return Vector2.right;

        // Tính vector từ player → NPC, bỏ trục Y
        Vector3 worldDelta = targetWorldPos - playerWorldPos;
        worldDelta.y = 0f;

        if (worldDelta.sqrMagnitude < 0.0001f) return Vector2.right;

        // Chiếu 2 điểm: player và (player + delta) lên screen
        // Dùng playerPos làm anchor để loại bỏ ảnh hưởng của camera offset
        Vector3 playerScreen = mainCamera.WorldToScreenPoint(playerWorldPos);
        Vector3 targetScreen = mainCamera.WorldToScreenPoint(
            playerWorldPos + worldDelta);

        Vector2 dir = new Vector2(
            targetScreen.x - playerScreen.x,
            targetScreen.y - playerScreen.y);

        // Nếu target ở sau camera → đảo hướng
        Vector3 targetVP = mainCamera.WorldToViewportPoint(targetWorldPos);
        if (targetVP.z < 0f) dir = -dir;

        if (dir.sqrMagnitude < 0.0001f) return Vector2.right;
        return dir.normalized;
    }

    /// <summary>
    /// Tính góc quay (degrees) cho mũi tên hướng về NPC.
    /// Kết quả dùng với Quaternion.AngleAxis(angle + spriteOffset, Vector3.forward).
    /// spriteOffset: LEFT=180°, RIGHT=0°, UP=-90°, DOWN=90°
    /// </summary>
    public static float CalculateArrowRotation(
        Vector3 playerWorldPos,
        Vector3 targetWorldPos,
        Camera mainCamera)
    {
        if (mainCamera == null) return 0f;
        Vector2 dir = GetScreenDirection(playerWorldPos, targetWorldPos, mainCamera);
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

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
    /// Trả về screen position 2D đã clamp về mép màn hình.
    /// Dùng player screen position làm tâm thay vì tâm màn hình.
    /// </summary>
    public static Vector2 CalculateEdgeScreenPos(
        Vector3 playerWorldPos,
        Vector3 targetWorldPos,
        Camera mainCamera,
        float padding = 20f)
    {
        if (mainCamera == null) return Vector2.zero;

        Vector2 dir = GetScreenDirection(playerWorldPos, targetWorldPos, mainCamera);

        // Dùng tâm màn hình làm anchor cho edge clamp (marker nằm trên UI)
        Vector2 center = new Vector2(mainCamera.pixelWidth * 0.5f, mainCamera.pixelHeight * 0.5f);

        float halfW = mainCamera.pixelWidth  * 0.5f - padding;
        float halfH = mainCamera.pixelHeight * 0.5f - padding;

        float scale = Mathf.Min(
            Mathf.Abs(dir.x) > 0.0001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue,
            Mathf.Abs(dir.y) > 0.0001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue);

        return center + dir * scale;
    }

    // ── Legacy overloads (không break code cũ) ───────────────────────────────

    public static Vector2 CalculateEdgeScreenPos(
        Vector3 targetWorldPos,
        Camera mainCamera,
        float padding = 20f)
    {
        if (mainCamera == null) return Vector2.zero;
        return CalculateEdgeScreenPos(mainCamera.transform.position, targetWorldPos, mainCamera, padding);
    }

    public static Vector2 CalculateEdgePosition(
        Vector3 targetWorldPos,
        Camera mainCamera,
        RectTransform canvasRect,
        float padding = 20f)
    {
        if (mainCamera == null || canvasRect == null) return Vector2.zero;
        Vector2 edgeScreenPos = CalculateEdgeScreenPos(targetWorldPos, mainCamera, padding);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, edgeScreenPos, null, out Vector2 canvasPos);
        return canvasPos;
    }

    public static float CalculateArrowRotation(Vector3 targetWorldPos, Camera mainCamera)
    {
        if (mainCamera == null) return 0f;
        return CalculateArrowRotation(mainCamera.transform.position, targetWorldPos, mainCamera);
    }
}
