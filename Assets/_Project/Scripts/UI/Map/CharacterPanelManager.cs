using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterPanelManager : MonoBehaviour
{
    [Header("Button Container")]
    public Transform buttonContainer;         // nơi chứa các nút nhân vật
    public GameObject buttonPrefab;           // prefab của nút (đã gắn CharacterButtonUI)

    [Header("Info Panel")]
    public Transform infoPanelContainer;      // nơi chứa panel thông tin
    public GameObject infoPanelPrefab;        // prefab panel thông tin (đã gắn CharacterInfoUI)

    private List<GameObject> currentButtons = new List<GameObject>();
    private Dictionary<CharacterData, GameObject> currentInfoPanels = new Dictionary<CharacterData, GameObject>();
    private CharacterData selectedCharacter = null;

    private enum State { Main, Info }
    private State currentState = State.Main;

    private void OnEnable()
    {
        RefreshList();
    }

    /// <summary>
    /// Xóa danh sách cũ và tạo lại các nút cho nhân vật đã mở khóa.
    /// </summary>
    public void RefreshList()
    {
        // Xóa các nút cũ
        foreach (var btn in currentButtons)
            Destroy(btn);
        currentButtons.Clear();

        // Xóa tất cả info panel cũ
        foreach (var panel in currentInfoPanels.Values)
            Destroy(panel);
        currentInfoPanels.Clear();

        // Lấy danh sách nhân vật đã mở khóa từ FormationManager
        var formationMgr = FindObjectOfType<FormationManager>();
        if (formationMgr == null)
        {
            Debug.LogWarning("CharacterPanelManager: Không tìm thấy FormationManager!");
            return;
        }

        var unlocked = formationMgr.UnlockedCharacters;
        if (unlocked == null || unlocked.Count == 0)
        {
            Debug.Log("CharacterPanelManager: Chưa có nhân vật nào được mở khóa.");
            return;
        }

        int order = 1;
        foreach (var character in unlocked)
        {
            GameObject btnGO = Instantiate(buttonPrefab, buttonContainer);
            currentButtons.Add(btnGO);

            // Lấy component CharacterButtonUI
            var btnUI = btnGO.GetComponent<CharacterButtonUI>();
            if (btnUI != null)
            {
                btnUI.Setup(character, order);
                btnUI.OnClicked += OnCharacterSelected;
            }
            else
            {
                Debug.LogWarning("CharacterPanelManager: buttonPrefab thiếu component CharacterButtonUI!");
            }

            order++;
        }

        // Hiển thị danh sách chính
        ShowMainPanel();
    }

    private void OnCharacterSelected(CharacterData character)
    {
        selectedCharacter = character;
        ShowInfoPanel(character);
    }

    private void ShowInfoPanel(CharacterData character)
    {
        if (infoPanelPrefab == null || infoPanelContainer == null)
        {
            Debug.LogError("CharacterPanelManager: Thiếu infoPanelPrefab hoặc infoPanelContainer!");
            return;
        }

        // Nếu đã có panel cho nhân vật này → hiển thị nó
        if (currentInfoPanels.TryGetValue(character, out GameObject existingPanel))
        {
            // Ẩn tất cả panel khác
            foreach (var panel in currentInfoPanels.Values)
                panel.SetActive(false);
            existingPanel.SetActive(true);
            currentState = State.Info;
            buttonContainer.gameObject.SetActive(false);
            return;
        }

        // Tạo panel mới
        GameObject panelGO = Instantiate(infoPanelPrefab, infoPanelContainer);
        currentInfoPanels[character] = panelGO;

        // Cập nhật thông tin
        var infoUI = panelGO.GetComponent<CharacterInfoUI>();
        if (infoUI != null)
            infoUI.Setup(character);
        else
            Debug.LogWarning("CharacterPanelManager: infoPanelPrefab thiếu component CharacterInfoUI!");

        // Ẩn tất cả panel khác
        foreach (var panel in currentInfoPanels.Values)
            panel.SetActive(false);
        panelGO.SetActive(true);

        currentState = State.Info;
        buttonContainer.gameObject.SetActive(false);
    }

    private void ShowMainPanel()
    {
        // Ẩn tất cả info panel
        foreach (var panel in currentInfoPanels.Values)
            panel.SetActive(false);
        buttonContainer.gameObject.SetActive(true);
        currentState = State.Main;
    }

    /// <summary>
    /// Gọi từ MapMenuManager khi nhấn ESC/chuột phải.
    /// Trả về true nếu đã xử lý (đang ở info → quay lại main).
    /// </summary>
    public bool TryGoBack()
    {
        if (currentState == State.Info)
        {
            ShowMainPanel();
            return true;
        }
        return false;
    }
}