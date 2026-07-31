using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Hiển thị thông tin chi tiết của một nhân vật.
/// Gắn vào prefab panel thông tin trong Character Panel.
/// </summary>
public class CharacterInfoUI : MonoBehaviour
{
    [Header("=== IDENTITY ===")]
    public Image characterImageDisplay;      // Dùng characterImage (full-body/illustration)
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI titleText;        // Danh hiệu
    public TextMeshProUGUI roleText;         // Vai trò

    [Header("=== LEVEL & EXP ===")]
    public TextMeshProUGUI levelText;        // Chỉ hiển thị số level
    public Slider expSlider;
    public TextMeshProUGUI expText;          // Hiển thị "current / needed"

    [Header("=== SPRITE / ANIMATION ===")]
    public Image spriteDisplay;              // Hiển thị battle sprite (phụ)
    public Animator previewAnimator;         // Animator dành cho preview animation

    [Header("=== STATS ===")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI pdefText;
    public TextMeshProUGUI mdefText;

    [Header("=== SKILLS ===")]
    public TextMeshProUGUI skill1Text;
    public TextMeshProUGUI skill2Text;
    public TextMeshProUGUI skill3Text;

    // ──────────────────────────────────────────────────────────────

    private CharacterData currentCharacter;

    /// <summary>
    /// Cập nhật toàn bộ thông tin cho nhân vật.
    /// </summary>
    public void Setup(CharacterData character)
    {
        if (character == null)
        {
            Debug.LogWarning("CharacterInfoUI: character is null!");
            return;
        }

        currentCharacter = character;

        // ── 1. IDENTITY ──
        if (characterImageDisplay != null)
        {
            // ✅ Giữ tỉ lệ hình ảnh gốc
            characterImageDisplay.preserveAspect = true;
            characterImageDisplay.type = Image.Type.Simple; // Đảm bảo không bị cắt

            if (character.characterImage != null)
                characterImageDisplay.sprite = character.characterImage;
            else if (character.portrait != null)
                characterImageDisplay.sprite = character.portrait; // fallback
        }

        if (nameText != null)
            nameText.text = character.characterName;

        if (titleText != null)
            titleText.text = character.GetTitleOrDefault();

        if (roleText != null)
            roleText.text = character.GetRoleOrDefault();

        // ── 2. LEVEL & EXP ──
        int level = 1;
        int currentExp = 0;
        int neededExp = 100;
        float progress = 0f;

        if (PlayerProgression.Instance != null)
        {
            level = PlayerProgression.Instance.GetLevel(character);
            currentExp = PlayerProgression.Instance.GetCurrentExp(character);
            neededExp = PlayerProgression.Instance.GetExpToNextLevel(character);
            progress = (float)currentExp / neededExp;
        }

        if (levelText != null)
            levelText.text = level.ToString();

        if (expSlider != null)
            expSlider.value = Mathf.Clamp01(progress);

        if (expText != null)
            expText.text = $"{currentExp} / {neededExp}";

        // ── 3. SPRITE / ANIMATION ──
        if (spriteDisplay != null && character.battleSprite != null)
            spriteDisplay.sprite = character.battleSprite;

        if (previewAnimator != null && character.previewAnimation != null)
        {
            previewAnimator.runtimeAnimatorController = null;
            previewAnimator.Play(character.previewAnimation.name, 0, 0f);
            previewAnimator.speed = 1f;
        }

        // ── 4. STATS ──
        int hp = character.GetHP(level);
        int atk = character.GetATK(level);
        int pdef = character.GetPDEF(level);
        int mdef = character.GetMDEF(level);

        if (EquipmentManager.Instance != null)
        {
            var equipment = EquipmentManager.Instance.GetEquipment(character);
            if (equipment != null)
            {
                hp += equipment.GetHPBonus();
                atk += equipment.GetATKBonus();
                pdef += equipment.GetPDEFBonus();
                mdef += equipment.GetMDEFBonus();
            }
        }

        if (hpText != null) hpText.text = $"{hp}";
        if (atkText != null) atkText.text = $"{atk}";
        if (pdefText != null) pdefText.text = $"{pdef}";
        if (mdefText != null) mdefText.text = $"{mdef}";

        // ── 5. SKILLS ──
        if (character.skills != null && character.skills.Length > 0)
        {
            if (skill1Text != null)
                skill1Text.text = character.skills.Length > 0 ? character.skills[0].skillName : "—";

            if (skill2Text != null)
                skill2Text.text = character.skills.Length > 1 ? character.skills[1].skillName : "—";

            if (skill3Text != null)
                skill3Text.text = character.skills.Length > 2 ? character.skills[2].skillName : "—";
        }
        else
        {
            if (skill1Text != null) skill1Text.text = "—";
            if (skill2Text != null) skill2Text.text = "—";
            if (skill3Text != null) skill3Text.text = "—";
        }
    }

    public void Refresh()
    {
        if (currentCharacter != null)
            Setup(currentCharacter);
    }

    private void OnEnable()
    {
        if (currentCharacter != null)
            Refresh();
    }
}