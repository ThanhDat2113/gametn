using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị thông tin tiến trình cho một nhân vật sau chiến thắng.
/// </summary>
public class VictoryEntryUI : MonoBehaviour
{
    [Header("Visuals")]
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI gainedExpText;
    public Slider expBar;
    public TextMeshProUGUI expProgressText;

    public void Setup(CharacterData data, int gainedExp, int currentLevel, int currentExp, int neededExp)
    {
        // Ảnh đại diện
        if (portraitImage != null)
        {
            Sprite sprite = data.portrait != null ? data.portrait : data.battleSprite;
            portraitImage.sprite = sprite;
            portraitImage.preserveAspect = true;
        }

        // Tên
        if (nameText != null)
            nameText.text = data.characterName;

        // Cấp độ
        if (levelText != null)
            levelText.text = $"Cấp {currentLevel}";

        // EXP nhận được trong trận - định dạng (+xxx)
        if (gainedExpText != null)
            gainedExpText.text = $"(+{gainedExp})";

        // Thanh EXP
        float progress = neededExp > 0 ? (float)currentExp / neededExp : 1f;
        if (expBar != null)
            expBar.value = progress;

        // Chữ tiến trình - định dạng "xxx/xxx"
        if (expProgressText != null)
        {
            if (currentLevel >= 50) // cấp tối đa
                expProgressText.text = "MAX";
            else
                expProgressText.text = $"{currentExp}/{neededExp}";
        }
    }
}