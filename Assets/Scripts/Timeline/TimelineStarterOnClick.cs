using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TimelineStarterOnClick : MonoBehaviour
{
    [Tooltip("PlayableDirector chứa Timeline cần điều khiển")]
    public PlayableDirector timelineDirector;
    [Tooltip("Tên scene cần chuyển sau khi timeline kết thúc")]
    public string nextSceneName = "MAP";

    private bool hasStarted = false;

    void Start()
    {
        if (timelineDirector == null)
        {
            Debug.LogError("Chưa gán Timeline Director!", this);
            return;
        }

        // Dừng Timeline nếu đang chạy, đưa về đầu
        if (timelineDirector.state == PlayState.Playing)
            timelineDirector.Stop();
        timelineDirector.time = 0;
        timelineDirector.Evaluate();

        // Đăng ký sự kiện kết thúc
        timelineDirector.stopped += OnTimelineFinished;
    }

    void Update()
    {
        if (!hasStarted && Input.GetMouseButtonDown(0))
        {
            StartTimeline();
        }
    }

    void StartTimeline()
    {
        if (timelineDirector == null) return;
        hasStarted = true;
        timelineDirector.Play();
        Debug.Log("Timeline đã bắt đầu chạy do click chuột.");
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        if (timelineDirector != null)
            timelineDirector.stopped -= OnTimelineFinished;
    }
}