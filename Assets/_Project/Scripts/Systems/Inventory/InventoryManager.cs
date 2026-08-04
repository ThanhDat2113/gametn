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
        const int maxAttempts = 5;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using (FileStream file = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    bf.Serialize(file, inventory);
                }
                Debug.Log("Inventory saved.");
                break;
            }
            catch (IOException ex)
            {
                // Possible sharing violation or transient IO error - retry a few times.
                if (attempt == maxAttempts - 1)
                {
                    Debug.LogError($"Failed to save inventory after {maxAttempts} attempts: {ex}");
                }
                else
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Unexpected error while saving inventory: " + ex);
                break;
            }
        }
    }

    void LoadFromFile()
    {
        if (File.Exists(savePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                using (FileStream file = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (file.Length == 0)
                    {
                        Debug.LogWarning("Save file is empty — deleting and starting fresh.");
                        file.Close();
                        try { File.Delete(savePath); } catch { }
                        inventory = new Inventory();
                        return;
                    }

                    inventory = (Inventory)bf.Deserialize(file);
                    Debug.Log("Inventory loaded.");
                }
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                Debug.LogWarning("Save file corrupted or incomplete — deleting and starting fresh.");
                try { File.Delete(savePath); } catch { }
                inventory = new Inventory();
            }
            catch (IOException ex)
            {
                Debug.LogError("IO error while loading inventory: " + ex);
                inventory = new Inventory();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Unexpected error while loading inventory: " + ex);
                inventory = new Inventory();
            }
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