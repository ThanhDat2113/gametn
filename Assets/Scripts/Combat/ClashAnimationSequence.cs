using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tái cấu trúc lại để quản lý chuỗi animation hành động một cách rõ ràng và mạnh mẽ hơn.
/// Logic được chia thành các "Phase" (giai đoạn) riêng biệt, được điều khiển bởi PlayAction.
/// </summary>
public class ClashAnimationSequence : MonoBehaviour
{
    [Header("References")]
    public CombatCameraManager cameraManager;

    [Header("Timing")]
    public float moveToTargetDuration = 0.3f;
    public float returnDuration = 0.4f;
    public float postSkillWait = 0.2f;

    [Header("Effects")]
    public float faceOffDistance = 2.0f;
    [Range(0, 1)]
    public float dimAlpha = 0.5f;

    private List<UnitView> allUnitViews = new List<UnitView>();

    private void Awake()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
    }

    /// <summary>
    /// Phương thức "đạo diễn", điều khiển toàn bộ chuỗi animation.
    /// </summary>
    public IEnumerator PlayAction(ActionResult result)
    {
        // Tìm tất cả UnitView một lần để tối ưu
        allUnitViews = FindObjectsByType<UnitView>(FindObjectsSortMode.None).ToList();

        var actorView = GetViewForUnit(result.Actor);
        var primaryTargetView = GetViewForUnit(result.InitialTargets.FirstOrDefault());

        if (actorView == null || primaryTargetView == null)
        {
            Debug.LogWarning("[ActionAnimation] Actor hoặc Target View không tồn tại. Bỏ qua animation.");
            result.ApplyOutcomes();
            yield break;
        }

        bool shouldMove = ShouldCharacterMove(result.Actor, result.Skill);

        // --- CHUỖI HÀNH ĐỘNG THEO GIAI ĐOẠN ---
        
        // Giai đoạn 1: Chuẩn bị sân khấu
        yield return StartCoroutine(SetupPhase(actorView, result, shouldMove));

        Vector3 actorOrigin = actorView.transform.position;

        // Giai đoạn 2: Di chuyển (nếu cần)
        if (shouldMove)
        {
            Vector3 targetPosition = primaryTargetView.transform.position;
            Vector3 direction = (targetPosition - actorOrigin).normalized;
            Vector3 attackPosition = targetPosition - direction * faceOffDistance;
            yield return StartCoroutine(ApproachPhase(actorView, attackPosition));
        }

        // Giai đoạn 3: Thực hiện skill và hiệu ứng
        float animationLength = ExecutePhase(result);
        yield return new WaitForSeconds(animationLength + postSkillWait);

        // Giai đoạn 4: Quay về (nếu đã di chuyển)
        if (shouldMove)
        {
            yield return StartCoroutine(ReturnPhase(actorView, actorOrigin, result));
        }

        // Giai đoạn 5: Dọn dẹp và reset toàn bộ
        yield return StartCoroutine(CleanupPhase());
    }

    private bool ShouldCharacterMove(CombatUnit actor, SkillData skill)
    {
        if (skill == null) return true; // Mặc định di chuyển nếu không có skill data

        switch (skill.movementOverride)
        {
            case SkillMovementOverride.ForceRushToTarget:
                return true;
            case SkillMovementOverride.ForceStationary:
                return false;
            case SkillMovementOverride.InheritFromCharacter:
            default:
                return actor.Data.defaultCombatStyle == CombatStyle.Melee;
        }
    }

    #region Animation Phases

    private IEnumerator SetupPhase(UnitView actorView, ActionResult result, bool isMoving)
    {
        // Reset màu sắc của tất cả unit
        SetAllUnitAlphas(1.0f);

        // Làm mờ các unit không liên quan
        var involvedUnits = new HashSet<CombatUnit>(result.Outcomes.Select(o => o.Target));
        involvedUnits.Add(result.Actor);
        foreach (var view in allUnitViews)
        {
            if (view.LinkedUnit != null && !involvedUnits.Contains(view.LinkedUnit))
            {
                view.SetAlpha(dimAlpha);
            }
        }

        // Camera logic
        if (cameraManager != null)
        {
            if (isMoving)
            {
                // Hành vi cũ: zoom vào actor
                cameraManager.ZoomToUnit(actorView.transform, cameraManager.clashZoomSize);
                yield return new WaitForSeconds(cameraManager.zoomInDuration);
            }
            else
            {
                // Hành vi mới: bao quát tất cả các đối tượng liên quan
                var allTargets = new List<UnitView> { actorView };
                allTargets.AddRange(result.InitialTargets.Select(t => GetViewForUnit(t)));
                cameraManager.FrameTargets(allTargets.Where(v => v != null).Distinct().ToList());
                yield return new WaitForSeconds(cameraManager.zoomInDuration); // Chờ một chút cho camera bắt đầu zoom
            }
        }
    }

    private IEnumerator ApproachPhase(UnitView actorView, Vector3 attackPosition)
    {
        actorView.PlayAnimation(AnimationConstants.Rush);
        yield return StartCoroutine(MoveCoroutine(actorView, attackPosition, moveToTargetDuration));
    }

    private float ExecutePhase(ActionResult result)
    {
        var actorView = GetViewForUnit(result.Actor);
        var skill = result.Skill;
        float animLength = 0.5f; // Thời gian chờ mặc định

        if (skill == null)
        {
            if (actorView != null)
            {
                actorView.SetAnimationTrigger(AnimationConstants.Attack);
                animLength = actorView.GetClipLength(AnimationConstants.Attack);
            }
            return animLength;
        }

        Action onHitHandler = () => {
            foreach (var outcome in result.Outcomes)
            {
                var targetView = GetViewForUnit(outcome.Target);
                if (targetView == null) continue;

                if (cameraManager != null) cameraManager.PlayImpactShake();
                targetView.SetAnimationTrigger(AnimationConstants.Hurt);
            }
        };

        actorView.OnHitAnimationEvent += onHitHandler;
        
        // Gán một delegate để có thể hủy đăng ký event sau này
        Action cleanupHandler = null;
        cleanupHandler = () => {
            actorView.OnHitAnimationEvent -= onHitHandler;
            // Hủy đăng ký chính nó
            if (actorView != null)
            {
                actorView.OnAnimationEndEvent -= cleanupHandler;
                actorView.FlushPendingOutcomes();
            }
        };
        actorView.OnAnimationEndEvent += cleanupHandler;


        if (!string.IsNullOrEmpty(skill.animationTrigger))
        {
            actorView.SetAnimationTrigger(skill.animationTrigger);
            animLength = actorView.GetClipLength(skill.animationTrigger);
        }
        else
        {
            actorView.SetAnimationTrigger(AnimationConstants.Attack);
            animLength = actorView.GetClipLength(AnimationConstants.Attack);
        }

        return animLength;
    }

    private IEnumerator ReturnPhase(UnitView actorView, Vector3 originPosition, ActionResult result)
    {
        // Reset animation của các mục tiêu về Idle
        foreach (var outcome in result.Outcomes)
        {
            var targetView = GetViewForUnit(outcome.Target);
            if (targetView != null) targetView.PlayAnimation(AnimationConstants.Idle);
        }

        // Actor di chuyển về
        actorView.PlayAnimation(AnimationConstants.Idle); // Chuyển sang Idle để chạy về
        yield return StartCoroutine(MoveCoroutine(actorView, originPosition, returnDuration));
    }

    private IEnumerator CleanupPhase()
    {
        // Hard Reset: Đảm bảo TẤT CẢ các unit đều ở trạng thái Idle
        foreach (var view in allUnitViews)
        {
            if(view != null) view.PlayAnimation(AnimationConstants.Idle);
        }

        // Khôi phục màu sắc của tất cả unit
        SetAllUnitAlphas(1.0f);

        // Reset camera
        if (cameraManager != null)
        {
            cameraManager.ResetCamera();
            yield return new WaitForSeconds(cameraManager.zoomOutDuration);
        }
    }

    #endregion

    #region Helper Methods

    private IEnumerator MoveCoroutine(UnitView view, Vector3 targetPos, float duration)
    {
        Vector3 startPos = view.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            view.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        view.transform.position = targetPos;
    }

    private UnitView GetViewForUnit(CombatUnit unit)
    {
        if (unit == null) return null;
        return allUnitViews.FirstOrDefault(v => v.LinkedUnit == unit);
    }

    private void SetAllUnitAlphas(float alpha)
    {
        foreach (var view in allUnitViews)
        {
            if(view != null) view.SetAlpha(alpha);
        }
    }

    #endregion
}