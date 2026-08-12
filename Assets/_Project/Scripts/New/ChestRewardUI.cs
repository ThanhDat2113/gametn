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

    [Header("Settings")]
    public float autoCloseDelay = 3f;

    private Coroutine _autoCloseCoroutine;

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

        panel.SetActive(true);

        if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
        _autoCloseCoroutine = StartCoroutine(AutoClose());
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