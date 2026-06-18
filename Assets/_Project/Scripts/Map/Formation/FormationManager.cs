using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FormationManager : MonoBehaviour
{
    public event System.Action OnFormationChanged;

    [Header("Formation UI Panel (sẽ hiện khi nhấn F)")]
    public GameObject formationPanel;

    [Header("Grid (3x3)")]
    public Transform gridContainer;
    public GameObject slotPrefab;

    [Header("Character List")]
    public Transform rosterContainer;
    public GameObject characterIconPrefab;

    [Header("Nhân vật khởi đầu (chỉ kéo nhân vật bắt đầu game vào đây)")]
    public CharacterData[] startingCharacters;

    [Header("All Characters (danh sách đầy đủ tất cả nhân vật trong game)")]
    public CharacterData[] allCharacters;

    [Header("Default Character")]
    public CharacterData defaultCharacter;

    [Header("Mapping: UI slot index -> Combat slot index (0-8)")]
    public int[] uiToCombatSlot = new int[9] { 6, 3, 0, 7, 4, 1, 8, 5, 2 };

    [Header("Counter")]
    public TextMeshProUGUI counterText;

    private SlotUI[] slots = new SlotUI[9];
    private FormationData currentFormation = new FormationData { slots = new FormationSlot[9] };
    private const int MAX_UNITS = 5;
    private bool isFormationUIOpen = false;

    // Danh sách nhân vật đã mở khóa (có thể xem trong roster)
    private HashSet<CharacterData> unlockedCharacters = new HashSet<CharacterData>();
    private Dictionary<CharacterData, CharacterDragItem> rosterItemMap
        = new Dictionary<CharacterData, CharacterDragItem>();

    void Start()
    {
        BuildGrid();
        // Khởi tạo unlockedCharacters với startingCharacters
        foreach (var c in startingCharacters)
            if (c != null) unlockedCharacters.Add(c);
        BuildRoster();
        EnsureAtLeastOneCharacter();
        formationPanel.SetActive(false);
        UpdateCounter();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFormationUIOpen = !isFormationUIOpen;
            formationPanel.SetActive(isFormationUIOpen);
            if (isFormationUIOpen) RefreshRoster();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            int count = currentFormation.slots.Count(s => s != null && s.data != null);
            if (count > 0) SaveAndStartCombat();
            else Debug.Log("Chưa có nhân vật nào trong đội hình!");
        }
    }

    void BuildGrid()
    {
        for (int i = 0; i < 9; i++)
        {
            var go = Instantiate(slotPrefab, gridContainer);
            var slotUI = go.GetComponent<SlotUI>();
            slotUI.Initialize(i, this);
            slots[i] = slotUI;
        }
    }

    void BuildRoster()
    {
        // Xóa roster cũ
        foreach (var kvp in rosterItemMap)
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        rosterItemMap.Clear();

        // Chỉ hiển thị nhân vật đã mở khóa
        foreach (var cd in allCharacters)
        {
            if (cd == null || !unlockedCharacters.Contains(cd)) continue;
            var go = Instantiate(characterIconPrefab, rosterContainer);
            var drag = go.GetComponent<CharacterDragItem>();
            drag.Initialize(cd, this);
            rosterItemMap[cd] = drag;
        }
    }

    /// <summary>Refresh roster UI (gọi sau khi unlock)</summary>
    private void RefreshRoster()
    {
        BuildRoster();
        UpdateCounter();
    }

    private void UpdateCounter()
    {
        int count = currentFormation.slots.Count(s => s != null && s.data != null);
        if (counterText != null) counterText.text = $"{count}/{MAX_UNITS}";
    }

    private void SetRosterVisible(CharacterData character, bool visible)
    {
        if (character == null) return;
        if (rosterItemMap.TryGetValue(character, out var dragItem))
        {
            dragItem.gameObject.SetActive(visible);
            if (visible) dragItem.ResetVisual();
        }
    }

    private void EnsureAtLeastOneCharacter()
    {
        int currentCount = currentFormation.slots.Count(s => s != null && s.data != null);
        if (currentCount > 0) return;

        CharacterData defaultChar = defaultCharacter;
        if (defaultChar == null && startingCharacters.Length > 0)
            defaultChar = startingCharacters[0];
        if (defaultChar == null) return;

        int emptySlot = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (currentFormation.slots[i] == null || currentFormation.slots[i].data == null)
            { emptySlot = i; break; }
        }
        if (emptySlot == -1) return;

        currentFormation.slots[emptySlot] = new FormationSlot
        {
            data = defaultChar,
            level = 1,
            gridSlot = emptySlot
        };
        slots[emptySlot].SetCharacter(defaultChar);
        SetRosterVisible(defaultChar, false);
        OnFormationChanged?.Invoke();
    }

    private bool CanRemoveCharacter(int idx)
    {
        int count = currentFormation.slots.Count(s => s != null && s.data != null);
        return !(count == 1 && currentFormation.slots[idx]?.data != null);
    }

    public bool TryPlaceCharacter(CharacterData character, int uiSlotIndex)
    {
        if (character == null) return false;
        if (IsCharacterAlreadyPlaced(character)) return false;

        int currentCount = currentFormation.slots.Count(s => s != null && s.data != null);
        if (currentCount >= MAX_UNITS) return false;
        if (currentFormation.slots[uiSlotIndex]?.data != null) return false;

        ClearSlot(uiSlotIndex);

        int characterLevel = 1;
        if (PlayerProgression.Instance != null)
            characterLevel = PlayerProgression.Instance.GetLevel(character);

        currentFormation.slots[uiSlotIndex] = new FormationSlot
        {
            data = character,
            level = characterLevel,
            gridSlot = uiSlotIndex
        };
        slots[uiSlotIndex].SetCharacter(character, characterLevel);
        SetRosterVisible(character, false);
        UpdateCounter();
        OnFormationChanged?.Invoke();
        return true;
    }

    public void RemoveCharacter(int uiSlotIndex)
    {
        if (!CanRemoveCharacter(uiSlotIndex)) return;
        ClearSlot(uiSlotIndex);
        UpdateCounter();
        OnFormationChanged?.Invoke();
    }

    public void TrySwapCharacters(int fromSlot, int toSlot)
    {
        if (fromSlot == toSlot) return;
        var charFrom = currentFormation.slots[fromSlot]?.data;
        var charTo = currentFormation.slots[toSlot]?.data;

        if (charTo == null)
        {
            if (charFrom != null)
            {
                currentFormation.slots[toSlot] = new FormationSlot { data = charFrom, level = currentFormation.slots[fromSlot].level, gridSlot = toSlot };
                ClearSlotInternal(fromSlot);
                slots[toSlot].SetCharacter(charFrom);
                UpdateCounter();
                OnFormationChanged?.Invoke();
            }
        }
        else
        {
            var l1 = currentFormation.slots[fromSlot].level;
            var l2 = currentFormation.slots[toSlot].level;
            currentFormation.slots[fromSlot] = new FormationSlot { data = charTo, level = l2, gridSlot = fromSlot };
            currentFormation.slots[toSlot] = new FormationSlot { data = charFrom, level = l1, gridSlot = toSlot };
            slots[fromSlot].SetCharacter(charTo);
            slots[toSlot].SetCharacter(charFrom);
            UpdateCounter();
            OnFormationChanged?.Invoke();
        }
    }

    void ClearSlot(int idx)
    {
        if (currentFormation.slots[idx] != null)
        {
            var removed = currentFormation.slots[idx].data;
            currentFormation.slots[idx] = null;
            slots[idx].Clear();
            SetRosterVisible(removed, true);
        }
    }

    void ClearSlotInternal(int idx)
    {
        if (currentFormation.slots[idx] != null)
        {
            currentFormation.slots[idx] = null;
            slots[idx].Clear();
        }
    }

    bool IsCharacterAlreadyPlaced(CharacterData character)
    {
        return currentFormation.slots.Any(s => s != null && s.data == character);
    }

    public void SaveFormation()
    {
        var mapped = new FormationData { slots = new FormationSlot[9] };
        for (int i = 0; i < currentFormation.slots.Length; i++)
        {
            if (currentFormation.slots[i] != null)
            {
                int cs = uiToCombatSlot[i];
                int actualLevel = currentFormation.slots[i].level;
                if (PlayerProgression.Instance != null)
                    actualLevel = PlayerProgression.Instance.GetLevel(currentFormation.slots[i].data);
                mapped.slots[cs] = new FormationSlot { data = currentFormation.slots[i].data, level = actualLevel, gridSlot = cs };
            }
        }
        FormationDataStorage.PendingFormation = mapped;
    }

    void SaveAndStartCombat()
    {
        SaveFormation();
        SceneManager.LoadScene("CombatScene");
    }

    public int GetSlotAtPosition(Vector2 screenPos)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].IsPointerOver(screenPos)) return i;
        return -1;
    }

    public FormationData GetCurrentFormationData() => currentFormation;

    // Backward compatibility: EquipmentPanel và các script khác dùng availableCharacters
    public CharacterData[] availableCharacters
    {
        get { return allCharacters; }
    }

    /// <summary>Mở khóa nhân vật (gọi từ quest reward, vào allCharacters + unlocked)</summary>
    public void UnlockCharacter(CharacterData character)
    {
        if (character == null) return;
        if (unlockedCharacters.Contains(character)) return;

        unlockedCharacters.Add(character);

        // Thêm vào allCharacters nếu chưa có
        if (!allCharacters.Contains(character))
        {
            var list = new List<CharacterData>(allCharacters) { character };
            allCharacters = list.ToArray();
        }

        // Tạo UI item
        var go = Instantiate(characterIconPrefab, rosterContainer);
        var drag = go.GetComponent<CharacterDragItem>();
        drag.Initialize(character, this);
        rosterItemMap[character] = drag;

        Debug.Log($"[FormationManager] Đã mở khóa nhân vật mới: '{character.characterName}'");
    }
}