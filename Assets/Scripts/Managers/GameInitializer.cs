using UnityEngine;

/// <summary>
/// Tự động tạo các singleton khi game khởi động.
/// Gắn script này vào GameObject bất kỳ trong scene đầu tiên (MainMenu/Intro).
/// </summary>
public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        // Tạo PlayerProgressData nếu chưa có
        if (PlayerProgressData.Instance == null)
        {
            var progressGO = new GameObject("PlayerProgressData");
            progressGO.AddComponent<PlayerProgressData>();
            DontDestroyOnLoad(progressGO);
            Debug.Log("[GameInitializer] Đã tạo PlayerProgressData singleton.");
        }

        // Tạo CombatExperienceManager nếu chưa có
        if (CombatExperienceManager.Instance == null)
        {
            var expGO = new GameObject("CombatExperienceManager");
            expGO.AddComponent<CombatExperienceManager>();
            DontDestroyOnLoad(expGO);
            Debug.Log("[GameInitializer] Đã tạo CombatExperienceManager singleton.");
        }
    }
}