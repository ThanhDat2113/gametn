using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FormationManager : MonoBehaviour
{
    // ─── Event ────────────────────────────────────────────────
    public event System.Action OnFormationChanged;

    [Header("Formation UI Panel (sẽ hiện khi nhấn F)")]
    public GameObject formationPanel;

    [Header("Grid (3x3)")]
    public Transform gridContainer;
    public GameObject slotPrefab;

    [Header("Character List")]
    public Transform rosterContainer;
    public GameObject characterIconPrefab;
    public CharacterData[] availableCharacters;

    [Header("Default Character (must always be present if formation would be empty)")]
    public CharacterData defaultCharacter;

    [Header("Mapping: UI slot index -> Combat slot index (0-8)")]
    public int[] uiToCombatSlot = new int[9] { 6, 3, 0, 7, 4, 1, 8, 5, 2 };

    [Header("Counter")]
    public TextMeshProUGUI counterText;

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

    private void EnsureAtLeastOneCharacter()
    {
        int currentCount = currentFormation.slots.Count(s => s != null && s.data != null);
        if (currentCount > 0) return;

        CharacterData defaultChar = defaultCharacter;
        if (defaultChar == null && availableCharacters.Length > 0)
            defaultChar = availableCharacters[0];

        if (defaultChar == null)
        {
            Debug.LogError("[FormationManager] Không có defaultCharacter và không có availableCharacters nào! Không thể tạo đội hình.");
            return;
        }

        int emptySlotIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (currentFormation.slots[i] == null || currentFormation.slots[i].data == null)
            {
                emptySlotIndex = i;
                break;
            }
        }

        if (emptySlotIndex == -1)
        {
            Debug.LogWarning("[FormationManager] Không tìm thấy ô trống để đặt nhân vật mặc định!");
            return;
        }

        currentFormation.slots[emptySlotIndex] = new FormationSlot
        {
            data = defaultChar,
            level = 1,
            gridSlot = emptySlotIndex
        };
        slots[emptySlotIndex].SetCharacter(defaultChar);
        SetRosterVisible(defaultChar, false);
        OnFormationChanged?.Invoke();
        Debug.Log($"[FormationManager] Đã tự động thêm nhân vật mặc định '{defaultChar.characterName}' vào ô {emptySlotIndex}");
    }

    private bool CanRemoveCharacter(int uiSlotIndex)
    {
        int currentCount = currentFormation.slots.Count(s => s != null && s.data != null);
        if (currentCount == 1 && currentFormation.slots[uiSlotIndex]?.data != null)
        {
            Debug.Log("[FormationManager] Không thể gỡ nhân vật cuối cùng. Đội hình phải có ít nhất 1 người.");
            return false;
        }
        return true;
    }

    public bool TryPlaceCharacter(CharacterData character, int uiSlotIndex)
    {
        if (character == null) return false;
        if (IsCharacterAlreadyPlaced(character)) return false;

        int currentCount = currentFormation.slots.Count(s => s != null && s.data != null);
        if (currentCount >= MAX_UNITS) return false;

        if (currentFormation.slots[uiSlotIndex]?.data != null) return false;

        ClearSlot(uiSlotIndex);
<<<<<<< Updated upstream
        int savedLevel = FormationProgressHelper.GetCurrentLevel(character);
        currentFormation.slots[uiSlotIndex] = new FormationSlot
        {
            data = character,
            level = savedLevel,
=======

        // Lấy level thực từ PlayerProgression (hoặc mặc định 1)
        int characterLevel = 1;
        if (PlayerProgression.Instance != null)
            characterLevel = PlayerProgression.Instance.GetLevel(character);

        currentFormation.slots[uiSlotIndex] = new FormationSlot
        {
            data = character,
            level = characterLevel,
>>>>>>> Stashed changes
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
                currentFormation.slots[toSlot] = new FormationSlot
                {
                    data = charFrom,
                    level = currentFormation.slots[fromSlot].level,
                    gridSlot = toSlot
                };
                ClearSlotInternal(fromSlot);
                slots[toSlot].SetCharacter(charFrom);
                UpdateCounter();
                OnFormationChanged?.Invoke();
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
            OnFormationChanged?.Invoke();
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

    public void SaveFormation()
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
        Debug.Log($"[FormationManager] SaveFormation: đã lưu {currentFormation.slots.Count(s => s != null && s.data != null)} nhân vật.");
    }

    void SaveAndStartCombat()
    {
        SaveFormation();
        SceneManager.LoadScene("CombatScene");
    }

    public int GetSlotAtPosition(Vector2 screenPos)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].IsPointerOver(screenPos))
                return i;
        return -1;
    }

    public FormationData GetCurrentFormationData()
    {
        return currentFormation;
    }

    public void UnlockCharacter(CharacterData character)
    {
        if (character == null) return;

        if (rosterItemMap.ContainsKey(character))
        {
            Debug.LogWarning($"[FormationManager] '{character.characterName}' đã có trong roster.");
            return;
        }

        var go = Instantiate(characterIconPrefab, rosterContainer);
        var drag = go.GetComponent<CharacterDragItem>();
        drag.Initialize(character, this);
        rosterItemMap[character] = drag;

        var list = new List<CharacterData>(availableCharacters) { character };
        availableCharacters = list.ToArray();

        Debug.Log($"[FormationManager] Đã mở khóa nhân vật mới: '{character.characterName}'");
    }
}