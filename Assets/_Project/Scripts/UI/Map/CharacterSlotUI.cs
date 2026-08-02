// CharacterSlotUI.cs (cập nhật)
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSlotUI : MonoBehaviour
{
    [Header("References (kéo tay hoặc tự tìm theo tên)")]
    public Image portrait;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;      // ✅ Chỉ hiển thị số
    public TextMeshProUGUI orderText;
    public Slider expSlider;
    public TextMeshProUGUI expText;        // Hiển thị "xxx/xxx"

    private CharacterData currentCharacter;
    private int currentLevel;
    private int currentOrder;

    private void Awake()
    {
        if (portrait   == null) portrait   = FindInChildren<Image>("Portrait");
        if (nameText   == null) nameText   = FindInChildren<TextMeshProUGUI>("Name");
        if (levelText  == null) levelText  = FindInChildren<TextMeshProUGUI>("Level");
        if (orderText  == null) orderText  = FindInChildren<TextMeshProUGUI>("Order");
        if (expSlider  == null) expSlider  = FindInChildren<Slider>("ExpSlider");
        if (expText    == null) expText    = FindInChildren<TextMeshProUGUI>("ExpText");
    }

    public void Setup(CharacterData data, int level, int order, float expProgress = -1f, int currentExp = 0, int neededExp = 0)
    {
        if (data == null) return;
        currentCharacter = data;
        currentLevel = level;
        currentOrder = order;

        // Portrait
        if (portrait != null)
        {
            var sprite = data.portrait != null ? data.portrait : data.battleSprite;
            if (sprite != null)
            {
                portrait.sprite = sprite;
                portrait.enabled = true;
            }
            else portrait.enabled = false;
        }

        // Tên
        if (nameText != null) nameText.text = data.characterName;

        // ✅ Level – chỉ hiển thị số (không có chữ "Lv.")
        if (levelText != null) levelText.text = level.ToString();

        // Số thứ tự
        if (orderText != null) orderText.text = order.ToString();

        // EXP Slider
        if (expSlider != null)
        {
            if (expProgress >= 0f) expSlider.value = expProgress;
            else expSlider.value = 0f;
        }

        // EXP Text (xxx/xxx)
        if (expText != null)
        {
            if (neededExp > 0 && currentExp >= 0)
                expText.text = $"{currentExp}/{neededExp}";
            else if (expProgress >= 0f)
                expText.text = $"{(int)(expProgress * neededExp)}/{neededExp}"; // fallback
            else
                expText.text = "";
        }
    }

    private T FindInChildren<T>(string childName) where T : Component
    {
        foreach (T comp in GetComponentsInChildren<T>(true))
            if (comp.gameObject.name == childName) return comp;
        return null;
    }

    public CharacterData GetCharacter() => currentCharacter;
    public int GetLevel() => currentLevel;
    public int GetOrder() => currentOrder;
}