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

    [Header("Status & Buff Colors (Overrides)")]
    [Tooltip("Màu cho hiệu ứng Burn (thiêu đốt).")]
    public Color burnColor = new Color(1f, 0.3f, 0.1f); // cam đỏ
    [Tooltip("Màu cho hiệu ứng Stun (choáng).")]
    public Color stunColor = new Color(0.6f, 0.2f, 1f); // tím
    [Tooltip("Màu cho buff tăng sát thương (DMG UP).")]
    public Color damageUpColor = new Color(1f, 0.4f, 0.4f); // đỏ nhạt
    [Tooltip("Màu cho buff tăng phòng thủ (DEF UP).")]
    public Color defenseUpColor = new Color(0.8f, 0.8f, 0.85f); // bạc

    [Header("New Passive Colors (Overrides)")]
    [Tooltip("Màu cho hiệu ứng Nọc Độc (Nhện).")]
    public Color poisonColor = new Color(0f, 0.8f, 0f); // xanh lá độc
    [Tooltip("Màu cho hiệu ứng Bầy Đàn (Goblin).")]
    public Color packColor = new Color(1f, 0.5f, 0f); // cam
    [Tooltip("Màu cho hiệu ứng Thịnh Nộ (Orc).")]
    public Color rageColor = new Color(1f, 0f, 0f); // đỏ
    [Tooltip("Màu cho hiệu ứng Bào Tử Nổ (Nấm).")]
    public Color explosionColor = new Color(0.6f, 0f, 0f); // đỏ thẫm
    [Tooltip("Màu cho hiệu ứng Kỷ Luật Sắt (Lính tinh nhuệ).")]
    public Color ironColor = new Color(0.8f, 0.8f, 0.85f); // bạc
    [Tooltip("Màu cho hiệu ứng Hồi Sinh (Skeleton, Hassan, Madara).")]
    public Color reviveColor = Color.yellow; // vàng
    [Tooltip("Màu cho hiệu ứng Ảo Ảnh (Hassan, Madara).")]
    public Color illusionColor = new Color(0.6f, 0.2f, 1f); // tím
    [Tooltip("Màu cho hiệu ứng Huyết Mạch (Reinhard).")]
    public Color bloodColor = new Color(0.8f, 0f, 0f); // đỏ máu
    [Tooltip("Màu cho hiệu ứng Tích Lũy (Gilgamesh).")]
    public Color accumulateColor = new Color(1f, 0.5f, 0f); // cam
    [Tooltip("Màu cho hiệu ứng Dính (Slime).")]
    public Color stickyColor = new Color(0.3f, 0.6f, 1f); // xanh dương
    [Tooltip("Màu cho hiệu ứng Sói Đi Săn (Wolf).")]
    public Color wolfColor = new Color(0.7f, 0.7f, 0.7f); // xám
    [Tooltip("Màu cho hiệu ứng Hồi Máu (Treant Sinh Khí).")]
    public Color healColor = Color.green; // xanh lá

    // Các màu mặc định (fallback)
    private static readonly Color DefaultPhysical = new Color(1f, 0.5f, 0f);
    private static readonly Color DefaultMagical = new Color(0.6f, 0f, 1f);
    private static readonly Color DefaultTrue = Color.white;
    private static readonly Color DefaultBurn = new Color(1f, 0.3f, 0.1f);
    private static readonly Color DefaultStun = new Color(0.6f, 0.2f, 1f);
    private static readonly Color DefaultDamageUp = new Color(1f, 0.4f, 0.4f);
    private static readonly Color DefaultDefenseUp = new Color(0.8f, 0.8f, 0.85f);

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

    /// <summary>
    /// Lấy màu cho hiệu ứng status/buff (ưu tiên override nếu có).
    /// </summary>
    private Color GetColorForStatus(StatusEffectType status)
    {
        switch (status)
        {
            case StatusEffectType.ThieuDot:
                return burnColor.a > 0 ? burnColor : DefaultBurn;
            case StatusEffectType.Stun:
                return stunColor.a > 0 ? stunColor : DefaultStun;
            case StatusEffectType.SieuViet:
            case StatusEffectType.YChi:
            case StatusEffectType.BuiSao:
            case StatusEffectType.Empowered:
                return damageUpColor.a > 0 ? damageUpColor : DefaultDamageUp;
            case StatusEffectType.GiamSatThuong:
            case StatusEffectType.ThuThe:
                return defenseUpColor.a > 0 ? defenseUpColor : DefaultDefenseUp;
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Hiển thị text status (STUN!, BURN!, v.v.) tại vị trí world.
    /// </summary>
    public void ShowStatusText(string text, Vector3 worldPosition, StatusEffectType status, Vector2 direction)
    {
        Color color = GetColorForStatus(status);
        ShowStatusText(text, worldPosition, color, direction);
    }

/// <summary>
    /// Hiển thị text status với màu tùy chỉnh.
    /// </summary>
    public void ShowStatusText(string text, Vector3 worldPosition, Color textColor, Vector2 direction)
    {
        DamageText textObj = GetPooledObject();
        textObj.gameObject.SetActive(true);
        textObj.Show(text, worldPosition, textColor, direction, false, true, false);
    }

    /// <summary>
    /// Hiển thị text buff (DEF UP!, DMG UP!, v.v.) tại vị trí world.
    /// Buff text bay lên nhẹ nhàng, có offset riêng.
    /// </summary>
    public void ShowBuffText(string text, Vector3 worldPosition, StatType stat, bool isBuff, Vector2 direction)
    {
        Color color;
        if (isBuff)
        {
            // Buff tăng phòng thủ → màu bạc; tăng sát thương → màu đỏ nhạt
            if (stat == StatType.PDEF || stat == StatType.MDEF)
                color = defenseUpColor.a > 0 ? defenseUpColor : DefaultDefenseUp;
            else
                color = damageUpColor.a > 0 ? damageUpColor : DefaultDamageUp;
        }
        else
        {
            // Debuff → đỏ
            color = new Color(1f, 0.3f, 0.3f);
        }

        // Buff text: bay lên nhẹ nhàng với isBuff = true
        // Buff text offset thấp hơn damage text (phía dưới) để tách biệt
        Vector3 buffPos = worldPosition + Vector3.down * 0.5f;
        DamageText textObj = GetPooledObject();
        textObj.gameObject.SetActive(true);
        textObj.Show(text, buffPos, color, direction, false, true, true);
    }
}
