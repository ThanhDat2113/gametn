using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("ID khớp với targetId trong quest step (Gather)")]
    public string itemId;

    [Tooltip("Dữ liệu item để hiển thị tên và icon")]
    public ItemData itemData;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt; // UI prompt "Nhấn E để nhặt"

    private bool playerInRange;
    private bool isPickedUp;

    private void Update()
    {
        if (!playerInRange || isPickedUp) return;
        if (Input.GetKeyDown(interactKey))
        {
            ShowPickupUI();
        }
    }

    private void ShowPickupUI()
    {
        if (ItemPickupUI.Instance != null)
        {
            ItemPickupUI.Instance.Show(itemData, OnPickupConfirmed);
        }
        else
        {
            Debug.LogError("[ItemPickup] Không tìm thấy ItemPickupUI trong scene!");
        }
    }

    private void OnPickupConfirmed()
    {
        isPickedUp = true;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);

        // Thông báo cho QuestManager
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnItemPickedUp(itemId);
        else
            Debug.LogWarning("[ItemPickup] QuestManager không tồn tại.");

        // Ẩn hoặc hủy vật phẩm
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (interactionPrompt != null && !isPickedUp)
            interactionPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
}