using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Static utility: tính đường đi bằng NavMesh (chỉ dùng để QUERY path,
/// KHÔNG di chuyển object nào — phù hợp khi player dùng CharacterController/Rigidbody riêng).
///
/// YÊU CẦU:
///   - Map phải đã Bake NavMesh (Window → AI → Navigation → Bake)
///   - Địa hình đi được phải đánh dấu "Navigation Static"
///
/// CÁCH DÙNG:
///   NavMeshPath path = new NavMeshPath();
///   bool ok = NavMeshPathfinder.TryCalculatePath(playerPos, npcPos, path);
///   Vector3[] corners = path.corners; // các điểm rẽ, [0] luôn = playerPos
/// </summary>
public static class NavMeshPathfinder
{
    /// <summary>Tính đường đi giữa 2 điểm. Trả về false nếu không tìm được đường (bị chặn/ngoài NavMesh).</summary>
    public static bool TryCalculatePath(Vector3 from, Vector3 to, NavMeshPath resultPath, int areaMask = NavMesh.AllAreas)
    {
        // Snap về NavMesh gần nhất trước khi query (tránh lỗi nếu điểm hơi lệch khỏi mesh)
        if (!SampleOnNavMesh(from, out Vector3 fromOnMesh, areaMask)) return false;
        if (!SampleOnNavMesh(to,   out Vector3 toOnMesh,   areaMask)) return false;

        bool found = NavMesh.CalculatePath(fromOnMesh, toOnMesh, areaMask, resultPath);
        return found && resultPath.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>Snap một world position về điểm gần nhất trên NavMesh.</summary>
    public static bool SampleOnNavMesh(Vector3 worldPos, out Vector3 result, int areaMask = NavMesh.AllAreas, float maxDistance = 5f)
    {
        if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, maxDistance, areaMask))
        {
            result = hit.position;
            return true;
        }
        result = worldPos;
        return false;
    }

    /// <summary>
    /// Lấy waypoint kế tiếp cần đi tới từ vị trí hiện tại, dựa trên corners của path.
    /// Bỏ qua corner[0] (luôn là điểm xuất phát).
    /// Trả về null nếu path không hợp lệ hoặc chỉ có 1 điểm (đã ở đích).
    /// </summary>
    public static Vector3? GetNextCorner(NavMeshPath path, int cornerIndex)
    {
        if (path == null || path.corners == null) return null;
        if (cornerIndex <= 0 || cornerIndex >= path.corners.Length) return null;
        return path.corners[cornerIndex];
    }

    public static int CornerCount(NavMeshPath path) => path?.corners?.Length ?? 0;
}
