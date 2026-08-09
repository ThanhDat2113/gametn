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

    [Header("Loading UI")]
    [Tooltip("Loading panel sẽ tự động ẩn khi camera bắt đầu di chuyển")]
    public bool autoHideLoadingOnStart = true;

    private bool dialogueFinished;
    private bool _hasStartedMoving = false;

    // Property để các hệ thống khác (LoadingUIManager) kiểm tra
    public bool HasStartedMoving => _hasStartedMoving;

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
        // ✅ Bật flag để ẩn toàn bộ UI (Quest, Minimap, Menu)
        DialogueBubbleUI.SetDialogueActive(true);

        // Chờ một frame để các hệ thống khác ổn định
        yield return null;
        yield return new WaitForSeconds(0.2f);

        // Tắt điều khiển player
        if (playerController != null)
            playerController.enabled = false;

        // Fade từ đen
        if (FadeController.Instance == null)
        {
            Debug.LogError("[CutsceneIntro] FadeController.Instance == null!");
        }
        else
        {
            FadeController.Instance.SetAlpha(1f);
            yield return new WaitForSeconds(0.1f);
            yield return FadeController.Instance.FadeFromBlack();
        }

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

        // ✅ Đánh dấu cutscene đã bắt đầu di chuyển (để LoadingUIManager biết)
        _hasStartedMoving = true;

        // ✅ Ẩn loading UI ngay khi camera bắt đầu di chuyển
        if (autoHideLoadingOnStart && LoadingUIManager.Instance != null)
        {
            LoadingUIManager.Instance.HideLoading();
        }

        // Di chuyển camera từ A đến B
        float timer = 0f;
        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / moveDuration);
            cutsceneCamera.transform.position = Vector3.Lerp(pointA.position, pointB.position, t);
            cutsceneCamera.transform.rotation = Quaternion.Slerp(pointA.rotation, pointB.rotation, t);
            yield return null;
        }

        // Phát dialogue (nếu có)
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

        // Fade to black trước khi chuyển camera về
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeToBlack();

        // Chuyển về camera chính
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
        if (mainCamera != null) mainCamera.SetActive(true);

        // Fade from black
        if (FadeController.Instance != null)
            yield return FadeController.Instance.FadeFromBlack();

        // Bật lại điều khiển player
        if (playerController != null)
            playerController.enabled = true;

        // ✅ Kết thúc cutscene → tắt flag dialogue để hiện lại UI
        DialogueBubbleUI.SetDialogueActive(false);

        // ✅ BUỘC HIỆN LẠI QUEST UI NGAY LẬP TỨC
        if (QuestUI.Instance != null)
        {
            QuestUI.Instance.Show();
            Debug.Log("[CutsceneIntro] ✅ Đã gọi QuestUI.Instance.Show() để hiện lại UI");
        }
        else
        {
            Debug.LogWarning("[CutsceneIntro] QuestUI.Instance null, không thể hiện lại UI");
        }

        // Reset flag cutscene
        _hasStartedMoving = false;
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