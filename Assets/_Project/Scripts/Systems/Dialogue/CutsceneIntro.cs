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

    // Vị trí spawn dialogue
    public Transform dialoguePoint;

    [Header("Player")]
    public MonoBehaviour playerController;

    private bool dialogueFinished;

    private void Start()
    {
        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        // Khóa player
        if (playerController != null)
            playerController.enabled = false;

        //--------------------------------
        // Fade đen
        //--------------------------------

        yield return FadeController.Instance.FadeToBlack();

        //--------------------------------
        // Chuyển camera
        //--------------------------------

        mainCamera.SetActive(false);
        cutsceneCamera.SetActive(true);

        cutsceneCamera.transform.position = pointA.position;
        cutsceneCamera.transform.rotation = pointA.rotation;

        yield return FadeController.Instance.FadeFromBlack();

        //--------------------------------
        // Camera Move
        //--------------------------------

        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / moveDuration);

            cutsceneCamera.transform.position =
                Vector3.Lerp(
                    pointA.position,
                    pointB.position,
                    t);

            cutsceneCamera.transform.rotation =
                Quaternion.Slerp(
                    pointA.rotation,
                    pointB.rotation,
                    t);

            yield return null;
        }

        //--------------------------------
        // Dialogue
        //--------------------------------

        dialogueFinished = false;

        DialogueBubbleUI.Instance.ShowSequential(
            dialogueLines,
            dialoguePoint,
            OnDialogueFinished
        );

        yield return new WaitUntil(() => dialogueFinished);

        //--------------------------------
        // Fade kết thúc
        //--------------------------------

        yield return FadeController.Instance.FadeToBlack();

        cutsceneCamera.SetActive(false);
        mainCamera.SetActive(true);

        yield return FadeController.Instance.FadeFromBlack();

        //--------------------------------
        // Trả điều khiển player
        //--------------------------------

        if (playerController != null)
            playerController.enabled = true;
    }

    private void OnDialogueFinished()
    {
        dialogueFinished = true;
    }
}