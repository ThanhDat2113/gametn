using UnityEngine;

/// <summary>
/// Gắn lên trigger collider trong map. Khi player đi vào, hoàn thành step Explore.
/// </summary>
public class LocationTrigger : MonoBehaviour
{
    [Header("Identity")]
    public string locationID; // Khớp với targetId của step Explore

    [Header("Visual")]
    [Tooltip("GameObject prompt hiển thị khi ở gần (tùy chọn)")]
    public GameObject interactionPrompt;

    [Header("Settings")]
    public bool requirePlayerInRange = true; // Nếu true, cần player chạm trigger mới kích hoạt
    public bool autoCompleteOnEnter = true; // Tự động hoàn thành khi chạm, không cần nhấn E

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isTriggered) return;

        if (autoCompleteOnEnter)
        {
            CompleteLocation();
        }
        else
        {
            // Chờ nhấn E (nếu không auto)
            // Bạn có thể mở rộng thêm logic này nếu muốn
            Debug.Log("[LocationTrigger] Player entered, press E to interact.");
            // Ở đây ta có thể hiển thị prompt yêu cầu nhấn E và bắt phím.
        }
    }

    private void CompleteLocation()
    {
        if (isTriggered) return;
        isTriggered = true;

        Debug.Log($"[LocationTrigger] Location reached: {locationID}");

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnLocationReached(locationID);
        }

        // Ẩn prompt nếu có
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    // Tùy chọn: nếu dùng chế độ nhấn E để hoàn thành
    private void Update()
    {
        if (!autoCompleteOnEnter && isTriggered == false && Input.GetKeyDown(KeyCode.E))
        {
            // Có thể thêm logic kiểm tra player trong range
            CompleteLocation();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        // Nếu chưa hoàn thành, hiển thị lại prompt (nếu có)
        if (!isTriggered && interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.8f);
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"Location: {locationID}");
        #endif
    }
}