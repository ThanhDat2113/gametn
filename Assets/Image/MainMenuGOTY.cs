using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class HoverGlowEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI textComponent; // Kéo Text cần hiệu ứng vào đây
    public Color glowColor = Color.yellow;
    public float glowOuterSize = 0.5f;

    private Material materialInstance;
    private Color originalColor;
    private float originalOuter;

    void Start()
    {
        // Tạo một bản sao Material để không ảnh hưởng đến các Text khác
        materialInstance = new Material(textComponent.fontMaterial);
        textComponent.fontMaterial = materialInstance;

        // Lưu giá trị gốc
        originalColor = materialInstance.GetColor("_GlowColor");
        originalOuter = materialInstance.GetFloat("_GlowOuter");
        
        // Tắt Glow khi bắt đầu
        SetGlowActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetGlowActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetGlowActive(false);
    }

    void SetGlowActive(bool isActive)
    {
        if (isActive)
        {
            materialInstance.SetColor("_GlowColor", glowColor);
            materialInstance.SetFloat("_GlowOuter", glowOuterSize);
            // Bật keyword GLOW_ON để kích hoạt hiệu ứng (quan trọng!)
            materialInstance.EnableKeyword("GLOW_ON"); 
        }
        else
        {
            // Trả về màu và kích thước gốc
            materialInstance.SetColor("_GlowColor", originalColor);
            materialInstance.SetFloat("_GlowOuter", originalOuter);
            // Tắt keyword GLOW_ON để tắt hiệu ứng
            materialInstance.DisableKeyword("GLOW_ON"); 
        }
    }
}