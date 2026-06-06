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
        var equip = EquipmentManager.Instance.GetEquipment(currentCharacter).GetEquipment(slotType);
        if (equip != null && icon != null)
        {
            icon.sprite = equip.icon;
            icon.enabled = true;
            icon.color = Color.white;
        }
        else if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dragItem = eventData.pointerDrag?.GetComponent<EquipmentDragItem>();
        if (dragItem != null && dragItem.Equipment != null)
        {
            if (dragItem.Equipment.slot == slotType)
            {
                EquipmentManager.Instance.Equip(currentCharacter, slotType, dragItem.Equipment);
                Refresh();
                panel.RefreshEquipmentList(); // Cập nhật danh sách trang bị (xóa item đã gắn)
                dragItem.DestroyGhost();
            }
            else
            {
                Debug.Log($"Cannot equip {dragItem.Equipment.slot} into {slotType} slot");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Click phải để tháo trang bị
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            EquipmentManager.Instance.Unequip(currentCharacter, slotType);
            Refresh();
            panel.RefreshEquipmentList();
        }
    }
}