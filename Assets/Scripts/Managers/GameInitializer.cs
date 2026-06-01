
using UnityEngine;

/// <summary>
/// Tự động khởi tạo các hệ thống quản lý toàn cục (singletons) khi game bắt đầu.
/// Gắn script này vào một GameObject trong scene đầu tiên của game (VD: Main Menu).
/// </summary>
public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        // Tạo PlayerProgressData nếu chưa có
        if (PlayerProgressData.Instance == null)
        {
            GameObject progressManager = new GameObject("PlayerProgressData");
            progressManager.AddComponent<PlayerProgressData>();
            Debug.Log("[GameInitializer] PlayerProgressData instance created.");
        }

        // Tạo CombatExperienceManager nếu chưa có
        if (CombatExperienceManager.Instance == null)
        {
            GameObject expManager = new GameObject("CombatExperienceManager");
            expManager.AddComponent<CombatExperienceManager>();
            Debug.Log("[GameInitializer] CombatExperienceManager instance created.");
        }
    }
}