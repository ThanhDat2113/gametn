using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance { get; private set; }

    public TextMeshProUGUI objectiveText;
    public GameObject panel;

    private bool _isVisible = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateVisibility();
    }

    private void Update()
    {
        UpdateVisibility();
    }

    public void UpdateVisibility()
    {
        bool isDialogueActive = DialogueBubbleUI.IsDialogueActive;
        bool isTimelinePlaying = TimelinePlaybackManager.IsTimelinePlaying;
        bool hasActiveQuest = QuestManager.Instance != null
                              && QuestManager.Instance.CurrentStep != null
                              && !QuestManager.Instance.IsQuestCompleted();

        bool shouldShow = !isDialogueActive && !isTimelinePlaying && hasActiveQuest;

        // 🔥 Log để debug
        // Debug.Log($"[QuestUI] shouldShow={shouldShow}, isDialogueActive={isDialogueActive}, hasActiveQuest={hasActiveQuest}");

        if (shouldShow && !_isVisible)
        {
            if (panel != null) panel.SetActive(true);
            _isVisible = true;
            Debug.Log("[QuestUI] ✅ Hiện panel");
        }
        else if (!shouldShow && _isVisible)
        {
            if (panel != null) panel.SetActive(false);
            _isVisible = false;
            Debug.Log("[QuestUI] ❌ Ẩn panel");
        }
    }

    public void SetObjective(string description)
    {
        if (objectiveText != null)
            objectiveText.text = description;
        UpdateVisibility();
    }

    public void Show()
    {
        // 🔥 Gọi UpdateVisibility để hiện nếu có thể
        UpdateVisibility();
    }

    public void Hide()
    {
        if (objectiveText != null)
            objectiveText.text = "";
        if (panel != null && panel.activeSelf)
        {
            panel.SetActive(false);
            _isVisible = false;
            Debug.Log("[QuestUI] Hide() được gọi → ẩn panel");
        }
    }
}