using UnityEngine;
using System.Collections;

public class CutsceneIntro : MonoBehaviour
{
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

    [Header("Bubble Settings")]
    [Tooltip("Nếu true, tất cả các dòng trong cutscene sẽ dùng NPC Bubble (bất kể isPlayerLine)")]
    public bool forceNPCBubble = true;

    private bool dialogueFinished;

    private void Awake()
    {
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
        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        // Chờ một frame để các hệ thống khác ổn định (SceneTransitionManager, FadeController, v.v.)
        yield return null;
        yield return new WaitForSeconds(0.2f);

        if (playerController != null)
            playerController.enabled = false;

        if (FadeController.Instance == null)
        {
            Debug.LogError("[CutsceneIntro] FadeController.Instance == null!");
        }
        else
        {
            // Đảm bảo màn hình đang đen trước khi fade từ đen ra
            // (FadeController mới khởi tạo có alpha = 0, cần set về 1 để thấy hiệu ứng fade)
            FadeController.Instance.SetAlpha(1f);
            yield return new WaitForSeconds(0.1f);
            yield return FadeController.Instance.FadeFromBlack();
        }

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

        float timer = 0f;
        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / moveDuration);
            cutsceneCamera.transform.position = Vector3.Lerp(pointA.position, pointB.position, t);
            cutsceneCamera.transform.rotation = Quaternion.Slerp(pointA.rotation, pointB.rotation, t);
            yield return null;
        }

        dialogueFinished = false;
        if (DialogueBubbleUI.Instance != null && dialogueLines != null && dialogueLines.Length > 0)
        {
            DialogueLineData[] linesToUse = dialogueLines;
            if (forceNPCBubble)
            {
                linesToUse = CloneLinesWithNPCBubble(dialogueLines);
            }

            DialogueBubbleUI.Instance.ShowSequential(
                linesToUse,
                dialoguePoint != null ? dialoguePoint : transform,
                null,
                OnDialogueFinished,
                0,
                null
            );
        }
        else
        {
            OnDialogueFinished();
        }
        yield return new WaitUntil(() => dialogueFinished);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeFromBlack();

        if (playerController != null)
            playerController.enabled = true;

    }

    private void OnDialogueFinished()
    {
        dialogueFinished = true;
    }

    private DialogueLineData[] CloneLinesWithNPCBubble(DialogueLineData[] original)
    {
        DialogueLineData[] clones = new DialogueLineData[original.Length];
        for (int i = 0; i < original.Length; i++)
        {
            DialogueLineData clone = ScriptableObject.CreateInstance<DialogueLineData>();
            clone.speakerName = original[i].speakerName;
            clone.text = original[i].text;
            clone.offset = original[i].offset;
            clone.offsetRight = original[i].offsetRight;
            clone.isPlayerLine = false;
            clones[i] = clone;
        }
        return clones;
    }
}