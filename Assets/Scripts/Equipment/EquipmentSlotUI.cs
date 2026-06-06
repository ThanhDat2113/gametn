// EquipmentSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("Slot Info")]
    public EquipmentSlot slotType;
    public CharacterData currentCharacter;

    [Header("Visual")]
    public Image icon;
    public TextMeshProUGUI slotNameText;

    private EquipmentPanel panel;

    private void Awake()
    {
        // ⭐ Đảm bảo có Image với raycastTarget=true để OnDrop fire được
        var bg = GetComponent<Image>();
        if (bg == null)
        {
            bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0); // trong suốt
        }
        bg.raycastTarget = true;
    }

    public void Initialize(EquipmentPanel parentPanel, EquipmentSlot type, CharacterData character)
    {
        panel = parentPanel;
        slotType = type;
        currentCharacter = character;
        if (slotNameText != null) slotNameText.text = type.ToString();
        Refresh();
    }

    public void Refresh()
    {
        if (currentCharacter == null) return;
        if (EquipmentManager.Instance == null) return;

        var equip = EquipmentManager.Instance.GetEquipment(currentCharacter).GetEquipment(slotType);
        if (icon == null) return;

        if (equip != null)
        {
            icon.sprite = equip.icon;
            icon.enabled = true;
            icon.color = Color.white;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dragItem = eventData.pointerDrag?.GetComponent<EquipmentDragItem>();
        if (dragItem == null || dragItem.Equipment == null) return;

        if (dragItem.Equipment.slot != slotType)
        {
            Debug.Log($"Cannot equip {dragItem.Equipment.slot} into {slotType} slot");
            return;
        }

        EquipmentManager.Instance.Equip(currentCharacter, slotType, dragItem.Equipment);
        Refresh();
        panel?.RefreshEquipmentList();
        // ⭐ KHÔNG gọi dragItem.DestroyGhost() ở đây — OnEndDrag sẽ xử lý
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            EquipmentManager.Instance.Unequip(currentCharacter, slotType);
            Refresh();
            panel?.RefreshEquipmentList();
        }
    }
}