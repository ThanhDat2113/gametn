using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory
{
    public List<InventorySlot> slots = new List<InventorySlot>();
    public event Action OnInventoryChanged;

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null) return;

        // Nếu có thể stack và đã tồn tại trong túi
        if (item.maxStack > 1)
        {
            var existing = slots.Find(s => s.item == item);
            if (existing != null)
            {
                existing.amount += amount;
                existing.amount = Mathf.Min(existing.amount, item.maxStack);
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        // Thêm mới
        slots.Add(new InventorySlot(item, amount));
        OnInventoryChanged?.Invoke();
    }

    public void RemoveItem(ItemData item, int amount = 1)
    {
        var existing = slots.Find(s => s.item == item);
        if (existing == null) return;

        existing.amount -= amount;
        if (existing.amount <= 0)
            slots.Remove(existing);
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(ItemData item, int amount = 1)
    {
        var existing = slots.Find(s => s.item == item);
        return existing != null && existing.amount >= amount;
    }

    public void Clear()
    {
        slots.Clear();
        OnInventoryChanged?.Invoke();
    }
}

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public InventorySlot(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}