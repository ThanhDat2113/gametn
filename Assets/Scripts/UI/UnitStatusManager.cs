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

    void Start()
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

    private void OnDestroy()
    {
        if (combat == null) return;
        combat.OnCombatStarted -= OnCombatStarted;
        combat.OnVictory -= ClearAllSlots;
        combat.OnDefeat -= ClearAllSlots;
    }

    private void OnCombatStarted()
    {
        ClearAllSlots();
        CreateSlots();
    }

    private void CreateSlots()
    {
        var playerUnits = combat.PlayerUnits.Where(u => u.IsAlive).ToList();

        foreach (var unit in playerUnits)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            var slot = slotObj.GetComponent<UnitStatusSlot>();
            if (slot == null) slot = slotObj.AddComponent<UnitStatusSlot>();
            slot.Setup(unit);
            unitSlots[unit] = slot;
        }
    }

    private void ClearAllSlots()
    {
        foreach (var slot in unitSlots.Values)
            if (slot != null) Destroy(slot.gameObject);
        unitSlots.Clear();
    }
}