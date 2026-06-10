using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public Inventory inventory = new Inventory();

    [Header("Default Items (khởi tạo lần đầu)")]
    public ItemData[] startingItems;

    private string savePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        savePath = Application.persistentDataPath + "/inventory.dat";
        LoadInventory();
    }

    void Start()
    {
        // Nếu chưa có item nào (inventory trống) thì load mặc định
        if (inventory.slots.Count == 0 && startingItems.Length > 0)
        {
            foreach (var item in startingItems)
                inventory.AddItem(item, 1);
        }
    }

    public void AddItem(ItemData item, int amount = 1) => inventory.AddItem(item, amount);
    public void RemoveItem(ItemData item, int amount = 1) => inventory.RemoveItem(item, amount);
    public bool HasItem(ItemData item, int amount = 1) => inventory.HasItem(item, amount);
    public void SaveInventory() => SaveToFile();
    public void LoadInventory() => LoadFromFile();

    void SaveToFile()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(savePath);
        bf.Serialize(file, inventory);
        file.Close();
        Debug.Log("Inventory saved.");
    }

    void LoadFromFile()
    {
        if (File.Exists(savePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(savePath, FileMode.Open);
            inventory = (Inventory)bf.Deserialize(file);
            file.Close();
            Debug.Log("Inventory loaded.");
        }
        else
        {
            Debug.Log("No save file, starting fresh.");
        }
    }

    void OnApplicationQuit()
    {
        SaveInventory();
    }
}