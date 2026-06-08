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
    private Dictionary<CharacterData, int> expGainedThisBattle = new Dictionary<CharacterData, int>();

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
        combat.OnCombatStarted += OnCombatStarted;
        combat.OnVictory += OnVictory;

        // Lắng nghe sự kiện EXP để tích lũy
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnExperienceGained += OnExperienceGained;
        else
            Debug.LogWarning("[VictoryPanelController] PlayerProgression chưa sẵn sàng.");

        // Đảm bảo panel bị ẩn lúc đầu
        if (victoryPanel != null)
            victoryPanel.gameObject.SetActive(false);
    }

    private void OnCombatStarted()
    {
        expGainedThisBattle.Clear();
    }

    private void OnExperienceGained(CharacterData character, int amount)
    {
        if (expGainedThisBattle.ContainsKey(character))
            expGainedThisBattle[character] += amount;
        else
            expGainedThisBattle[character] = amount;
    }

    private void OnVictory()
    {
        if (victoryPanel == null)
        {
            Debug.LogError("[VictoryPanelController] Chưa gán VictoryPanel!");
            return;
        }

        // Hiển thị panel với dữ liệu
        victoryPanel.Show(combat.PlayerUnits, expGainedThisBattle);
    }

    private void OnDestroy()
    {
        if (combat != null)
        {
            combat.OnCombatStarted -= OnCombatStarted;
            combat.OnVictory -= OnVictory;
        }
        if (PlayerProgression.Instance != null)
            PlayerProgression.Instance.OnExperienceGained -= OnExperienceGained;
    }
}