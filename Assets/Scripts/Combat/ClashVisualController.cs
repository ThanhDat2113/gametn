using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClashVisualController : MonoBehaviour
{
    [Header("Dice UI")]
    public GameObject clashPanel;        // Panel chứa toàn bộ clash UI
    public TextMeshProUGUI playerDiceText; // Hiển thị số xúc xắc player
    public TextMeshProUGUI enemyDiceText;  // Hiển thị số xúc xắc enemy
    public TextMeshProUGUI playerScoreText;// Tổng điểm player
    public TextMeshProUGUI enemyScoreText; // Tổng điểm enemy
    public TextMeshProUGUI resultText;     // "PLAYER WINS!" / "ENEMY WINS!"

    [Header("Timing")]
    public float diceRollDuration = 1.0f;  // Thời gian xúc xắc quay
    public float diceInterval = 0.05f; // Tốc độ số nhảy
    public float resultShowDelay = 0.3f;  // Delay trước khi hiện kết quả

    // Gọi từ CombatManager khi bắt đầu clash
    public IEnumerator PlayClashSequence(ClashVisualData data,
                                          System.Action onComplete)
    {
        clashPanel.SetActive(true);
        resultText.gameObject.SetActive(false);

        // Reset text
        playerDiceText.text = "?";
        enemyDiceText.text = "?";
        playerScoreText.text = $"Base: {data.PlayerBasePoint}";
        enemyScoreText.text = $"Base: {data.EnemyBasePoint}";

        // Animation xúc xắc quay số ngẫu nhiên
        yield return StartCoroutine(RollDiceAnimation(
            playerDiceText, data.PlayerDiceResult, diceRollDuration));

        yield return StartCoroutine(RollDiceAnimation(
            enemyDiceText, data.EnemyDiceResult, diceRollDuration));

        // Hiện tổng điểm
        playerScoreText.text = $"{data.PlayerBasePoint} + {data.PlayerDiceResult}" +
                               $" = {data.PlayerTotalScore}";
        enemyScoreText.text = $"{data.EnemyBasePoint} + {data.EnemyDiceResult}" +
                               $" = {data.EnemyTotalScore}";

        yield return new WaitForSeconds(resultShowDelay);

        // Hiện kết quả
        resultText.gameObject.SetActive(true);
        if (data.PlayerWins)
        {
            resultText.text = "PLAYER WINS!";
            resultText.color = Color.yellow;
        }
        else
        {
            resultText.text = "ENEMY WINS!";
            resultText.color = Color.red;
        }

        yield return new WaitForSeconds(0.8f);

        clashPanel.SetActive(false);
        onComplete?.Invoke();
    }

    // Animation số nhảy lung tung rồi dừng lại đúng kết quả
    private IEnumerator RollDiceAnimation(TextMeshProUGUI text,
                                           int finalValue,
                                           float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            text.text = Random.Range(1, 7).ToString();
            elapsed += diceInterval;
            yield return new WaitForSeconds(diceInterval);
        }

        // Hiện số thật
        text.text = finalValue.ToString();
    }
}

// Data truyền vào ClashVisualController
public class ClashVisualData
{
    public int PlayerBasePoint;
    public int PlayerDiceResult;
    public int PlayerTotalScore;
    public int EnemyBasePoint;
    public int EnemyDiceResult;
    public int EnemyTotalScore;
    public bool PlayerWins;
}