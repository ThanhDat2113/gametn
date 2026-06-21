using UnityEngine;

/// <summary>
/// Singleton quản lý player trong toàn bộ game.
/// Đặt trong Persistent Scene, cung cấp tham chiếu và các tiện ích liên quan đến player.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Player Reference")]
    [Tooltip("Kéo GameObject player từ Persistent Scene vào đây.")]
    public GameObject player;

    [Header("Movement Script Reference")]
    [Tooltip("Kéo script điều khiển di chuyển của player vào đây (nếu có).")]
    public MonoBehaviour playerMovementScript;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Nếu chưa gán player, tự tìm theo tag
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                Debug.LogError("[PlayerManager] Không tìm thấy Player! Hãy gán thủ công hoặc đảm bảo có tag 'Player'.");
        }
    }

    /// <summary>
    /// Lấy GameObject của player.
    /// </summary>
    public GameObject GetPlayer()
    {
        return player;
    }

    /// <summary>
    /// Lấy vị trí của player.
    /// </summary>
    public Vector3 GetPlayerPosition()
    {
        return player != null ? player.transform.position : Vector3.zero;
    }

    /// <summary>
    /// Lấy rotation của player.
    /// </summary>
    public Quaternion GetPlayerRotation()
    {
        return player != null ? player.transform.rotation : Quaternion.identity;
    }

    /// <summary>
    /// Teleport player tới vị trí mới.
    /// </summary>
    public void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        if (player == null) return;

        // Tạm thời tắt script điều khiển
        if (playerMovementScript != null && playerMovementScript.enabled)
            playerMovementScript.enabled = false;

        // Xử lý CharacterController
        CharacterController cc = player.GetComponent<CharacterController>();
        bool ccWasEnabled = false;
        if (cc != null && cc.enabled)
        {
            ccWasEnabled = true;
            cc.enabled = false;
        }

        // Xử lý Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        bool rbWasKinematic = false;
        bool rbWasGravity = false;
        if (rb != null)
        {
            rbWasKinematic = rb.isKinematic;
            rbWasGravity = rb.useGravity;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Set position
        player.transform.position = position;
        player.transform.rotation = rotation;
        Physics.SyncTransforms();

        // Khôi phục
        if (cc != null && ccWasEnabled)
        {
            cc.enabled = true;
            cc.Move(Vector3.zero);
        }
        if (rb != null)
        {
            rb.isKinematic = rbWasKinematic;
            rb.useGravity = rbWasGravity;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Bật lại script điều khiển sau 2 frame
        if (playerMovementScript != null)
            StartCoroutine(EnableMovementDelayed());

        Debug.Log($"[PlayerManager] Teleport player đến {position}");
    }

    /// <summary>
    /// Dừng player ngay lập tức (reset velocity, tắt script điều khiển).
    /// </summary>
    public void StopPlayer()
    {
        if (player == null) return;

        // Tắt script điều khiển
        if (playerMovementScript != null && playerMovementScript.enabled)
            playerMovementScript.enabled = false;

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

    /// <summary>
    /// Kích hoạt lại script điều khiển sau một khoảng thời gian.
    /// </summary>
    public void EnableMovement()
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;
    }

    private System.Collections.IEnumerator EnableMovementDelayed()
    {
        yield return null;
        yield return null;
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;
    }
}