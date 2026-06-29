using UnityEngine;
using System.Collections;

public class CutsceneIntro : MonoBehaviour
{
    private static bool _hasPlayed = false;

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

    [Header("Bubble Settings")]
    [Tooltip("Nếu true, tất cả các dòng trong cutscene sẽ dùng NPC Bubble (bất kể isPlayerLine)")]
    public bool forceNPCBubble = true;

    private bool dialogueFinished;

    private void Awake()
    {
        if (_hasPlayed)
        {
            Destroy(gameObject);
            return;
        }

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
        if (playerController != null)
            playerController.enabled = false;

        yield return FadeController.Instance.FadeFromBlack();

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
            // Nếu forceNPCBubble, clone các line và set isPlayerLine = false
            DialogueLineData[] linesToUse = dialogueLines;
            if (forceNPCBubble)
            {
                linesToUse = CloneLinesWithNPCBubble(dialogueLines);
            }

            DialogueBubbleUI.Instance.ShowSequential(
                linesToUse,
                dialoguePoint != null ? dialoguePoint : transform,
                null, // playerTarget (cutscene không cần)
                OnDialogueFinished,
                0,    // startIndex
                null  // side (không cần)
            );
        }
        else
        {
            OnDialogueFinished();
        }
        yield return new WaitUntil(() => dialogueFinished);

        yield return FadeController.Instance.FadeToBlack();

        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        yield return FadeController.Instance.FadeFromBlack();

        if (playerController != null)
            playerController.enabled = true;

        _hasPlayed = true;
    }

    private void OnDialogueFinished()
    {
        dialogueFinished = true;
    }

    // Helper để clone các line và set isPlayerLine = false
    private DialogueLineData[] CloneLinesWithNPCBubble(DialogueLineData[] original)
    {
        DialogueLineData[] clones = new DialogueLineData[original.Length];
        for (int i = 0; i < original.Length; i++)
        {
            // Tạo một instance mới từ ScriptableObject (không nên dùng Instantiate vì sẽ tạo asset mới)
            // Thay vào đó, tạo một đối tượng tạm thời và copy dữ liệu
            // Cách đơn giản: tạo một ScriptableObject mới và gán giá trị
            DialogueLineData clone = ScriptableObject.CreateInstance<DialogueLineData>();
            clone.speakerName = original[i].speakerName;
            clone.text = original[i].text;
            clone.offset = original[i].offset;
            clone.offsetRight = original[i].offsetRight;
            clone.isPlayerLine = false; // Luôn false để dùng NPC Bubble
            clones[i] = clone;
        }
        return clones;
    }
}