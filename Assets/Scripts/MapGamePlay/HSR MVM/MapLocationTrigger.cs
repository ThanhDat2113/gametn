using UnityEngine;

public class MapLocationTrigger : MonoBehaviour
{
    public string locationName = "Ngôi Làng U Ám";
    [TextArea] public string description = "Một nơi đầy sương mù...";
    public Sprite icon;
    public string typeLabel = "🏘 Thị Trấn";
    
    private bool isDiscovered = false;
    private MapUIController ui;

    void Start() {
        ui = FindAnyObjectByType<MapUIController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && ui != null)
        {
            // Gọi hàm đã sửa với đầy đủ tham số
            ui.ShowLocationInfo(locationName, description, icon, typeLabel);

            // Nếu là lần đầu bước vào, hiện hiệu ứng khám phá
            if (!isDiscovered) {
                ui.ShowDiscoveryEffect(locationName, transform.position);
                isDiscovered = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && ui != null)
        {
            ui.HideLocationInfo();
        }
    }
}