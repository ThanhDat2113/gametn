using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VictoryEntryUI : MonoBehaviour
{
    [Header("Visuals")]
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI gainedExpText;
    public Slider expBar;
    public TextMeshProUGUI expProgressText;

    [Header("Animation")]
    [Tooltip("Tổng thời gian chạy hiệu ứng tăng level và thanh EXP (giây)")]
    public float animationDuration = 1.5f;

    private CharacterData data;
    private int gainedExp;
    private int startLevel;
    private int startExp;
    private int currentLevel;
    private int currentExp;
    private int neededExp;

    public void Setup(CharacterData data, int gainedExp, int startLevel, int startExp)
    {
        this.data = data;
        this.gainedExp = gainedExp;
        this.startLevel = startLevel;
        this.startExp = startExp;

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

        // Trạng thái ban đầu
        currentLevel = startLevel;
        currentExp = startExp;
        UpdateLevelText();
        UpdateExpBar();

        // ✅ QUAN TRỌNG: Hiển thị số EXP nhận được ngay lập tức, không animation
        gainedExpText.text = $"(+{gainedExp})";
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = $"Cấp {currentLevel}";
    }

    private void UpdateExpBar()
    {
        neededExp = PlayerProgression.Instance.GetExpToNextLevel(data, currentLevel);
        if (neededExp <= 0) neededExp = int.MaxValue;

        float progress = (float)currentExp / neededExp;
        progress = Mathf.Clamp01(progress);
        if (expBar != null)
            expBar.value = progress;

        if (expProgressText != null)
        {
            if (currentLevel >= 50)
                expProgressText.text = "MAX";
            else
                expProgressText.text = $"{currentExp}/{neededExp}";
        }
    }

    public IEnumerator AnimateExpGain(PlayerProgression progression, System.Action onComplete)
    {
        int remainingExp = gainedExp;
        if (remainingExp <= 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        // Tự động tính bước nhảy dựa trên thời gian animation
        int maxSteps = Mathf.Min(remainingExp, 200);
        float stepDelay = animationDuration / maxSteps;
        stepDelay = Mathf.Clamp(stepDelay, 0.01f, 0.1f);
        int actualSteps = Mathf.FloorToInt(animationDuration / stepDelay);
        if (actualSteps <= 0) actualSteps = 1;
        int expStep = Mathf.CeilToInt((float)remainingExp / actualSteps);
        if (expStep <= 0) expStep = 1;

        int totalGained = 0;
        while (remainingExp > 0)
        {
            int step = Mathf.Min(remainingExp, expStep);
            remainingExp -= step;
            totalGained += step;

            int newExp = currentExp + step;
            int newLevel = currentLevel;
            int expNeeded = progression.GetExpToNextLevel(data, newLevel);
            if (expNeeded <= 0) expNeeded = int.MaxValue;

            while (newExp >= expNeeded && newLevel < 50)
            {
                newExp -= expNeeded;
                newLevel++;
                expNeeded = progression.GetExpToNextLevel(data, newLevel);
                if (expNeeded <= 0) expNeeded = int.MaxValue;
            }

            currentExp = newExp;
            currentLevel = newLevel;
            UpdateExpBar();
            UpdateLevelText();

            // ❌ KHÔNG cập nhật gainedExpText ở đây nữa (đã hiển thị cố định)

            yield return new WaitForSeconds(stepDelay);
        }

        onComplete?.Invoke();
    }
}