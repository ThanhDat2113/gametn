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
    public float faceOffDistance = 4.0f;
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

        // --- CHUỖI HÀNH ĐỘNG THEO GIAI ĐOẠN ---
        
        // Giai đoạn 1: Chuẩn bị sân khấu
        yield return StartCoroutine(SetupPhase(actorView, result));

        // Giai đoạn 2: Di chuyển đến mục tiêu
        Vector3 actorOrigin = actorView.transform.position;
        Vector3 targetPosition = primaryTargetView.transform.position;
        Vector3 direction = (targetPosition - actorOrigin).normalized;
        Vector3 attackPosition = targetPosition - direction * faceOffDistance;
        yield return StartCoroutine(ApproachPhase(actorView, attackPosition));

        // Giai đoạn 3: Thực hiện skill và hiệu ứng
        yield return StartCoroutine(ExecutePhase(result));
        
        yield return new WaitForSeconds(postSkillWait);

        // Giai đoạn 4: Quay về vị trí cũ
        yield return StartCoroutine(ReturnPhase(actorView, actorOrigin, result));

        // Giai đoạn 5: Dọn dẹp và reset toàn bộ
        yield return StartCoroutine(CleanupPhase());
    }

    #region Animation Phases

    private IEnumerator SetupPhase(UnitView actorView, ActionResult result)
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

        // Camera zoom vào actor
        if (cameraManager != null)
        {
            cameraManager.ZoomToUnit(actorView.transform, cameraManager.clashZoomSize);
            yield return new WaitForSeconds(cameraManager.zoomInDuration);
        }
    }

    private IEnumerator ApproachPhase(UnitView actorView, Vector3 attackPosition)
    {
        actorView.PlayAnimation(AnimationConstants.Rush);
        yield return StartCoroutine(MoveCoroutine(actorView, attackPosition, moveToTargetDuration));
    }

    private IEnumerator ExecutePhase(ActionResult result)
    {
        var actorView = GetViewForUnit(result.Actor);
        var skill = result.Skill;

        if (skill == null)
        {
            // Không có skill asset → chỉ play animation mặc định
            if (actorView != null)
            {
                actorView.SetAnimationTrigger(AnimationConstants.Attack);
                float animLength = actorView.GetClipLength(AnimationConstants.Attack);
                yield return new WaitForSeconds(animLength);
            }
            yield break;
        }

        // VISUAL ONLY: Damage đã được apply bởi effect.Apply() trong CombatManager.ResolveAction
        // Animation events chỉ trigger camera shake và hurt animation
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

        if (!string.IsNullOrEmpty(skill.animationTrigger))
        {
            actorView.SetAnimationTrigger(skill.animationTrigger);
            float animLength = actorView.GetClipLength(skill.animationTrigger);
            yield return new WaitForSeconds(animLength);
        }
        else
        {
            // Fallback nếu skill không có animation trigger
            actorView.SetAnimationTrigger(AnimationConstants.Attack);
            float animLength = actorView.GetClipLength(AnimationConstants.Attack);
            yield return new WaitForSeconds(animLength);
        }

        actorView.OnHitAnimationEvent -= onHitHandler;

        // Fallback: nếu sau animation vẫn còn pendingOutcomes chưa process → force apply
        if (actorView != null)
        {
            actorView.FlushPendingOutcomes();
        }
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