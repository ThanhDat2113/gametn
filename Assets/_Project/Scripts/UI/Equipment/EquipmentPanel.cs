using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public Image characterBattleSpriteDisplay;

    [Header("Stat Display")]
    public TextMeshProUGUI statHpText;
    public TextMeshProUGUI statAtkText;
    public TextMeshProUGUI statPdefText;
    public TextMeshProUGUI statMdefText;

    private List<CharacterSlotUI> characterSlots = new List<CharacterSlotUI>();
    private CharacterData selectedCharacter;

    private EquipmentData previewEquipment;
    private EquipmentSlot previewSlot;
    private bool isPreviewing = false;

    // ── Thêm flag drag preview ──
    private bool _isDraggingPreview = false;
    public bool IsDraggingPreview => _isDraggingPreview;

    void OnEnable()
    {
        RefreshCharacterList();
        if (selectedCharacter == null && characterSlots.Count > 0)
            SelectCharacter(characterSlots[0].GetCharacter());
    }

    void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
    }

    private void OnEquipmentChanged(CharacterData character)
    {
        if (character == selectedCharacter)
        {
            ClearPreview();
            RefreshStatsDisplay();
        }
    }

    public void RefreshCharacterList()
    {
        foreach (var slot in characterSlots)
            if (slot != null) Destroy(slot.gameObject);
        characterSlots.Clear();

        var formationMgr = FindFirstObjectByType<FormationManager>();
        if (formationMgr == null) return;

        var allCharacters = formationMgr.availableCharacters;
        if (allCharacters == null || allCharacters.Length == 0) return;

        for (int i = 0; i < allCharacters.Length; i++)
        {
            var character = allCharacters[i];
            if (character == null) continue;

            int level = 1;
            if (PlayerProgression.Instance != null)
                level = PlayerProgression.Instance.GetLevel(character);

            GameObject go = Instantiate(characterSlotPrefab, characterContainer);
            var slotUI = go.GetComponent<CharacterSlotUI>();
            if (slotUI != null)
            {
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

        if (characterBattleSpriteDisplay != null)
        {
            characterBattleSpriteDisplay.sprite = character.battleSprite;
            characterBattleSpriteDisplay.preserveAspect = true;
        }

        weaponSlot.Initialize(this, EquipmentSlot.Weapon, character);
        helmetSlot.Initialize(this, EquipmentSlot.Helmet, character);
        armorSlot.Initialize(this, EquipmentSlot.Armor, character);
        accessorySlot.Initialize(this, EquipmentSlot.Accessory, character);

        if (equipmentList != null)
        {
            equipmentList.Initialize(this);
            equipmentList.Refresh();
        }

        ClearPreview();
        RefreshStatsDisplay();

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
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
        ClearPreview();
    }

    public CharacterData GetSelectedCharacter() => selectedCharacter;
    public bool TryGoBack() => false;

    // ── Preview ──

    public void ShowPreview(EquipmentData equip)
    {
        // Nếu đang kéo preview, không cho thay đổi preview (giữ nguyên item đang kéo)
        if (_isDraggingPreview) return;

        if (selectedCharacter == null || equip == null) return;

        previewEquipment = equip;
        previewSlot = equip.slot;
        isPreviewing = true;

        GetCurrentTotalStats(selectedCharacter, out int curHp, out int curAtk, out int curPdef, out int curMdef);
        GetStatsWithEquipment(selectedCharacter, equip, previewSlot, out int newHp, out int newAtk, out int newPdef, out int newMdef);

        UpdateStatText(statHpText, curHp, newHp, "HP");
        UpdateStatText(statAtkText, curAtk, newAtk, "ATK");
        UpdateStatText(statPdefText, curPdef, newPdef, "P.DEF");
        UpdateStatText(statMdefText, curMdef, newMdef, "M.DEF");
    }

    // Preview stats khi kéo trang bị vào một slot cụ thể (bất kỳ loại nào)
    public void ShowPreviewForSlot(EquipmentData equip, EquipmentSlot slot)
    {
        if (selectedCharacter == null || equip == null) return;

        previewEquipment = equip;
        previewSlot = slot;
        isPreviewing = true;

        GetCurrentTotalStats(selectedCharacter, out int curHp, out int curAtk, out int curPdef, out int curMdef);
        GetStatsWithEquipment(selectedCharacter, equip, slot, out int newHp, out int newAtk, out int newPdef, out int newMdef);

        UpdateStatText(statHpText, curHp, newHp, "HP");
        UpdateStatText(statAtkText, curAtk, newAtk, "ATK");
        UpdateStatText(statPdefText, curPdef, newPdef, "P.DEF");
        UpdateStatText(statMdefText, curMdef, newMdef, "M.DEF");
    }

    public void ClearPreview()
    {
        _isDraggingPreview = false; // reset flag khi clear
        if (!isPreviewing) return;
        isPreviewing = false;
        previewEquipment = null;
        if (selectedCharacter != null)
            RefreshStatsDisplay();
    }

    // ── Drag Preview ──

    public void StartDragPreview(EquipmentData equip)
    {
        _isDraggingPreview = true;
        // Gọi ShowPreview trực tiếp (bỏ qua check flag vì ta đang set true)
        // Nhưng ShowPreview có check _isDraggingPreview nên sẽ bị chặn.
        // Ta gọi trực tiếp logic preview để override
        if (selectedCharacter == null || equip == null) return;

        previewEquipment = equip;
        previewSlot = equip.slot;
        isPreviewing = true;

        GetCurrentTotalStats(selectedCharacter, out int curHp, out int curAtk, out int curPdef, out int curMdef);
        GetStatsWithEquipment(selectedCharacter, equip, previewSlot, out int newHp, out int newAtk, out int newPdef, out int newMdef);

        UpdateStatText(statHpText, curHp, newHp, "HP");
        UpdateStatText(statAtkText, curAtk, newAtk, "ATK");
        UpdateStatText(statPdefText, curPdef, newPdef, "P.DEF");
        UpdateStatText(statMdefText, curMdef, newMdef, "M.DEF");
    }

    public void EndDragPreview(bool keepPreview = false)
    {
        _isDraggingPreview = false;
        if (!keepPreview)
            ClearPreview();
    }

    // ── Stat Helpers ──

    public void RefreshStatsDisplay()
    {
        if (selectedCharacter == null) return;
        GetCurrentTotalStats(selectedCharacter, out int hp, out int atk, out int pdef, out int mdef);
        SetStatText(statHpText, hp, "HP");
        SetStatText(statAtkText, atk, "ATK");
        SetStatText(statPdefText, pdef, "P.DEF");
        SetStatText(statMdefText, mdef, "M.DEF");
    }

    private void GetCurrentTotalStats(CharacterData character, out int hp, out int atk, out int pdef, out int mdef)
    {
        int level = 1;
        if (PlayerProgression.Instance != null)
            level = PlayerProgression.Instance.GetLevel(character);

        int baseHp = character.GetHP(level);
        int baseAtk = character.GetATK(level);
        int basePdef = character.GetPDEF(level);
        int baseMdef = character.GetMDEF(level);

        var equipment = EquipmentManager.Instance?.GetEquipment(character);
        if (equipment != null)
        {
            hp = baseHp + equipment.GetHPBonus();
            atk = baseAtk + equipment.GetATKBonus();
            pdef = basePdef + equipment.GetPDEFBonus();
            mdef = baseMdef + equipment.GetMDEFBonus();
        }
        else
        {
            hp = baseHp;
            atk = baseAtk;
            pdef = basePdef;
            mdef = baseMdef;
        }
    }

    private void GetStatsWithEquipment(CharacterData character, EquipmentData newEquip, EquipmentSlot slot,
        out int hp, out int atk, out int pdef, out int mdef)
    {
        GetCurrentTotalStats(character, out hp, out atk, out pdef, out mdef);

        var currentEquip = EquipmentManager.Instance?.GetEquipment(character)?.GetEquipment(slot);
        if (currentEquip != null)
        {
            hp -= currentEquip.hpBonus;
            atk -= currentEquip.atkBonus;
            pdef -= currentEquip.pdefBonus;
            mdef -= currentEquip.mdefBonus;
        }

        hp += newEquip.hpBonus;
        atk += newEquip.atkBonus;
        pdef += newEquip.pdefBonus;
        mdef += newEquip.mdefBonus;
    }

    private void UpdateStatText(TextMeshProUGUI text, int currentValue, int newValue, string statName)
    {
        if (text == null) return;
        int diff = newValue - currentValue;
        if (diff > 0)
            text.text = $"{statName}: {newValue} <color=#00AAFF>(+{diff})</color>";
        else if (diff < 0)
            text.text = $"{statName}: {newValue} <color=#FF5555>({diff})</color>";
        else
            text.text = $"{statName}: {newValue}";
    }

    private void SetStatText(TextMeshProUGUI text, int value, string statName)
    {
        if (text != null)
            text.text = $"{statName}: {value}";
    }

    public void OnClickBackground()
    {
        ClearPreview();
    }
}