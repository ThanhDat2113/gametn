using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị panel Victory/Defeat với animation pop-up ở cuối trận.
/// Gắn vào Canvas trong CombatScene. Tự động tìm nếu chưa có.
/// </summary>
public class CombatResultUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject resultPanel;

    [Header("Text")]
    public TextMeshProUGUI resultText;

    [Header("Image")]
    public Image resultImage;

    [Header("Victory Settings")]
    public string victoryText = "VICTORY!";
    public Color victoryColor = Color.yellow;
    public Sprite victorySprite;

    [Header("Defeat Settings")]
    public string defeatText = "DEFEAT";
    public Color defeatColor = Color.red;
    public Sprite defeatSprite;

    [Header("Animation")]
    public float animationDuration = 0.4f;
    public float overshootScale = 1.2f;
    public float displayDelay = 1.0f; // thời gian hiển thị trước khi chuyển scene

    private CanvasGroup panelCanvasGroup;

    private void Awake()
    {
        if (resultPanel != null)
        {
            panelCanvasGroup = resultPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = resultPanel.AddComponent<CanvasGroup>();

            resultPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Hiển thị panel Victory/Defeat với animation pop-up.
    /// Trả về coroutine để CombatSceneStarter có thể yield.
    /// </summary>
    public IEnumerator ShowResult(bool isVictory)
    {
        if (resultPanel == null) yield break;

        // Cấu hình nội dung
        if (resultText != null)
        {
            resultText.text = isVictory ? victoryText : defeatText;
            resultText.color = isVictory ? victoryColor : defeatColor;
        }
        if (resultImage != null)
        {
            resultImage.sprite = isVictory ? victorySprite : defeatSprite;
            resultImage.gameObject.SetActive(true);
        }

        // Reset trạng thái
        resultPanel.transform.localScale = Vector3.zero;
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        resultPanel.SetActive(true);

        // Animation pop-up: 0 → overshoot → 1 (giống skill button)
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float scale = Mathf.Lerp(0f, overshootScale, t);
            if (t > 0.5f)
            {
                float t2 = (t - 0.5f) / 0.5f; // 0→1
                scale = Mathf.Lerp(overshootScale, 1f, t2);
            }
            resultPanel.transform.localScale = new Vector3(scale, scale, scale);
            yield return null;
        }
        resultPanel.transform.localScale = Vector3.one;

        // Chờ 1s rồi tự động tắt (scene sẽ được unload bởi CombatSceneStarter)
        yield return new WaitForSeconds(displayDelay);
    }
}