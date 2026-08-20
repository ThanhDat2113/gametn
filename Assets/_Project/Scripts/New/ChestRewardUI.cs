using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ChestRewardUI : MonoBehaviour
{
    public static ChestRewardUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI rewardListText; // ✅ Text hiển thị danh sách item
    public Button closeButton;

    [Header("Reward Icons")]
    public Transform rewardIconContainer; // Container chứa các icon item
    public GameObject rewardIconPrefab;   // Prefab icon (có Image component)

    [Header("Settings")]
    public float autoCloseDelay = 3f;

    private Coroutine _autoCloseCoroutine;
    private List<GameObject> _spawnedIcons = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (panel != null) panel.SetActive(false);
        Debug.Log("[ChestRewardUI] Awake");
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    public void ShowReward(ChestData chestData, List<ChestRewardItem> items)
    {
        Debug.Log($"[ChestRewardUI] ShowReward: {items?.Count} items");

        // Tiêu đề
        if (titleText != null)
            titleText.text = chestData != null ? chestData.chestName : "Rương kho báu";

        // Tạo danh sách item name
        string itemList = "";
        if (items != null && items.Count > 0)
        {
            foreach (var reward in items)
            {
                if (reward.item == null) continue;
                string amountText = reward.amount > 1 ? $" x{reward.amount}" : "";
                itemList += $"- {reward.item.itemName}{amountText}\n";
            }
        }
        else
        {
            itemList = "Không có vật phẩm nào";
        }

        if (rewardListText != null)
            rewardListText.text = itemList;

        // Tạo icon cho từng item
        SpawnRewardIcons(items);

        panel.SetActive(true);

        if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
        _autoCloseCoroutine = StartCoroutine(AutoClose());
    }

    private void SpawnRewardIcons(List<ChestRewardItem> items)
    {
        // Xóa icon cũ
        foreach (var icon in _spawnedIcons)
            if (icon != null) Destroy(icon);
        _spawnedIcons.Clear();

        if (rewardIconContainer == null) return;

        if (items == null || items.Count == 0) return;

        foreach (var reward in items)
        {
            if (reward.item == null || reward.item.icon == null) continue;

            GameObject iconObj;
            if (rewardIconPrefab != null)
            {
                iconObj = Instantiate(rewardIconPrefab, rewardIconContainer);
            }
            else
            {
                // Tạo icon động bằng code nếu không có prefab
                iconObj = new GameObject("RewardIcon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(rewardIconContainer, false);
            }

            var img = iconObj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = reward.item.icon;
                img.preserveAspect = true;
                img.color = Color.white;
            }

            _spawnedIcons.Add(iconObj);
        }
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        ClosePanel();
    }

    public void ClosePanel()
    {
        if (_autoCloseCoroutine != null)
        {
            StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = null;
        }
        if (panel != null) panel.SetActive(false);
        Debug.Log("[ChestRewardUI] Đóng panel");
    }
}