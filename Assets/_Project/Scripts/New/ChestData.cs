using UnityEngine;

[CreateAssetMenu(fileName = "NewChest", menuName = "RPG/Chest Data")]
public class ChestData : ScriptableObject
{
    public string chestID;
    public string chestName = "Rương kho báu";
    public ChestRewardItem[] items;
    public Sprite chestIcon;
}

[System.Serializable]
public class ChestRewardItem
{
    public ItemData item;
    public int amount = 1;
}