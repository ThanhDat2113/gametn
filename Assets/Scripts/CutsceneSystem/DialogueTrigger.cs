using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueLineData[] lines;
    public KeyCode interactKey = KeyCode.E;
    public GameObject interactionPrompt;
    public bool playOnce = true;
    public bool sequential = true;

    private bool _playerInRange;
    private bool _hasPlayed;

    void Update()
    {
        if (_playerInRange && Input.GetKeyDown(interactKey))
        {
            if (playOnce && _hasPlayed) return;
            _hasPlayed = true;
            if (sequential)
                DialogueBubbleUI.Instance.ShowSequential(lines, transform);
            else
                DialogueBubbleUI.Instance.Show(lines[0], transform);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            if (interactionPrompt) interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            if (interactionPrompt) interactionPrompt.SetActive(false);
        }
    }
}