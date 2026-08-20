using UnityEngine;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Player Reference")]
    public GameObject player;

    [Header("Movement Script Reference")]
    [Tooltip("Kéo script điều khiển di chuyển của player vào đây (VD: PlayerController)")]
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

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                Debug.LogError("[PlayerManager] Không tìm thấy Player! Hãy gán thủ công hoặc đảm bảo có tag 'Player'.");
        }
    }

    public GameObject GetPlayer() => player;

    public Vector3 GetPlayerPosition() => player != null ? player.transform.position : Vector3.zero;

    public Quaternion GetPlayerRotation() => player != null ? player.transform.rotation : Quaternion.identity;

    /// <summary>
    /// Dừng player ngay lập tức: tắt script điều khiển, reset Rigidbody, dừng CharacterController.
    /// </summary>
    public void StopPlayer()
    {
        if (player == null) return;

        if (playerMovementScript != null && playerMovementScript.enabled)
            playerMovementScript.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
            cc.Move(Vector3.zero);
    }

    /// <summary>
    /// Bật lại script điều khiển của player.
    /// </summary>
    public void EnableMovement()
    {
        if (player == null) return;

        // 🔥 Bật lại CharacterController trước (nếu đã bị tắt bởi StopPlayer/MapEnemy/EncounterZone)
        var cc = player.GetComponent<CharacterController>();
        if (cc != null && !cc.enabled)
        {
            cc.enabled = true;
            Debug.Log("[PlayerManager] Re-enabled CharacterController.");
        }

        if (playerMovementScript != null && !playerMovementScript.enabled)
        {
            playerMovementScript.enabled = true;
            Debug.Log("[PlayerManager] Player movement enabled.");
        }
    }

    /// <summary>
    /// Teleport player tới vị trí mới.
    /// </summary>
    public void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        if (player == null) return;

        if (playerMovementScript != null && playerMovementScript.enabled)
            playerMovementScript.enabled = false;

        CharacterController cc = player.GetComponent<CharacterController>();
        bool ccWasEnabled = false;
        if (cc != null && cc.enabled)
        {
            ccWasEnabled = true;
            cc.enabled = false;
        }

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

        player.transform.position = position;
        player.transform.rotation = rotation;
        Physics.SyncTransforms();

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

        StartCoroutine(EnableMovementDelayed());
    }

    private IEnumerator EnableMovementDelayed()
    {
        yield return null;
        yield return null;
        EnableMovement();
    }
}