using UnityEngine;

/// <summary>
/// Đặt component này trên GameObject "Letterbox".
/// Gán LetterboxTop và LetterboxBottom (RectTransform) trong Inspector.
/// </summary>
public class LetterboxController : MonoBehaviour
{
    [Header("UI")]
    public RectTransform topBar;
    public RectTransform bottomBar;

    private float _screenH;

    void Awake() => _screenH = Screen.height;

    public void SetHeight(float ratio)
    {
        float px = _screenH * ratio;
        if (topBar    != null) topBar.sizeDelta    = new Vector2(topBar.sizeDelta.x,    px);
        if (bottomBar != null) bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, px);
    }
}