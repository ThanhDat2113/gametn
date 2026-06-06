// EquipmentPanel.cs
using System.Collections.Generic;
using System.Linq;
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
        foreach (var slot in characterSlots)
            if (slot != null) Destroy(slot.gameObject);
        characterSlots.Clear();

        var formationMgr = FindFirstObjectByType<FormationManager>();
        if (formationMgr == null) return;

        var formation = formationMgr.GetCurrentFormationData();
        if (formation?.slots == null) return;

        var activeCharacters = formation.slots
            .Where(s => s != null && s.data != null)
            .Select(s => s.data)
            .Distinct()
            .ToList();

        foreach (var character in activeCharacters)
        {
            GameObject go = Instantiate(characterSlotPrefab, characterContainer);
            var slotUI = go.GetComponent<CharacterSlotUI>();
            if (slotUI != null)
            {
                int level = PlayerProgression.Instance?.GetLevel(character) ?? 1;
                slotUI.Setup(character, level, 0);
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
        weaponSlot.Initialize(this, EquipmentSlot.Weapon, character);
        helmetSlot.Initialize(this, EquipmentSlot.Helmet, character);
        armorSlot.Initialize(this, EquipmentSlot.Armor, character);
        accessorySlot.Initialize(this, EquipmentSlot.Accessory, character);
        equipmentList.Initialize(this);
        equipmentList.Refresh();
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