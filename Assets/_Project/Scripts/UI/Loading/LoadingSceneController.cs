using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI")]
    public Slider progressBar;
    public Text progressText;

    [Header("Settings")]
    public float minLoadTime = 1f;         // Thời gian tối thiểu hiển thị loading (giây)
    public float fakeProgressSpeed = 0.2f; // Tốc độ tăng fake progress sau khi đạt 90%

    private IEnumerator Start()
    {
        if (string.IsNullOrEmpty(SceneLoader.sceneToLoad))
        {
            Debug.LogError("[LoadingScene] Không có scene đích! Gán SceneLoader.sceneToLoad trước khi load.");
            yield break;
        }

        // Bắt đầu load scene đích ở chế độ nền
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneLoader.sceneToLoad);
        operation.allowSceneActivation = false; // ❗ Quan trọng: không chuyển scene ngay

        float displayProgress = 0f;
        float realProgress = 0f;
        float elapsed = 0f;

        while (!operation.isDone)
        {
            elapsed += Time.deltaTime;

            // Progress thực tế từ Unity (0 - 0.9)
            // 0.9 là khi load gần xong, còn chờ allowSceneActivation
            realProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Hiển thị progress: lấy max(real, display)
            // Display chạy mượt hơn, không bị giật
            if (realProgress < 1f)
            {
                displayProgress = Mathf.Max(displayProgress, realProgress);
            }
            else
            {
                // Khi real đạt 100%, fake progress chạy nốt đến 100% + chờ minLoadTime
                if (elapsed >= minLoadTime)
                {
                    displayProgress = Mathf.MoveTowards(displayProgress, 1f, fakeProgressSpeed * Time.deltaTime * 10f);
                }
                else
                {
                    // Vẫn còn trong minLoadTime, giữ ở mức cao nhưng chưa tới 1
                    displayProgress = Mathf.MoveTowards(displayProgress, 0.95f, fakeProgressSpeed * Time.deltaTime * 5f);
                }
            }

            // Cập nhật UI
            UpdateProgressUI(displayProgress);

            // Khi thực sự sẵn sàng và đã hiển thị loading đủ lâu
            if (operation.progress >= 0.9f && displayProgress >= 1f)
            {
                // Chờ thêm 1 frame để UI kịp render
                yield return null;
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // Đảm bảo progress bar đầy trước khi scene mới active
        UpdateProgressUI(1f);
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBar != null)
            progressBar.value = progress;

        if (progressText != null)
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }
}