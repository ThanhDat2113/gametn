using UnityEngine;
using TMPro;

/// <summary>
/// Gắn script này lên GameObject chứa TMP_Text hiển thị tình trạng quest theo khu vực,
/// nằm trong Persistent Scene (DontDestroyOnLoad). Vì Canvas này tồn tại xuyên suốt các
/// scene, các script AreaQuestStatusUI ở từng Map (Map1, Map2...) không thể kéo tay
/// reference chéo scene vào được — nên gọi qua Instance thay vì Inspector reference.
///
/// SETUP:
///   1. Gắn script này lên GameObject cùng Canvas ở Persistent Scene.
///   2. Kéo TMP_Text hiển thị vào field statusText.
///   3. Các AreaQuestStatusUI ở từng scene sẽ tự gọi AreaQuestStatusDisplay.Instance.SetText(...).
/// </summary>
public class AreaQuestStatusDisplay : MonoBehaviour
{
    public static AreaQuestStatusDisplay Instance { get; private set; }

    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetText(string text)
    {
        if (statusText != null) statusText.text = text;
    }
}
