using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Static utility: tính đường đi bằng NavMesh (chỉ dùng để QUERY path,
/// KHÔNG di chuyển object nào — phù hợp khi player dùng CharacterController/Rigidbody riêng).
///
/// YÊU CẦU:
///   - Map phải đã Bake NavMesh (NavMeshSurface → Bake)
///   - Player/NPC phải đứng đủ gần vùng NavMesh đã bake (trong maxDistance)
///
/// CÁCH DÙNG:
///   NavMeshPath path = new NavMeshPath();
///   var reason = NavMeshPathfinder.TryCalculatePath(playerPos, npcPos, path, out var detail);
///   Vector3[] corners = path.corners;
/// </summary>
public static class NavMeshPathfinder
{
    public enum FailReason
    {
        None,                 // thành công
        FromNotOnNavMesh,     // điểm xuất phát (player) không sample được lên NavMesh
        ToNotOnNavMesh,       // điểm đích (NPC) không sample được lên NavMesh
        PathPartial,          // có path nhưng không trọn vẹn (bị chặn giữa đường)
        PathInvalid           // CalculatePath trả false hoàn toàn
    }

    /// <summary>Tính đường đi giữa 2 điểm. Trả về FailReason.None nếu thành công.</summary>
    public static FailReason TryCalculatePath(
        Vector3 from, Vector3 to, NavMeshPath resultPath,
        int areaMask = NavMesh.AllAreas, float sampleMaxDistance = 5f)
    {
        if (!SampleOnNavMesh(from, out Vector3 fromOnMesh, areaMask, sampleMaxDistance))
            return FailReason.FromNotOnNavMesh;

        if (!SampleOnNavMesh(to, out Vector3 toOnMesh, areaMask, sampleMaxDistance))
            return FailReason.ToNotOnNavMesh;

        bool found = NavMesh.CalculatePath(fromOnMesh, toOnMesh, areaMask, resultPath);
        if (!found) return FailReason.PathInvalid;

        if (resultPath.status == NavMeshPathStatus.PathPartial) return FailReason.PathPartial;
        if (resultPath.status == NavMeshPathStatus.PathInvalid) return FailReason.PathInvalid;

        return FailReason.None;
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

    public static Vector3? GetNextCorner(NavMeshPath path, int cornerIndex)
    {
        if (path == null || path.corners == null) return null;
        if (cornerIndex <= 0 || cornerIndex >= path.corners.Length) return null;
        return path.corners[cornerIndex];
    }

    public static int CornerCount(NavMeshPath path) => path?.corners?.Length ?? 0;
}   