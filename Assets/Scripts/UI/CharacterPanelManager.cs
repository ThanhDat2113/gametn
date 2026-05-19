using UnityEngine;
using UnityEngine.UI;

public class CharacterPanelManager : MonoBehaviour
{
    [Header("Panel chính chứa 12 nút")]
    public GameObject mainCharacterPanel;

    [Header("Danh sách 12 nút nhân vật")]
    public Button[] characterButtons;

    [Header("Danh sách 12 panel thông tin")]
    public GameObject[] infoPanels;

    private enum State { Main, Info }
    private State currentState = State.Main;
    private int currentInfoIndex = -1;

    void Start()
    {
        if (characterButtons.Length != 12 || infoPanels.Length != 12)
        {
            Debug.LogError("Cần có đúng 12 nút và 12 panel!");
            return;
        }

        mainCharacterPanel.SetActive(true);
        foreach (var panel in infoPanels)
            if (panel != null) panel.SetActive(false);

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() => OpenInfoPanel(index));
        }
    }

    public void OpenInfoPanel(int index)
    {
        if (currentState == State.Info && currentInfoIndex >= 0 && infoPanels[currentInfoIndex] != null)
            infoPanels[currentInfoIndex].SetActive(false);

        mainCharacterPanel.SetActive(false);
        infoPanels[index].SetActive(true);
        currentState = State.Info;
        currentInfoIndex = index;
    }

    private void CloseInfoPanel()
    {
        if (currentInfoIndex >= 0 && infoPanels[currentInfoIndex] != null)
            infoPanels[currentInfoIndex].SetActive(false);

        mainCharacterPanel.SetActive(true);
        currentState = State.Main;
        currentInfoIndex = -1;
    }

    // Hàm này được gọi từ MapMenuManager khi nhấn ESC/chuột phải
    public bool TryGoBack()
    {
        if (currentState == State.Info)
        {
            CloseInfoPanel();
            return true; // Đã xử lý, không để MapMenuManager xử lý tiếp
        }
        return false; // Chưa xử lý, MapMenuManager sẽ tự xử lý (quay về main)
    }
}