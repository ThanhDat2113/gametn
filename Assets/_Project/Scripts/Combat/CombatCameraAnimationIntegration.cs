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
        if (cameraManager == null || result.InitialTargets == null || !result.InitialTargets.Any()) return;

        // Skill đa mục tiêu (AOE): bỏ qua — ClashAnimationSequence đã xử lý camera
        // AOE (FocusAOEAction + AdvanceAOEZoom) để center = toàn bộ nhóm target.
        bool isAOE = (result.Skill != null &&
            (result.Skill.targetType == TargetType.AllEnemies || result.Skill.targetType == TargetType.AllAllies))
            || result.InitialTargets.Count > 1;
        if (isAOE) return;

        // Skill đơn mục tiêu: lia camera + shake như cũ
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