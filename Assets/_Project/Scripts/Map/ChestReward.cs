using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Rương trên map — khi mở sẽ roll loot và thêm vào Inventory.
/// Dùng chung LootEntry với EnemyGroupData.
/// Kết nối với QuestManager cho step Gather.
/// </summary>
namespace Game.Map
{
    public class ChestReward : MonoBehaviour
    {
        [Header("Loot Table")]
        [Tooltip("Danh sách vật phẩm có thể drop. Dùng chung class LootEntry với EnemyGroupData.")]
        public EnemyGroupData.LootEntry[] lootTable;

        [Header("UI")]
        public GameObject rewardPanel;
        public TextMeshProUGUI rewardText;

        [Header("Tương tác")]
        public KeyCode interactKey = KeyCode.E;
        public GameObject promptUI;
        public float interactionRange = 2f;

        [Header("Animation")]
        public Animator chestAnimator;
        public string openTrigger = "Open";

        [Header("Quest (Tùy chọn)")]
        [Tooltip("ID duy nhất của rương này, dùng để QuestManager theo dõi (nếu cần)")]
        public string chestId;

        private bool isOpen = false;
        private bool playerInRange = false;
        private Collider chestCollider;

        void Start()
        {
            if (rewardPanel != null) rewardPanel.SetActive(false);
            if (promptUI != null) promptUI.SetActive(false);
            chestCollider = GetComponent<Collider>();
        }

        void Update()
        {
            if (isOpen) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            bool inRange = dist <= interactionRange;

            if (inRange && !playerInRange)
            {
                playerInRange = true;
                if (promptUI != null) promptUI.SetActive(true);
            }
            else if (!inRange && playerInRange)
            {
                playerInRange = false;
                if (promptUI != null) promptUI.SetActive(false);
            }

            if (playerInRange && Input.GetKeyDown(interactKey))
            {
                OpenChest();
            }
        }

        void OpenChest()
        {
            if (isOpen) return;
            isOpen = true;

            // Animation mở rương
            if (chestAnimator != null) chestAnimator.SetTrigger(openTrigger);

            // Vô hiệu hóa tương tác
            if (chestCollider != null) chestCollider.enabled = false;
            if (promptUI != null) promptUI.SetActive(false);

            // Roll loot và thêm vào Inventory
            GrantLoot();

            // Báo QuestManager nếu có chestId
            if (!string.IsNullOrEmpty(chestId) && QuestManager.Instance != null)
            {
                QuestManager.Instance.OnChestOpened(chestId);
            }
        }

        void GrantLoot()
        {
            if (lootTable == null || lootTable.Length == 0)
            {
                Debug.LogWarning($"[Chest] '{gameObject.name}' không có loot table!");
                ShowRewardPanel("Rương trống...");
                return;
            }

            string rewardMessage = "Bạn nhận được:\n";
            bool hasReward = false;

            foreach (var entry in lootTable)
            {
                if (entry.item == null) continue;

                float roll = Random.value;
                if (roll <= entry.dropRate)
                {
                    int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                    if (amount > 0)
                    {
                        if (InventoryManager.Instance != null)
                        {
                            InventoryManager.Instance.AddItem(entry.item, amount);
                        }

                        rewardMessage += $"+{amount}x {entry.item.itemName}\n";
                        hasReward = true;

                        Debug.Log($"[Chest] +{amount}x {entry.item.itemName} (rate: {entry.dropRate * 100}%, roll: {roll:F2})");
                    }
                }
            }

            if (hasReward)
            {
                ShowRewardPanel(rewardMessage);
            }
            else
            {
                ShowRewardPanel("Rương trống...");
            }
        }

        void ShowRewardPanel(string message)
        {
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(true);
                if (rewardText != null)
                    rewardText.text = message;
                Invoke(nameof(HideRewardPanel), 3f);
            }
        }

        void HideRewardPanel()
        {
            if (rewardPanel != null) rewardPanel.SetActive(false);
        }
    }
}