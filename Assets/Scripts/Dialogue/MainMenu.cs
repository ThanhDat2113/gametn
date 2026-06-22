using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;    // Ảnh đen full màn hình
    [SerializeField] private float fadeDuration = 1f; // Thời gian tối dần

    public void OnNewGameButton()
    {
        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;

        // Tăng dần alpha từ 0 lên 1
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            color.a = alpha;
            fadeImage.color = color;
            yield return null;
        }

        // Đảm bảo alpha = 1 sau khi kết thúc
        color.a = 1f;
        fadeImage.color = color;

        // Chuyển scene
        SceneManager.LoadScene("Intro");
    }
}