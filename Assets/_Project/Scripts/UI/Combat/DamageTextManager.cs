using System.Collections.Generic;
using UnityEngine;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    public GameObject damageTextPrefab;
    public int poolSize = 20;

    [Header("Damage Colors (Overrides)")]
    [Tooltip("Màu cho sát thương vật lý. Để trống sẽ dùng màu mặc định.")]
    public Color physicalColor = new Color(1f, 0.5f, 0f); // cam
    [Tooltip("Màu cho sát thương phép. Để trống sẽ dùng màu mặc định.")]
    public Color magicalColor = new Color(0.6f, 0f, 1f); // tím
    [Tooltip("Màu cho sát thương chuẩn (true damage). Để trống sẽ dùng màu mặc định.")]
    public Color trueColor = Color.white;

    // Các màu mặc định (fallback)
    private static readonly Color DefaultPhysical = new Color(1f, 0.5f, 0f);
    private static readonly Color DefaultMagical = new Color(0.6f, 0f, 1f);
    private static readonly Color DefaultTrue = Color.white;

    private List<DamageText> _pool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
            DamageText dt = obj.GetComponent<DamageText>();
            if (dt == null) dt = obj.AddComponent<DamageText>();
            _pool.Add(dt);
        }
    }

    private DamageText GetPooledObject()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].gameObject.activeInHierarchy)
                return _pool[i];
        }

        GameObject obj = Instantiate(damageTextPrefab, transform);
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt == null) dt = obj.AddComponent<DamageText>();
        _pool.Add(dt);
        poolSize++;
        return dt;
    }

    /// <summary>
    /// Lấy màu cho loại sát thương (ưu tiên override nếu có).
    /// </summary>
    private Color GetColorForDamageType(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return physicalColor.a > 0 ? physicalColor : DefaultPhysical;
            case DamageType.Magical:
                return magicalColor.a > 0 ? magicalColor : DefaultMagical;
            case DamageType.True:
                return trueColor.a > 0 ? trueColor : DefaultTrue;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Hiển thị damage text tại vị trí world.
    /// </summary>
    public void ShowDamage(int damage, Vector3 worldPosition, DamageType damageType, Vector2 direction, bool isFinalHit = false)
    {
        Color color = GetColorForDamageType(damageType);

        DamageText text = GetPooledObject();
        text.gameObject.SetActive(true);
        text.Show(damage, worldPosition, color, direction, isFinalHit);
    }

    // Overload không direction (dùng random)
    public void ShowDamage(int damage, Vector3 worldPosition, DamageType damageType, bool isFinalHit = false)
    {
        ShowDamage(damage, worldPosition, damageType, Vector2.up, isFinalHit);
    }

    // Overload màu tùy chỉnh
    public void ShowDamage(int damage, Vector3 worldPosition, Color textColor, Vector2 direction, bool isFinalHit = false)
    {
        DamageText text = GetPooledObject();
        text.gameObject.SetActive(true);
        text.Show(damage, worldPosition, textColor, direction, isFinalHit);
    }

    // Overload cũ để tương thích
    public void ShowDamage(int damage, Vector3 worldPosition, bool isFinalHit = false, bool isCrit = false)
    {
        Color color = isCrit ? Color.yellow : Color.white;
        ShowDamage(damage, worldPosition, color, Vector2.up, isFinalHit);
    }
}