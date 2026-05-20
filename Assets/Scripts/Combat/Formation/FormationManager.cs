using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Thêm dòng này

public class FormationManager : MonoBehaviour
{
    [Header("Formation UI Panel (sẽ hiện khi nhấn F)")]
    public GameObject formationPanel;

    [Header("Grid (3x3)")]
    public Transform gridContainer;
    public GameObject slotPrefab;

    [Header("Character List")]
    public Transform rosterContainer;
    public GameObject characterIconPrefab;
    public CharacterData[] availableCharacters;

    [Header("Mapping: UI slot index -> Combat slot index (0-8)")]
    public int[] uiToCombatSlot = new int[9] { 6, 3, 0, 7, 4, 1, 8, 5, 2 };

    [Header("Counter")]
    public TextMeshProUGUI counterText; // Kéo Text vào đây

    private SlotUI[] slots = new SlotUI[9];
    private FormationData currentFormation = new FormationData { slots = new FormationSlot[9] };
    private const int MAX_UNITS = 5;
    private bool isFormationUIOpen = false;

    private Dictionary<CharacterData, CharacterDragItem> rosterItemMap
        = new Dictionary<CharacterData, CharacterDragItem>();

    void Start()
    {
        BuildGrid();
        BuildRoster();
        formationPanel.SetActive(false);
        UpdateCounter(); // Khởi tạo hiển thị 0/5
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isFormationUIOpen = !isFormationUIOpen;
            formationPanel.SetActive(isFormationUIOpen);
            if (isFormationUIOpen) UpdateCounter();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            int count = currentFormation.slots.Count(s => s != null && s.data != null);
            if (count > 0)
                SaveAndStartCombat();
            else
                Debug.Log("Chưa có nhân vật nào trong đội hình!");
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
        rosterItemMap.Clear();
        foreach (var cd in availableCharacters)
        {
            var go = Instantiate(characterIconPrefab, rosterContainer);
            var drag = go.GetComponent<CharacterDragItem>();
            drag.Initialize(cd, this);
            rosterItemMap[cd] = drag;
        }
    }

    private void UpdateCounter()
    {
        int count = currentFormation.slots.Count(s => s != null && s.data != null);
        if (counterText != null)
            counterText.text = $"{count}/{MAX_UNITS}";
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

    public bool TryPlaceCharacter(CharacterData character, int uiSlotIndex)
    {
        if (character == null) return false;
        if (IsCharacterAlreadyPlaced(character)) return false;

        int currentCount = currentFormation.slots.Count(s => s != null && s.data != null);
        if (currentCount >= MAX_UNITS) return false;

        if (currentFormation.slots[uiSlotIndex]?.data != null) return false;

        ClearSlot(uiSlotIndex);
        currentFormation.slots[uiSlotIndex] = new FormationSlot
        {
            data = character,
            level = 1,
            gridSlot = uiSlotIndex
        };
        slots[uiSlotIndex].SetCharacter(character);
        SetRosterVisible(character, false);
        UpdateCounter(); // Cập nhật sau khi thêm
        return true;
    }

    public void RemoveCharacter(int uiSlotIndex)
    {
        ClearSlot(uiSlotIndex);
        UpdateCounter(); // Cập nhật sau khi xóa
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
                currentFormation.slots[toSlot] = new FormationSlot
                {
                    data = charFrom,
                    level = currentFormation.slots[fromSlot].level,
                    gridSlot = toSlot
                };
                ClearSlotInternal(fromSlot);
                slots[toSlot].SetCharacter(charFrom);
                UpdateCounter(); // Số lượng không đổi nhưng gọi để đồng bộ (nếu cần)
            }
        }
        else
        {
            var levelFrom = currentFormation.slots[fromSlot].level;
            var levelTo = currentFormation.slots[toSlot].level;

            currentFormation.slots[fromSlot] = new FormationSlot
            {
                data = charTo,
                level = levelTo,
                gridSlot = fromSlot
            };
            currentFormation.slots[toSlot] = new FormationSlot
            {
                data = charFrom,
                level = levelFrom,
                gridSlot = toSlot
            };
            slots[fromSlot].SetCharacter(charTo);
            slots[toSlot].SetCharacter(charFrom);
            UpdateCounter();
        }
    }

    void ClearSlot(int uiSlotIndex)
    {
        if (currentFormation.slots[uiSlotIndex] != null)
        {
            var removedChar = currentFormation.slots[uiSlotIndex].data;
            currentFormation.slots[uiSlotIndex] = null;
            slots[uiSlotIndex].Clear();
            SetRosterVisible(removedChar, true);
        }
    }

    void ClearSlotInternal(int uiSlotIndex)
    {
        if (currentFormation.slots[uiSlotIndex] != null)
        {
            currentFormation.slots[uiSlotIndex] = null;
            slots[uiSlotIndex].Clear();
        }
    }

    bool IsCharacterAlreadyPlaced(CharacterData character)
    {
        return currentFormation.slots.Any(s => s != null && s.data == character);
    }

    void SaveAndStartCombat()
    {
        var mappedFormation = new FormationData { slots = new FormationSlot[9] };
        for (int uiIdx = 0; uiIdx < currentFormation.slots.Length; uiIdx++)
        {
            if (currentFormation.slots[uiIdx] != null)
            {
                int combatSlot = uiToCombatSlot[uiIdx];
                mappedFormation.slots[combatSlot] = new FormationSlot
                {
                    data = currentFormation.slots[uiIdx].data,
                    level = currentFormation.slots[uiIdx].level,
                    gridSlot = combatSlot
                };
            }
        }
        FormationDataStorage.PendingFormation = mappedFormation;
        SceneManager.LoadScene("CombatScene");
    }

    public int GetSlotAtPosition(Vector2 screenPos)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].IsPointerOver(screenPos))
                return i;
        return -1;
    }
}