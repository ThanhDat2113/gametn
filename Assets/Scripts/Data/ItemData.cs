using UnityEngine;

public enum ItemType
{
    Equipment,   // trang bị
    Consumable,  // vật phẩm dùng một lần
    Quest,       // vật phẩm nhiệm vụ
    Material     // nguyên liệu
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemID;          // Mã định danh duy nhất (nên dùng GUID)
    public string itemName;
    public ItemType itemType;
    public Sprite icon;
    [TextArea] public string description;
    public int maxStack = 1;       // Tối đa xếp chồng (1 nếu không thể stack)
}