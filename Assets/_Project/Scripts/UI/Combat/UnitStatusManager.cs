using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitStatusManager : MonoBehaviour
{
    [Header("References")]
    public Transform slotContainer;
    public GameObject slotPrefab;

    private CombatManager combat;
    private Dictionary<CombatUnit, UnitStatusSlot> unitSlots = new Dictionary<CombatUnit, UnitStatusSlot>();

    void Awake()
    {
        combat = CombatManager.Instance;
        if (combat == null)
        {
            Debug.LogError("UnitStatusManager: CombatManager not found!");
            return;
        }

        combat.OnCombatStarted += OnCombatStarted;
        combat.OnVictory += ClearAllSlots;
        combat.OnDefeat += ClearAllSlots;
    }

    void Start()
    {
        if (combat == null) return;

        if (combat.CurrentPhase != CombatPhase.None)
        {
            Debug.Log($"[UnitStatusManager] Combat already in phase {combat.CurrentPhase}, creating slots.");
            OnCombatStarted();
        }
    }

    private void OnDestroy()
    {
        if (combat == null) return;
        combat.OnCombatStarted -= OnCombatStarted;
        combat.OnVictory -= ClearAllSlots;
        combat.OnDefeat -= ClearAllSlots;
    }

    private void OnCombatStarted()
    {
        Debug.Log("[UnitStatusManager] OnCombatStarted called");
        StartCoroutine(CreateSlotsDelayed());
    }

    private IEnumerator CreateSlotsDelayed()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            yield return null;
            if (combat.PlayerUnits.Count > 0)
            {
                ClearAllSlots();
                CreateSlots();
                yield break;
            }
            Debug.Log($"[UnitStatusManager] Attempt {attempt + 1}: PlayerUnits empty, retrying...");
        }
        Debug.LogWarning("[UnitStatusManager] PlayerUnits still empty after 5 frames!");
    }

    private void CreateSlots()
    {
        var playerUnits = combat.PlayerUnits.Where(u => u.IsAlive).ToList();
        Debug.Log($"[UnitStatusManager] Creating {playerUnits.Count} slots.");
        
        foreach (var unit in playerUnits)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            var slot = slotObj.GetComponent<UnitStatusSlot>();
            if (slot == null) slot = slotObj.AddComponent<UnitStatusSlot>();
            slot.Setup(unit);
            unitSlots[unit] = slot;
        }
    }

    // Overload cho OnVictory (có tham số) gọi ClearAllSlots gốc
    private void ClearAllSlots(Dictionary<CharacterData, int> _)
    {
        ClearAllSlots();
    }

    private void ClearAllSlots()
    {
        foreach (var slot in unitSlots.Values)
            if (slot != null) Destroy(slot.gameObject);
        unitSlots.Clear();
    }
}