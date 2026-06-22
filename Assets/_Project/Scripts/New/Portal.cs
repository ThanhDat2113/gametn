using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Destination")]
    public string targetMapName;
    public string targetSpawnPointID = "Spawn_Default";

    [Header("Audio")]
    public AudioClip portalSound;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;
        if (!other.CompareTag("Player")) return;

        isTriggered = true;

        // 🔥 Dừng player ngay lập tức (dùng PlayerManager)
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.StopPlayer();
        }
        else
        {
            // Fallback: tự xử lý
            StopPlayer(other.gameObject);
        }

        // Phát âm thanh
        if (portalSound != null)
            AudioManager.Instance?.PlaySFX2D(portalSound, 0.7f);

        // Chuyển scene
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToMap(targetMapName, targetSpawnPointID, () =>
            {
                isTriggered = false;
            });
        }
        else
        {
            Debug.LogError("[Portal] Không tìm thấy SceneTransitionManager!");
            isTriggered = false;
        }
    }

    /// <summary>
    /// Fallback: Dừng player nếu PlayerManager không có sẵn.
    /// </summary>
    private void StopPlayer(GameObject player)
    {
        // Tìm và tắt script điều khiển
        var scripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script == null || !script.enabled) continue;
            string name = script.GetType().Name.ToLower();
            if (name.Contains("movement") || name.Contains("controller") || name.Contains("input"))
            {
                script.enabled = false;
                break;
            }
        }

        // Reset Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Dừng CharacterController
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
            cc.Move(Vector3.zero);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, $"Portal → {targetMapName}");
        #endif
    }
}