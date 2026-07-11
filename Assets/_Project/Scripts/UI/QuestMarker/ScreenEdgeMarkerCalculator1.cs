using UnityEngine;

/// <summary>
/// Static utility: tính toán hướng, góc xoay VÀ vị trí trên vòng tròn quanh player
/// cho hệ thống quest marker.
///
/// CHIẾN LƯỢC CHO CAMERA 2.5D/ISOMETRIC (pitch lớn, nhìn xuống):
///   Chiếu vector "player → target" lên mặt phẳng ngang XZ, sau đó
///   dùng góc giữa vector đó và hướng "camera forward trên mặt phẳng XZ"
///   để xác định trái/phải (X) và trước/sau (Y trên màn hình).
///
///   Cách này hoạt động đúng cho mọi pitch camera vì không dùng
///   WorldToViewportPoint (bị méo khi pitch lớn hoặc target sau lưng).
///
/// GHI CHÚ (bản ring): GetScreenDirection trả về hướng CAMERA-RELATIVE (x=phải, y=trước).
/// Hướng này được dùng làm nguồn chân lý duy nhất cho cả:
///   - Vị trí marker trên vòng tròn UI overlay (ScreenOverlayRing) — dùng trực tiếp làm hướng 2D.
///   - Vị trí marker trên vòng tròn world-space (WorldSpaceRing) — convert qua GetWorldFlatDirection
///     để ra vector world thật (world-relative nhưng vẫn "xoay" theo hướng camera nhìn,
///     giống hệt hành vi mép màn hình cũ).
/// </summary>
public static class ScreenEdgeMarkerCalculator
{
    // Cache frustum planes để tránh tạo array mới mỗi frame (alloc GC)
    private static readonly Plane[] _frustumPlanes = new Plane[6];

    /// <summary>
    /// Kiểm tra target có nằm HOÀN TOÀN trong view frustum không, với một viewport
    /// shrink buffer để tránh "vùng chết" tại biên màn hình.
    ///
    /// VẤN ĐỀ của WorldToViewportPoint cũ:
    ///   Khi NPC vừa ra khỏi mép màn hình, hàm cũ có thể trả về kết quả không nhất
    ///   quán (z âm / tọa độ NaN khi target sau near clip plane) → marker bị mất rồi
    ///   hiện lại đột ngột. Error "Screen position out of view frustum" trong Console
    ///   là triệu chứng rõ nhất.
    ///
    /// GIẢI PHÁP:
    ///   • Dùng GeometryUtility.CalculateFrustumPlanes + TestPlanesAABB — chính xác
    ///     hơn và không bị NaN/edge-case near clip.
    ///   • Thêm `viewportShrink` co nhỏ vùng "in-view" vào trong một chút so với mép
    ///     màn hình thực. Marker chuyển sang ring mode SỚM HƠN một chút trước khi NPC
    ///     thật sự ra mép → tránh giật/nhấp nháy khi NPC đứng đúng biên frustum.
    ///     Giá trị mặc định 0.05 = 5% viewport mỗi cạnh (±50px trên màn hình 1920×1080).
    /// </summary>
    public static bool IsInViewFrustum(Vector3 targetWorldPos, Camera mainCamera,
                                       float viewportShrink = 0.05f)
    {
        if (mainCamera == null) return false;

        // Bước 1: kiểm tra nhanh bằng viewport point (loại ngay các trường hợp
        // rõ ràng ngoài màn hình, kể cả sau lưng camera z <= 0)
        Vector3 vp = mainCamera.WorldToViewportPoint(targetWorldPos);
        if (vp.z <= 0f) return false; // sau lưng camera

        float lo = viewportShrink;
        float hi = 1f - viewportShrink;
        if (vp.x < lo || vp.x > hi || vp.y < lo || vp.y > hi) return false;

        // Bước 2: xác nhận bằng frustum planes (xử lý đúng near/far clip)
        GeometryUtility.CalculateFrustumPlanes(mainCamera, _frustumPlanes);
        // Dùng AABB điểm (size = zero) để test một điểm đơn
        Bounds pointBounds = new Bounds(targetWorldPos, Vector3.zero);
        return GeometryUtility.TestPlanesAABB(_frustumPlanes, pointBounds);
    }

    /// <summary>
    /// Tính hướng screen-space 2D (camera-relative) từ camera đến target.
    ///
    /// Dùng camera forward/right đã được flatten xuống mặt phẳng XZ,
    /// phù hợp với camera 2.5D/isometric nhìn xuống với bất kỳ pitch nào.
    /// Xử lý đúng cả khi target ở sau lưng camera.
    /// </summary>
    public static Vector2 GetScreenDirection(Vector3 targetWorldPos, Camera mainCamera)
    {
        Vector3 toTarget = targetWorldPos - mainCamera.transform.position;
        Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);

        Vector3 camForwardFlat = mainCamera.transform.forward;
        camForwardFlat.y = 0f;

        Vector3 camRightFlat = mainCamera.transform.right;
        camRightFlat.y = 0f;

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

        if (toTargetFlat.sqrMagnitude < 0.001f)
            return Vector2.up;

        float x = Vector3.Dot(toTargetFlat, camRightFlat);
        float y = Vector3.Dot(toTargetFlat, camForwardFlat);

        Vector2 dir = new Vector2(x, y);
        if (dir.sqrMagnitude < 0.0001f) return Vector2.up;
        return dir.normalized;
    }

    /// <summary>
    /// Giống GetScreenDirection nhưng trả về vector WORLD thật (trên mặt phẳng XZ),
    /// dùng để đặt marker world-space quanh chân player.
    ///
    /// Vẫn "camera-relative" theo nghĩa: hướng x/y camera-relative (từ GetScreenDirection)
    /// được chiếu ngược lại thành world bằng camRightFlat/camForwardFlat — nên khi camera
    /// xoay quanh player, marker world-space cũng xoay theo đúng như bản mép màn hình cũ.
    /// </summary>
    public static Vector3 GetWorldFlatDirection(Vector3 targetWorldPos, Camera mainCamera)
    {
        if (mainCamera == null) return Vector3.forward;

        Vector2 dir = GetScreenDirection(targetWorldPos, mainCamera);

        Vector3 camForwardFlat = mainCamera.transform.forward;
        camForwardFlat.y = 0f;
        Vector3 camRightFlat = mainCamera.transform.right;
        camRightFlat.y = 0f;

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

        Vector3 worldDir = camRightFlat * dir.x + camForwardFlat * dir.y;
        if (worldDir.sqrMagnitude < 0.0001f) return camForwardFlat;
        return worldDir.normalized;
    }

    /// <summary>
    /// Vị trí world-space trên vòng tròn bán kính `radius` quanh `ringCenterWorld`,
    /// theo hướng camera-relative tới targetWorldPos.
    /// </summary>
    public static Vector3 CalculateRingWorldPos(
        Vector3 ringCenterWorld, Vector3 targetWorldPos, Camera mainCamera, float radius)
    {
        Vector3 dir = GetWorldFlatDirection(targetWorldPos, mainCamera);
        return ringCenterWorld + dir * radius;
    }

    /// <summary>
    /// Trả về screen position 2D đã clamp về mép màn hình, theo đúng hướng tới target.
    /// Giữ lại cho tương thích ngược / dùng nơi khác nếu cần fallback mép màn hình.
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