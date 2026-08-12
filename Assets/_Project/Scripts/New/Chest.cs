using UnityEngine;
using System.Collections.Generic;

public class Chest : MonoBehaviour
{
    [Header("Chest Data")]
    public ChestData chestData;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject promptUI;

    [Header("Animation")]
    public Animator chestAnimator;
    public string openTrigger = "Open";

    [Header("Audio")]
    public AudioClip openSound;

    private bool _isOpen = false;
    private bool _playerInRange = false;

    private void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
        Debug.Log($"[Chest] {gameObject.name} đã khởi tạo (dùng trigger)");
    }

    private void Update()
    {
        if (_isOpen) return;

        if (_playerInRange && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"[Chest] {gameObject.name} → Nhấn E, mở rương!");
            OpenChest();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (promptUI != null) promptUI.SetActive(true);
        Debug.Log($"[Chest] {gameObject.name} → Player vào vùng tương tác");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
        Debug.Log($"[Chest] {gameObject.name} → Player rời vùng tương tác");
    }

    private void OpenChest()
    {
        if (_isOpen) return;
        _isOpen = true;
        Debug.Log($"[Chest] {gameObject.name} → Mở rương!");

        if (chestAnimator != null)
            chestAnimator.SetTrigger(openTrigger);

        if (promptUI != null)
            promptUI.SetActive(false);

        if (openSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX2D(openSound, 0.7f);

        GrantRewardAndShowUI();

        // Vô hiệu hóa collider để không tương tác lại
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void GrantRewardAndShowUI()
    {
        List<ChestRewardItem> items = new List<ChestRewardItem>();
        if (chestData != null && chestData.items != null)
        {
            foreach (var reward in chestData.items)
            {
                if (reward.item == null) continue;
                items.Add(reward);
            }
        }

        Debug.Log($"[Chest] {gameObject.name} → Trao {items.Count} phần thưởng.");

        foreach (var reward in items)
        {
            if (reward.item != null && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(reward.item, reward.amount);
                Debug.Log($"[Chest] +{reward.amount} {reward.item.itemName}");
            }
        }

        if (ChestRewardUI.Instance != null)
        {
            ChestRewardUI.Instance.ShowReward(chestData, items);
            Debug.Log($"[Chest] {gameObject.name} → Đã gọi ChestRewardUI.ShowReward()");
        }
        else
        {
            Debug.LogWarning($"[Chest] {gameObject.name} → KHÔNG tìm thấy ChestRewardUI!");
        }
    }

    private void OnDrawGizmos()
    {
        // Vẽ collider nếu có
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(transform.position + box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}