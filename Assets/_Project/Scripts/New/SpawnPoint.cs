using UnityEngine;

/// <summary>
/// Gắn lên GameObject trong map để đánh dấu vị trí spawn của player.
/// Mỗi spawn point có một ID riêng để SceneTransitionManager tìm đúng.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn ID (phải là duy nhất trong toàn bộ game)")]
    public string spawnID = "Spawn_Default";

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, $"Spawn: {spawnID}");
        #endif
    }
}