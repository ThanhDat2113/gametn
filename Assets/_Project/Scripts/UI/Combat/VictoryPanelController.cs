using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều khiển panel chiến thắng có sẵn trong scene.
/// </summary>
public class VictoryPanelController : MonoBehaviour
{
    [Header("Victory Panel (gán từ Inspector)")]
    public VictoryPanel victoryPanel; // Tham chiếu đến GameObject có script VictoryPanel

    private CombatManager combat;

    private void Awake()
    {
        combat = CombatManager.Instance;
        if (combat == null)
        {
            Debug.LogError("[VictoryPanelController] Không tìm thấy CombatManager!");
            enabled = false;
            return;
        }

        // Đăng ký sự kiện combat
        combat.OnVictory += OnVictory;

        // Đảm bảo panel bị ẩn lúc đầu
        if (victoryPanel != null)
            victoryPanel.gameObject.SetActive(false);
    }

    private void OnVictory(Dictionary<CharacterData, int> expGained)
    {
        if (victoryPanel == null)
        {
            Debug.LogError("[VictoryPanelController] Chưa gán VictoryPanel!");
            return;
        }

        // Hiển thị panel với dữ liệu
        victoryPanel.Show(combat.PlayerUnits, expGained);
    }

    private void OnDestroy()
    {
        if (combat != null)
        {
            combat.OnVictory -= OnVictory;
        }
    }
}