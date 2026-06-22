using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI amountText;

    public void Setup(ItemData item, int amount)
    {
        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = true;
            icon.color = Color.white;
        }
        if (amountText != null)
        {
            amountText.text = amount > 1 ? amount.ToString() : "";
            amountText.enabled = true;
        }
    }

    public void SetEmpty()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
            icon.color = Color.clear;
        }
        if (amountText != null)
        {
            amountText.text = "";
            amountText.enabled = false;
        }
    }
}