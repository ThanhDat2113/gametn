using UnityEngine;
using TMPro;

public class ChestReward : MonoBehaviour
{
    [Header("Phần thưởng")]
    public int goldReward = 100;
    public string itemReward = "Bình máu";

    [Header("UI")]
    public GameObject rewardPanel;           // Panel thông báo
    public TextMeshProUGUI rewardText;       // Text hiển thị thưởng

    [Header("Tương tác")]
    public KeyCode interactKey = KeyCode.E;
    public GameObject promptUI;              // UI "Nhấn E"
    public float interactionRange = 2f;

    [Header("Animation")]
    public Animator chestAnimator;
    public string openTrigger = "Open";

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

        // Tìm player mỗi frame hoặc dùng cache
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

        // Chạy animation mở
        if (chestAnimator != null) chestAnimator.SetTrigger(openTrigger);

        // Vô hiệu hóa collider để không tương tác lại
        if (chestCollider != null) chestCollider.enabled = false;

        // Ẩn prompt
        if (promptUI != null) promptUI.SetActive(false);

        // Hiện panel thưởng
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            if (rewardText != null)
                rewardText.text = $"Bạn nhận được:\n{GetGoldText()}{itemReward}";
            Invoke(nameof(HideRewardPanel), 3f);
        }

        // Trao thưởng (kết nối với hệ thống của bạn)
        GrantReward();
    }

    string GetGoldText()
    {
        return goldReward > 0 ? $"{goldReward} Vàng\n" : "";
    }

    void HideRewardPanel()
    {
        if (rewardPanel != null) rewardPanel.SetActive(false);
    }

    void GrantReward()
    {
        // TODO: Cộng vàng/ item vào player inventory
        Debug.Log($"[Chest] Nhận {goldReward} vàng, {itemReward}");
    }
}