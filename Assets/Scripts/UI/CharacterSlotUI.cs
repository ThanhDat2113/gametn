using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào prefab CharacterSlot trong CharacterContainer của MapMenuManager.
/// Prefab cần có các child GameObject với đúng tên sau:
///   - "Portrait"    → Image  (ảnh nhân vật)
///   - "Name"        → TextMeshProUGUI (tên nhân vật)
///   - "Level"       → TextMeshProUGUI (hiện "Lv. X")
///   - "Order"       → TextMeshProUGUI (số thứ tự: 1, 2, 3...)
///   - "ExpSlider"   → Slider (thanh exp %, 0-1)
/// Tất cả đều optional — thiếu cái nào thì bỏ qua cái đó.
/// </summary>
public class CharacterSlotUI : MonoBehaviour
{
    [Header("References (kéo tay hoặc tự tìm theo tên)")]
    public Image portrait;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI orderText;
    public Slider expSlider;

    private void Awake()
    {
        // Tự tìm nếu chưa gán
        if (portrait   == null) portrait   = FindInChildren<Image>("Portrait");
        if (nameText   == null) nameText   = FindInChildren<TextMeshProUGUI>("Name");
        if (levelText  == null) levelText  = FindInChildren<TextMeshProUGUI>("Level");
        if (orderText  == null) orderText  = FindInChildren<TextMeshProUGUI>("Order");
        if (expSlider  == null) expSlider  = FindInChildren<Slider>("ExpSlider");
    }

    /// <summary>
    /// Gọi từ MapMenuManager.RefreshCharacterContainer().
    /// </summary>
    public void Setup(CharacterData data, int level, int order, float expProgress = -1f)
    {
        if (data == null) return;

        // Portrait
        if (portrait != null)
        {
            var sprite = data.portrait != null ? data.portrait : data.battleSprite;
            if (sprite != null)
            {
                portrait.sprite = sprite;
                portrait.enabled = true;
            }
            else
            {
                portrait.enabled = false;
            }
        }

        // Tên
        if (nameText != null)
            nameText.text = data.characterName;

        // Level
        if (levelText != null)
            levelText.text = $"Lv. {level}";

        // Số thứ tự
        if (orderText != null)
            orderText.text = order.ToString();

        // EXP Slider
        if (expSlider != null)
        {
            if (expProgress >= 0f)
                expSlider.value = expProgress;
            else
                expSlider.value = 0f;
        }
    }

    // Helper tìm component trong children theo tên, kể cả object đang inactive
    private T FindInChildren<T>(string childName) where T : Component
    {
        foreach (T comp in GetComponentsInChildren<T>(true))
        {
            if (comp.gameObject.name == childName)
                return comp;
        }
        return null;
    }
}