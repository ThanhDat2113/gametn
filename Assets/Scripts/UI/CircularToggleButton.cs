using UnityEngine;
using UnityEngine.EventSystems;

public class CircularToggleButton : MonoBehaviour, IPointerClickHandler
{
    public GameObject targetPanel;
    public bool startOpen = false;

    private bool isOpen;

    void Start()
    {
        if (targetPanel == null)
        {
            Debug.LogError("CircularToggleButton: targetPanel chưa được gán!");
            return;
        }
        isOpen = startOpen;
        targetPanel.SetActive(isOpen);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isOpen = !isOpen;
        targetPanel.SetActive(isOpen);
    }
}