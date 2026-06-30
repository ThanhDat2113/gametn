using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;    // Ảnh đen full màn hình
    [SerializeField] private float fadeDuration = 1f; // Thời gian tối dần

    public void OnNewGameButton()
    {
        // Load qua BootScene để đảm bảo singleton được init đúng thứ tự
        SceneManager.LoadScene("BootScene");
    }
}