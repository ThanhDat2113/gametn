using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn trên prefab button nhân vật trong Character Panel.
/// Chứa các tham chiếu trực tiếp đến các thành phần UI.
/// </summary>
public class CharacterButtonUI : MonoBehaviour
{
    [Header("UI References")]
    public Image avatarImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;      // Chỉ hiển thị số, không có chữ "Cấp" hay "Lv."
    public TextMeshProUGUI orderText;

    private Button button;
    private CharacterData characterData;

    public event System.Action<CharacterData> OnClicked;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(() => OnClicked?.Invoke(characterData));
    }

    /// <summary>
    /// Cập nhật thông tin cho button.
    /// </summary>
    public void Setup(CharacterData character, int order)
    {
        characterData = character;

        if (avatarImage != null && character.portrait != null)
            avatarImage.sprite = character.portrait;

        if (nameText != null)
            nameText.text = character.characterName;

        // Level – chỉ hiển thị số
        int level = 1;
        if (PlayerProgression.Instance != null)
            level = PlayerProgression.Instance.GetLevel(character);

        if (levelText != null)
            levelText.text = level.ToString();  // ✅ Chỉ hiển thị số

        if (orderText != null)
            orderText.text = order.ToString();
    }
}