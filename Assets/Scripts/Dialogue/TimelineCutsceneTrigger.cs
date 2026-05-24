using UnityEngine;
using UnityEngine.Playables;

public class TimelineCutsceneTrigger : MonoBehaviour
{
    public PlayableDirector timeline;
    public DialogueEvent afterDialogue; // Tuỳ chọn: dialogue sau cutscene
    public Transform speaker; // Ai nói sau cutscene
    public KeyCode interactKey = KeyCode.E;

    private bool playerInRange;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            PlayCutscene();
        }
    }

    void PlayCutscene()
    {
        if (timeline != null)
        {
            timeline.Play();
            // Khi timeline chạy xong, hiện dialogue
            timeline.stopped += OnCutsceneEnd;
        }
        else if (afterDialogue != null)
        {
            MapDialogueController.Instance.StartDialogue(afterDialogue, speaker);
        }
    }

    void OnCutsceneEnd(PlayableDirector _)
    {
        timeline.stopped -= OnCutsceneEnd;
        if (afterDialogue != null)
            MapDialogueController.Instance.StartDialogue(afterDialogue, speaker);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}