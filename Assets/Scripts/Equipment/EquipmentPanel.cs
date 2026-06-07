// EquipmentPanel.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentPanel : MonoBehaviour
{
    [Header("Character Selection")]
    public Transform characterContainer;
    public GameObject characterSlotPrefab;

    [Header("Equipment Slots")]
    public EquipmentSlotUI weaponSlot;
    public EquipmentSlotUI helmetSlot;
    public EquipmentSlotUI armorSlot;
    public EquipmentSlotUI accessorySlot;

    [Header("Equipment List")]
    public EquipmentListUI equipmentList;

    [Header("Character Battle Sprite Display")]
    public Image characterBattleSpriteDisplay;   // Image ở giữa panel

    private List<CharacterSlotUI> characterSlots = new List<CharacterSlotUI>();
    private CharacterData selectedCharacter;

    void OnEnable()
    {
        RefreshCharacterList();
        if (selectedCharacter == null && characterSlots.Count > 0)
            SelectCharacter(characterSlots[0].GetCharacter());
    }

    public void RefreshCharacterList()
    {
        // Xóa các slot cũ
        foreach (var slot in characterSlots)
            if (slot != null) Destroy(slot.gameObject);
        characterSlots.Clear();

        var formationMgr = FindFirstObjectByType<FormationManager>();
        if (formationMgr == null) return;

        // Lấy tất cả nhân vật người chơi sở hữu (đã mở khóa)
        var allCharacters = formationMgr.availableCharacters;
        if (allCharacters == null || allCharacters.Length == 0) return;

        // Duyệt qua tất cả nhân vật và tạo slot, order chạy từ 1 theo thứ tự trong mảng
        for (int i = 0; i < allCharacters.Length; i++)
        {
            var character = allCharacters[i];
            if (character == null) continue;

            // Lấy level thực từ PlayerProgression (nếu có)
            int level = 1;
            if (PlayerProgression.Instance != null)
                level = PlayerProgression.Instance.GetLevel(character);

            GameObject go = Instantiate(characterSlotPrefab, characterContainer);
            var slotUI = go.GetComponent<CharacterSlotUI>();
            if (slotUI != null)
            {
                // order = i+1 để hiển thị 1,2,3,... theo thứ tự trong danh sách
                slotUI.Setup(character, level, i + 1);

                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                CharacterData captured = character;
                btn.onClick.AddListener(() => SelectCharacter(captured));

                characterSlots.Add(slotUI);
            }
        }
    }

    private void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;

        // Hiển thị battle sprite với tỷ lệ gốc
        if (characterBattleSpriteDisplay != null)
        {
            characterBattleSpriteDisplay.sprite = character.battleSprite;
            characterBattleSpriteDisplay.preserveAspect = true;
        }

        // Cập nhật các slot trang bị
        weaponSlot.Initialize(this, EquipmentSlot.Weapon, character);
        helmetSlot.Initialize(this, EquipmentSlot.Helmet, character);
        armorSlot.Initialize(this, EquipmentSlot.Armor, character);
        accessorySlot.Initialize(this, EquipmentSlot.Accessory, character);

        if (equipmentList != null)
        {
            equipmentList.Initialize(this);
            equipmentList.Refresh();
        }
    }

    public void RefreshEquipmentList()
    {
        equipmentList.Refresh();
        if (selectedCharacter != null)
        {
            weaponSlot.Refresh();
            helmetSlot.Refresh();
            armorSlot.Refresh();
            accessorySlot.Refresh();
        }
    }

    public CharacterData GetSelectedCharacter() => selectedCharacter;

    public bool TryGoBack() => false;
}