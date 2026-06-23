using UnityEngine;
using System.Collections;

public class CutsceneIntro : MonoBehaviour
{
    private static bool _hasPlayed = false;

    // ✅ Reset flag mỗi khi chạy game (bao gồm cả trong Editor)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetPlayedFlag()
    {
        _hasPlayed = false;
    }

    [Header("Camera")]
    public GameObject mainCamera;
    public GameObject cutsceneCamera;

    [Header("Camera Movement")]
    public Transform pointA;
    public Transform pointB;
    public float moveDuration = 5f;

    [Header("Dialogue")]
    public DialogueLineData[] dialogueLines;
    public Transform dialoguePoint;

    [Header("Player")]
    public MonoBehaviour playerController;

    private bool dialogueFinished;

    private void Awake()
    {
        // Nếu đã chạy rồi → hủy luôn
        if (_hasPlayed)
        {
            Destroy(gameObject);
            return;
        }

        // Tự tìm các đối tượng nếu chưa gán
        if (mainCamera == null)
            mainCamera = Camera.main?.gameObject;

        if (dialoguePoint == null)
        {
            GameObject dp = GameObject.FindWithTag("DialoguePoint");
            if (dp == null) dp = GameObject.Find("DialoguePoint");
            if (dp != null) dialoguePoint = dp.transform;
        }

        if (playerController == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
                foreach (var s in scripts)
                {
                    string typeName = s.GetType().Name;
                    if (typeName.Contains("Controller") || typeName.Contains("Movement") || typeName.Contains("Input"))
                    {
                        playerController = s;
                        break;
                    }
                }
                if (playerController == null && scripts.Length > 0)
                    playerController = scripts[0];
            }
        }
    }

    private void Start()
    {
        if (_hasPlayed) return;
        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        // Khóa player
        if (playerController != null)
            playerController.enabled = false;

        // Màn hình đã đen từ Boot Scene → chỉ cần FadeFromBlack
        yield return FadeController.Instance.FadeFromBlack();

        // Chuyển camera
        if (mainCamera != null) mainCamera.SetActive(false);
        if (cutsceneCamera != null)
        {
            cutsceneCamera.SetActive(true);
            cutsceneCamera.transform.position = pointA.position;
            cutsceneCamera.transform.rotation = pointA.rotation;
        }
        else
        {
            Debug.LogError("[CutsceneIntro] cutsceneCamera chưa được gán!");
            yield break;
        }

        // Di chuyển camera từ A → B
        float timer = 0f;
        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / moveDuration);
            cutsceneCamera.transform.position = Vector3.Lerp(pointA.position, pointB.position, t);
            cutsceneCamera.transform.rotation = Quaternion.Slerp(pointA.rotation, pointB.rotation, t);
            yield return null;
        }

        // Dialogue
        dialogueFinished = false;
        if (DialogueBubbleUI.Instance != null && dialogueLines != null && dialogueLines.Length > 0)
        {
            DialogueBubbleUI.Instance.ShowSequential(
                dialogueLines,
                dialoguePoint != null ? dialoguePoint : transform,
                OnDialogueFinished
            );
        }
        else
        {
            OnDialogueFinished();
        }
        yield return new WaitUntil(() => dialogueFinished);

        // Fade đen
        yield return FadeController.Instance.FadeToBlack();

        // Chuyển về main camera
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        // Fade từ đen lên
        yield return FadeController.Instance.FadeFromBlack();

        // Mở khóa player
        if (playerController != null)
            playerController.enabled = true;

        _hasPlayed = true;
    }

    private void OnDialogueFinished()
    {
        dialogueFinished = true;
    }
}