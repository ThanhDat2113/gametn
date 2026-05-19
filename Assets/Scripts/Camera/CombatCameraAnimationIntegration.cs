using System.Collections;
using System.Linq;
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
            combat.OnActionResolved += OnActionHappened;
        }
    }

    private void OnActionHappened(ActionResult result)
    {
        // Khi một hành động được thực hiện, có thể lia camera hoặc shake
        if (cameraManager != null && result.InitialTargets.Any())
        {
            var primaryTarget = result.InitialTargets.First();
            var targetView = FindObjectsByType<UnitView>(FindObjectsSortMode.None)
                .FirstOrDefault(v => v.LinkedUnit == primaryTarget);

            if(targetView != null)
            {
                cameraManager.ZoomToUnit(targetView.transform, cameraManager.damageZoomSize);
                cameraManager.PlayImpactShake();
            }
        }
    }
}