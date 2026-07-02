using UnityEngine;

/// <summary>
/// Static utility: tính toán vị trí VÀ hướng xoay (rotation) của marker trên màn hình.
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
    /// Tính hướng screen-space (từ tâm màn hình) đến target — dùng để clamp marker
    /// về đúng cạnh màn hình, và làm cơ sở tính góc xoay cho CalculateArrowRotation.
    /// </summary>
    public static Vector2 GetScreenDirection(Vector3 targetWorldPos, Camera mainCamera)
    {
        Vector3 toVP = mainCamera.WorldToViewportPoint(targetWorldPos);

        // Target ở sau camera → flip qua tâm viewport để clamp về đúng phía
        if (toVP.z < 0f)
        {
            toVP.x = 1f - toVP.x;
            toVP.y = 1f - toVP.y;
        }

        Vector2 dir = new Vector2(
            (toVP.x - 0.5f) * mainCamera.pixelWidth,
            (toVP.y - 0.5f) * mainCamera.pixelHeight);

        if (dir.sqrMagnitude < 0.0001f) return Vector2.right;
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
    /// Tính góc quay (degrees) cho mũi tên hướng về target, dựa trên hướng screen-space
    /// tính từ tâm màn hình. Dùng Atan2 chuẩn — kết hợp với spriteAngleOffset trên
    /// QuestMarkerUI để căn đúng theo hướng gốc của sprite mũi tên.
    /// </summary>
    public static float CalculateArrowRotation(Vector3 targetWorldPos, Camera mainCamera)
    {
        if (mainCamera == null) return 0f;

        Vector2 dir = GetScreenDirection(targetWorldPos, mainCamera);
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }
}
