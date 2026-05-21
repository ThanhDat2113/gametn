using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    private void Awake()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
    }

    public IEnumerator PlayAction(ActionResult result)
    {
        var actor = result.Actor;
        var targets = result.Outcomes.Select(o => o.Target).ToList();

        var actorView = GetViewForUnit(result.Actor);
        var primaryTargetView = GetViewForUnit(result.InitialTargets.FirstOrDefault());

        if (actorView == null || primaryTargetView == null)
        {
            Debug.LogWarning("[ActionAnimation] Actor hoặc Target View không tồn tại. Bỏ qua animation.");
            result.ApplyOutcomes();
            yield break;
        }

        // Camera zoom
        if (cameraManager != null)
        {
            cameraManager.ZoomToUnit(actorView.transform, cameraManager.clashZoomSize);
            yield return new WaitForSeconds(cameraManager.zoomInDuration);
        }

        // Di chuyển đến mục tiêu
        Vector3 actorOrigin = actorView.transform.position;
        Vector3 targetPosition = primaryTargetView.transform.position;
        Vector3 direction = (targetPosition - actorOrigin).normalized;
        Vector3 attackPosition = targetPosition - direction * faceOffDistance;

        actorView.SetAnimationTrigger("Rush");
        yield return StartCoroutine(MoveAndDimCoroutine(actorView, attackPosition, moveToTargetDuration, targets, true));

        var skill = result.Skill;
        if (skill == null)
        {
            result.ApplyOutcomes();
            yield break;
        }

        int hitCount = skill.hitCount > 0 ? skill.hitCount : 1;
        
        // Tạo handler và đăng ký
        Action onHitHandler = null;
        onHitHandler = () => {
            foreach (var outcome in result.Outcomes)
            {
                var targetView = GetViewForUnit(outcome.Target);
                if (targetView == null) continue;
                if (cameraManager != null) cameraManager.PlayImpactShake();
                int damagePerHit = outcome.Damage / hitCount;
                if (damagePerHit > 0)
                {
                    outcome.Target.TakeDamage(damagePerHit);
                    targetView.SetAnimationTrigger("Hurt");
                }
            }
        };

        actorView.OnHitAnimationEvent += onHitHandler;

        // Kích hoạt animation
        if (!string.IsNullOrEmpty(skill.animationTrigger))
        {
            actorView.SetAnimationTrigger(skill.animationTrigger);
            float animLength = actorView.GetClipLength(skill.animationTrigger);
            if (animLength <= 0.01f)
            {
                Debug.LogWarning($"[ClashAnimationSequence] Clip length for '{skill.animationTrigger}' is {animLength}. Using fallback 1.0f");
                animLength = 1.0f;
            }
            Debug.Log($"[ClashAnimationSequence] Playing animation '{skill.animationTrigger}', length={animLength:F2}s");
            yield return new WaitForSeconds(animLength);
        }
        else
        {
            Debug.LogWarning($"[ClashAnimationSequence] Skill '{skill.skillName}' has no animation trigger. Applying damage directly.");
        }

        // Hủy đăng ký sau khi animation kết thúc
        actorView.OnHitAnimationEvent -= onHitHandler;

        // Xử lý sát thương dư
        foreach (var outcome in result.Outcomes)
        {
            int remainingDamage = outcome.Damage % hitCount;
            if (remainingDamage > 0)
                outcome.Target.TakeDamage(remainingDamage);
        }
        result.ApplyNonDamageOutcomes();

        yield return new WaitForSeconds(postSkillWait);

        // Quay về vị trí
        actorView.SetAnimationTrigger("Idle");
        yield return StartCoroutine(MoveAndDimCoroutine(actorView, actorOrigin, returnDuration, targets, false));
        actorView.SetAnimationTrigger("Idle");

        ResetAllUnitColors();

        if (cameraManager != null)
        {
            cameraManager.ResetCamera();
            yield return new WaitForSeconds(cameraManager.zoomOutDuration);
        }
    }

    private IEnumerator MoveAndDimCoroutine(UnitView actorView, Vector3 targetPos, float duration, List<CombatUnit> targets, bool isMovingForward)
    {
        Vector3 startPos = actorView.transform.position;
        float elapsed = 0f;

        var allUnitViews = FindObjectsByType<UnitView>(FindObjectsSortMode.None);
        var involvedUnits = new HashSet<CombatUnit>(targets);
        involvedUnits.Add(actorView.LinkedUnit);
        var uninvolvedViews = allUnitViews.Where(v => v.LinkedUnit != null && !involvedUnits.Contains(v.LinkedUnit)).ToList();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            actorView.transform.position = Vector3.Lerp(startPos, targetPos, t);
            float currentAlpha = isMovingForward ? Mathf.Lerp(1f, dimAlpha, t) : Mathf.Lerp(dimAlpha, 1f, t);
            foreach (var view in uninvolvedViews) view.SetAlpha(currentAlpha);
            yield return null;
        }

        actorView.transform.position = targetPos;
        float finalAlpha = isMovingForward ? dimAlpha : 1f;
        foreach (var view in uninvolvedViews) view.SetAlpha(finalAlpha);
    }

    private UnitView GetViewForUnit(CombatUnit unit)
    {
        if (unit == null) return null;
        return FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LinkedUnit == unit);
    }

    private void ResetAllUnitColors()
    {
        var allUnitViews = FindObjectsByType<UnitView>(FindObjectsSortMode.None);
        foreach (var unitView in allUnitViews) unitView.SetAlpha(1f);
    }
}