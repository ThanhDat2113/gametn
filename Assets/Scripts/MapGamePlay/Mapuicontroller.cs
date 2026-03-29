using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MapUIController : MonoBehaviour
{
    [Header("Location Info Panel")]
    public GameObject locationInfoPanel;
    public TextMeshProUGUI locationNameText;
    public TextMeshProUGUI locationDescriptionText;
    public Image locationIcon;
    public TextMeshProUGUI nodeTypeText;

    [Header("Travel Prompt")]
    public GameObject travelPromptPanel;
    public TextMeshProUGUI travelDestinationText;
    public Button travelConfirmButton;
    public Button travelCancelButton;

    [Header("Tooltip (khi hover)")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public float tooltipOffset = 50f;

    [Header("Discovery Effect")]
    public GameObject discoveryEffectPrefab;  
    public TextMeshProUGUI discoveryText;
    public float discoveryDisplayTime = 2f;

    private System.Action _onTravelConfirmed;
    private Coroutine _tooltipCoroutine;

    void Awake()
    {
        // Setup buttons
        if (travelConfirmButton)
            travelConfirmButton.onClick.AddListener(ConfirmTravel);
        if (travelCancelButton)
            travelCancelButton.onClick.AddListener(HideTravelPrompt);

        // Ẩn panels ban đầu
        if (travelPromptPanel) travelPromptPanel.SetActive(false);
        if (locationInfoPanel) locationInfoPanel.SetActive(false);
        HideTooltip();
    }

    // CHỈNH SỬA: Nhận dữ liệu trực tiếp thay vì nhận object MapNode
    public void ShowLocationInfo(string locName, string locDesc, Sprite locIcon, string typeLabel)
    {
        if (locationNameText) locationNameText.text = locName;
        if (locationDescriptionText) locationDescriptionText.text = locDesc;
        if (locationIcon) locationIcon.sprite = locIcon;
        if (nodeTypeText) nodeTypeText.text = typeLabel;

        if (locationInfoPanel)
        {
            locationInfoPanel.SetActive(true);
            StartCoroutine(AnimatePanel(locationInfoPanel, true));
        }
    }

    public void HideLocationInfo()
    {
        if (locationInfoPanel && locationInfoPanel.activeSelf)
        {
            StartCoroutine(AnimatePanel(locationInfoPanel, false));
        }
    }

    // CHỈNH SỬA: Hiệu ứng khám phá nhận string tên địa điểm và vị trí Vector3
    public void ShowDiscoveryEffect(string locName, Vector3 worldPos)
    {
        StartCoroutine(PlayDiscoveryEffect(locName, worldPos));
    }

    IEnumerator PlayDiscoveryEffect(string locName, Vector3 worldPos)
    {
        if (discoveryText)
        {
            discoveryText.text = $"✨ Đã khám phá: {locName}";
            discoveryText.gameObject.SetActive(true);

            CanvasGroup cg = discoveryText.GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.alpha = 0;
                float t = 0;
                while (t < 0.3f) { t += Time.deltaTime; cg.alpha = t / 0.3f; yield return null; }
            }

            yield return new WaitForSeconds(discoveryDisplayTime);

            if (cg)
            {
                float t = 0;
                while (t < 0.3f) { t += Time.deltaTime; cg.alpha = 1 - (t / 0.3f); yield return null; }
            }
            discoveryText.gameObject.SetActive(false);
        }

        if (discoveryEffectPrefab)
        {
            Instantiate(discoveryEffectPrefab, worldPos, Quaternion.identity);
        }
    }

    // Giữ nguyên các hàm bổ trợ khác...
    public void HideTooltip() { if (tooltipPanel) tooltipPanel.SetActive(false); }
    public void HideTravelPrompt() { if (travelPromptPanel) travelPromptPanel.SetActive(false); }
    void ConfirmTravel() { _onTravelConfirmed?.Invoke(); HideTravelPrompt(); }

    IEnumerator AnimatePanel(GameObject panel, bool show)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) { panel.SetActive(show); yield break; }
        float start = show ? 0 : 1;
        float end = show ? 1 : 0;
        float elapsed = 0;
        while (elapsed < 0.2f) { elapsed += Time.deltaTime; cg.alpha = Mathf.Lerp(start, end, elapsed / 0.2f); yield return null; }
        cg.alpha = end;
        if (!show) panel.SetActive(false);
    }
}