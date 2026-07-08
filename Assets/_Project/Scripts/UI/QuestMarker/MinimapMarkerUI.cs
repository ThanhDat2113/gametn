using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public class MinimapMarkerUI : MonoBehaviour
{
    public enum OutOfRangeBehavior { Clamp, Hide }

    [Header("Out-of-range Behavior")]
    [SerializeField] private OutOfRangeBehavior outOfRangeBehavior = OutOfRangeBehavior.Clamp;

    [Header("UI References")]
    [SerializeField] private Image markerIcon;

    [SerializeField] private float edgePadding = 8f;

    private RectTransform _rectTransform;
    private CanvasGroup   _canvasGroup;
    private QuestMarkerBridge _targetBridge;

    public QuestMarkerBridge TargetBridge => _targetBridge;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        if (markerIcon == null) markerIcon = GetComponent<Image>();
    }

    public void InitializeFromBridge(QuestMarkerBridge bridge)
    {
        if (bridge == null) { Debug.LogError("[MinimapMarkerUI] bridge is null"); return; }
        _targetBridge = bridge;
    }

    private void Update()
    {
        if (_targetBridge == null || MinimapController.Instance == null) return;

        // Ẩn marker minimap khi đang có dialogue — đọc trực tiếp từ DialogueBubbleUI
        bool dialogueActive = DialogueBubbleUI.Instance != null && DialogueBubbleUI.Instance.IsShowing;
        if (dialogueActive)
        {
            _canvasGroup.alpha = 0f;
            return;
        }

        Vector2 mapPos = MinimapController.Instance.WorldToMinimapPosition(
            _targetBridge.MarkerPosition, out bool isWithinRange);

        if (isWithinRange)
        {
            _rectTransform.anchoredPosition = mapPos;
            _canvasGroup.alpha = 1f;
        }
        else
        {
            switch (outOfRangeBehavior)
            {
                case OutOfRangeBehavior.Clamp:
                    _rectTransform.anchoredPosition = MinimapController.Instance.ClampToMinimapEdge(mapPos, edgePadding);
                    _canvasGroup.alpha = 1f;
                    break;

                case OutOfRangeBehavior.Hide:
                    _canvasGroup.alpha = 0f;
                    break;
            }
        }
    }

    public void SetActive(bool active) => gameObject.SetActive(active);
}