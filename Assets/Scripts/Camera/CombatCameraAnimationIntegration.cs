using System.Collections;
using UnityEngine;

/// <summary>
/// Bridge giữa Combat System và Camera System.
/// Gọi các method của CombatCameraManager tại đúng thời điểm.
///
/// SETUP:
/// 1. Add vào trong CombatManager GameObject
/// 2. Gán CombatCameraManager reference vào inspector
/// </summary>
public class CombatCameraAnimationIntegration : MonoBehaviour
{
    [SerializeField] private CombatCameraManager cameraManager;

    private void Start()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();

        if (cameraManager == null)
        {
            Debug.LogError("[CombatCameraAnimationIntegration] CombatCameraManager not found!");
            return;
        }

        // Subscribe vào CombatManager events để trigger camera effects
        CombatManager combat = CombatManager.Instance;
        if (combat != null)
        {
            // Khi clash bắt đầu
            combat.OnClashResolved += OnClashHappened;
        }
    }

    /// <summary>
    /// Gọi từ ClashAnimationSequence khi 2 unit va chạm
    /// </summary>
    public void OnClashAnimationStart(Transform attacker, Transform defender)
    {
        if (cameraManager == null) return;

        // Zoom vào điểm giữa 2 unit
        cameraManager.PlayClashZoom(attacker, defender);
    }

    /// <summary>
    /// Gọi từ ClashAnimationSequence sau KnockBack (winner thắng)
    /// </summary>
    public void OnClashWinnerAttack(Transform winner)
    {
        if (cameraManager == null) return;

        // Zoom vào winner + shake
        cameraManager.ZoomToUnit(winner, cameraManager.damageZoomSize);
        cameraManager.PlayImpactShake();
    }

    /// <summary>
    /// Gọi từ UnitView khi unit bị damage
    /// </summary>
    public void OnUnitTakeDamage(Transform targetUnit)
    {
        if (cameraManager == null) return;

        // Light zoom vào unit bị damage
        cameraManager.ZoomToUnit(targetUnit, cameraManager.damageZoomSize);
        cameraManager.PlayImpactShake();
    }

    /// <summary>
    /// Event handler
    /// </summary>
    private void OnClashHappened(ClashResult result)
    {
        // (Optional) có thể thêm logic khác
    }
}
