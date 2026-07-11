using UnityEngine;

/// <summary>
/// Gắn lên 1 GameObject trong scene ĐÍCH (scene được load sau teleport).
/// Khi scene load xong, script tự tìm player và đặt đúng vị trí đã lưu
/// bởi QuestTeleportTrigger qua PlayerPrefs.
///
/// SETUP:
///   1. Tạo 1 empty GameObject trong scene đích, đặt tên "PlayerSpawnPoint".
///   2. Gắn component này vào.
///   3. Đảm bảo spawnPosKeyPrefix khớp với field cùng tên trên QuestTeleportTrigger
///      (mặc định cả 2 đều là "Spawn").
///   4. Gán defaultSpawnPoint nếu muốn có vị trí spawn mặc định khi không có
///      dữ liệu từ QuestTeleportTrigger (vd: lần đầu vào scene).
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("Phải khớp với spawnPosKeyPrefix trên QuestTeleportTrigger. Mặc định: 'Spawn'")]
    [SerializeField] private string spawnPosKeyPrefix = "Spawn";

    [Tooltip("Vị trí/rotation spawn mặc định nếu không có dữ liệu PlayerPrefs " +
             "(vd: lần đầu vào scene, hoặc vào scene không qua teleport).")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Tooltip("Xoá PlayerPrefs spawn key sau khi spawn xong — tránh lần sau vào scene " +
             "lại spawn sai vị trí nếu không qua QuestTeleportTrigger.")]
    [SerializeField] private bool clearKeysAfterSpawn = true;

    [Tooltip("Để trống sẽ tự tìm qua MinimapController.Instance.Player hoặc tag 'Player'.")]
    [SerializeField] private Transform playerTransform;

    private void Start()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Transform player = ResolvePlayer();
        if (player == null)
        {
            Debug.LogWarning("[PlayerSpawnPoint] Không tìm được player transform.");
            return;
        }

        string keyX    = spawnPosKeyPrefix + "X";
        string keyY    = spawnPosKeyPrefix + "Y";
        string keyZ    = spawnPosKeyPrefix + "Z";
        string keyRotY = spawnPosKeyPrefix + "RotY";

        bool hasData = PlayerPrefs.HasKey(keyX);

        if (hasData)
        {
            Vector3 pos = new Vector3(
                PlayerPrefs.GetFloat(keyX),
                PlayerPrefs.GetFloat(keyY),
                PlayerPrefs.GetFloat(keyZ));
            float rotY = PlayerPrefs.GetFloat(keyRotY, 0f);

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));

            if (cc != null) cc.enabled = true;

            Debug.Log($"[PlayerSpawnPoint] Spawned player at {pos} (from QuestTeleportTrigger data).");

            if (clearKeysAfterSpawn)
            {
                PlayerPrefs.DeleteKey(keyX);
                PlayerPrefs.DeleteKey(keyY);
                PlayerPrefs.DeleteKey(keyZ);
                PlayerPrefs.DeleteKey(keyRotY);
                PlayerPrefs.Save();
            }
        }
        else if (defaultSpawnPoint != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.SetPositionAndRotation(defaultSpawnPoint.position, defaultSpawnPoint.rotation);

            if (cc != null) cc.enabled = true;

            Debug.Log($"[PlayerSpawnPoint] Spawned player at default point {defaultSpawnPoint.position}.");
        }
        else
        {
            Debug.Log("[PlayerSpawnPoint] Không có spawn data và không có defaultSpawnPoint — giữ nguyên vị trí player.");
        }
    }

    private Transform ResolvePlayer()
    {
        if (playerTransform != null) return playerTransform;
        if (MinimapController.Instance != null && MinimapController.Instance.Player != null)
            return MinimapController.Instance.Player;
        var go = GameObject.FindWithTag("Player");
        return go != null ? go.transform : null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (defaultSpawnPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(defaultSpawnPoint.position, 0.4f);
        UnityEditor.Handles.Label(defaultSpawnPoint.position + Vector3.up * 0.6f, "Default Spawn");
    }
#endif
}
