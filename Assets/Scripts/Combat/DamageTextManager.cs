using System.Collections.Generic;
using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    public GameObject damageTextPrefab; // Kéo Prefab vào đây
    public int poolSize = 20;

    private List<DamageText> _pool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        _pool = new List<DamageText>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(damageTextPrefab, transform);
            obj.SetActive(false);
            _pool.Add(obj.GetComponent<DamageText>());
        }
    }

    public DamageText GetPooledObject()
    {
        // Tìm một đối tượng không hoạt động trong pool
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].gameObject.activeInHierarchy)
            {
                return _pool[i];
            }
        }

        // Nếu không có, tạo mới và thêm vào pool (để phòng trường hợp cần nhiều hơn)
        GameObject obj = Instantiate(damageTextPrefab, transform);
        DamageText newText = obj.GetComponent<DamageText>();
        _pool.Add(newText);
        poolSize++;
        return newText;
    }

    public void ShowDamage(int damage, Vector3 worldPosition, bool isFinalHit = false, bool isCrit = false)
    {
        DamageText text = GetPooledObject();
        text.gameObject.SetActive(true);
        text.Show(damage, worldPosition, isFinalHit, isCrit);
    }
}