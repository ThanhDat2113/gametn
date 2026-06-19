using UnityEngine;

/// <summary>
/// Gắn script này vào GameObject để đánh dấu đó là điểm spawn/teleport
/// Tự động register vào TeleportPointRegistry
/// </summary>
public class TeleportPoint : MonoBehaviour
{
    [SerializeField] private string pointName = "SpawnPoint_Main";
    [SerializeField] private string description = "";

    private void OnEnable()
    {
        TeleportPointRegistry.RegisterPoint(this);
    }

    private void OnDisable()
    {
        TeleportPointRegistry.UnregisterPoint(this);
    }

    public string PointName => pointName;
    public string Description => description;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    /// <summary>
    /// Đặt tên cho điểm teleport
    /// </summary>
    public void SetPointName(string newName)
    {
        if (!string.IsNullOrEmpty(newName))
        {
            pointName = newName;
            gameObject.name = newName;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Vẽ sphere tại vị trí spawn
        Gizmos.color = new Color(0, 1, 0, 0.5f); // Green
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Vẽ direction forward
        Gizmos.color = new Color(0, 0, 1, 1f); // Blue
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);

        // Vẽ label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, pointName);
    }
#endif
}
