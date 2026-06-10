using UnityEngine;

public enum ItemType
{
    Equipment,   // trang bị
    Material     // nguyên liệu
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemID;
    public string itemName;
    public ItemType itemType;
    public Sprite icon;
    [TextArea] public string description;
    public int maxStack = 1;
}