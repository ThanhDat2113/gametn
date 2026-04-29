using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuUI;
    private bool isOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        isOpen = !isOpen;
        menuUI.SetActive(isOpen);

        // Optional: pause game
        Time.timeScale = isOpen ? 0f : 1f;

        // Optional: mở chuột
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }
}