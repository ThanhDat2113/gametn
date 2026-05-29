using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class IntroToMap : MonoBehaviour
{
    [SerializeField] private PlayableDirector timelineDirector; // Kéo Timeline vào đây
    [SerializeField] private string nextSceneName = "MAP";

    private void Start()
    {
        if (timelineDirector != null)
        {
            // Đăng ký sự kiện khi Timeline kết thúc
            timelineDirector.stopped += OnTimelineFinished;
            // Nếu Timeline chưa tự chạy, hãy bắt đầu nó
            timelineDirector.Play();
        }
        else
        {
            Debug.LogWarning("Chưa gán Timeline Director! Chuyển scene ngay.");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        // Chuyển sang scene MAP
        SceneManager.LoadScene(nextSceneName);
    }
}