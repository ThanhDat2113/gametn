using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    public TextMeshProUGUI objectiveText;
    public GameObject panel;

    private void Start()
    {
        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
        // ❌ Xóa dòng này: objectiveText.text = "";
    }

    public void SetObjective(string description)
    {
        objectiveText.text = description;
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        objectiveText.text = "";
        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);
    }
}