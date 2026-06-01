using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

public class TimelineStarter : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector timelineDirector;

    [Header("Scene")]
    public string nextSceneName = "MAP";

    [Header("Loading Scene")]
    public string loadingSceneName = "LoadingScene";

    [Header("Skip Button")]
    public GameObject skipButton;
    public float skipButtonVisibleTime = 3f;

    private bool isSkipping = false;
    private Coroutine hideCoroutine;

    private void Start()
    {
        if (timelineDirector == null)
        {
            Debug.LogError("Chưa gán Timeline Director!", this);
            return;
        }

        // Ẩn nút Skip lúc đầu
        if (skipButton != null)
            skipButton.SetActive(false);

        // Đăng ký sự kiện kết thúc Timeline
        timelineDirector.stopped += OnTimelineFinished;

        // Chạy Timeline ngay khi vào scene
        timelineDirector.Play();
    }

    private void Update()
    {
        if (isSkipping) return;

        // Chỉ hiện Skip khi Timeline đang chạy
        if (timelineDirector != null &&
            timelineDirector.state == PlayState.Playing)
        {
            if (Input.GetMouseButtonDown(0))
            {
                ShowSkipButton();
            }
        }
    }

    private void ShowSkipButton()
    {
        if (skipButton == null) return;

        skipButton.SetActive(true);

        // Nếu đang có coroutine ẩn nút thì hủy
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideSkipButtonAfterDelay());
    }

    private IEnumerator HideSkipButtonAfterDelay()
    {
        yield return new WaitForSeconds(skipButtonVisibleTime);

        if (skipButton != null)
            skipButton.SetActive(false);
    }

    public void SkipCutscene()
    {
        if (isSkipping) return;

        isSkipping = true;

        if (timelineDirector != null)
            timelineDirector.Stop();

        LoadNextScene();
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        if (isSkipping) return;

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneLoader.sceneToLoad = nextSceneName;
        SceneManager.LoadScene(loadingSceneName);
    }

    private void OnDestroy()
    {
        if (timelineDirector != null)
            timelineDirector.stopped -= OnTimelineFinished;
    }
}