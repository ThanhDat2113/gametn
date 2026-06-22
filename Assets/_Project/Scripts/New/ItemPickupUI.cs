using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPickupUI : MonoBehaviour
{
    public static ItemPickupUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI itemNameText;
    public Image itemIcon;
    public Button confirmButton;

    private System.Action onConfirmCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        panel.SetActive(false);
        confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    public void Show(ItemData item, System.Action onConfirm)
    {
        onConfirmCallback = onConfirm;
        if (itemNameText != null)
            itemNameText.text = item != null ? item.itemName : "Vật phẩm không xác định";
        if (itemIcon != null && item != null)
            itemIcon.sprite = item.icon;
        panel.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        panel.SetActive(false);
        var callback = onConfirmCallback;
        onConfirmCallback = null;
        callback?.Invoke();
    }
}