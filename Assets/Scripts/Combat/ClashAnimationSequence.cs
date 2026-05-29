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
    public float faceOffDistance = 2.0f;
    [Range(0, 1)]
    public float dimAlpha = 0.5f;

    private List<UnitView> allUnitViews = new();

    private void Awake()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
    }

    public IEnumerator PlayAction(ActionResult result)
    {
        allUnitViews = FindObjectsByType<UnitView>(FindObjectsSortMode.None).ToList();

        var actorView = GetViewForUnit(result.Actor);
        var primaryTargetView = GetViewForUnit(result.InitialTargets.FirstOrDefault());

        if (actorView == null)
        {
            Debug.LogWarning("[ActionAnimation] Actor View không tồn tại. Bỏ qua animation.");
            result.ApplyOutcomes();
            yield break;
        }

        // Cho phép animation chạy ngay cả khi không có target (ví dụ: kỹ năng tự buff)
        if (primaryTargetView == null)
        {
            Debug.Log("[ActionAnimation] Không có Target View (có thể là kỹ năng tự buff). Vẫn tiếp tục animation.");
        }

        bool shouldMove = ShouldCharacterMove(result.Actor, result.Skill);

        yield return StartCoroutine(SetupPhase(actorView, result, shouldMove));

        Vector3 actorOrigin = actorView.transform.position;

        if (shouldMove)
        {
            Vector3 targetPosition = primaryTargetView.transform.position;
            Vector3 direction = (targetPosition - actorOrigin).normalized;
            Vector3 attackPosition = targetPosition - direction * faceOffDistance;
            yield return StartCoroutine(ApproachPhase(actorView, attackPosition));
        }

        float animationLength = ExecutePhase(result);
        yield return new WaitForSeconds(animationLength + postSkillWait);

        if (shouldMove)
        {
            yield return StartCoroutine(ReturnPhase(actorView, actorOrigin, result));
        }

        // KHÔNG apply non-damage effects ở đây! ResolveAction() đã apply chúng rồi.
        // Nếu apply lại sẽ gây double buff/heal.
        Debug.Log($"[ActionAnimation] Non-damage effects were already applied in ResolveAction. Skipping.");

        yield return StartCoroutine(CleanupPhase());
    }

    private bool ShouldCharacterMove(CombatUnit actor, SkillData skill)
    {
        if (skill == null) return true;
        switch (skill.movementOverride)
        {
            case SkillMovementOverride.ForceRushToTarget: return true;
            case SkillMovementOverride.ForceStationary: return false;
            default: return actor.Data.defaultCombatStyle == CombatStyle.Melee;
        }
    }

    private IEnumerator SetupPhase(UnitView actorView, ActionResult result, bool isMoving)
    {
        SetAllUnitAlphas(1.0f);

        var involvedUnits = new HashSet<CombatUnit>(result.Outcomes.Select(o => o.Target));
        involvedUnits.Add(result.Actor);
        foreach (var view in allUnitViews)
        {
            if (view.LinkedUnit != null && !involvedUnits.Contains(view.LinkedUnit))
                view.SetAlpha(dimAlpha);
        }

        if (cameraManager != null)
        {
            if (isMoving)
            {
                cameraManager.ZoomToUnit(actorView.transform, cameraManager.clashZoomSize);
                yield return new WaitForSeconds(cameraManager.zoomInDuration);
            }
            else
            {
                var allTargets = new List<UnitView> { actorView };
                allTargets.AddRange(result.InitialTargets.Select(t => GetViewForUnit(t)));
                cameraManager.FrameTargets(allTargets.Where(v => v != null).Distinct().ToList());
                yield return new WaitForSeconds(cameraManager.zoomInDuration);
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
        var primaryTarget = result.InitialTargets.FirstOrDefault();

        if (actorView == null) return 0.5f;

        // ⭐ GÁN SKILL VÀ TARGET ĐỂ UNITVIEW CÓ THỂ SPAWN VFX ⭐
        actorView.SetCurrentSkill(skill);
        if (primaryTarget != null)
            actorView.SetCurrentTarget(primaryTarget);

        // Spawn VFX ngay khi bắt đầu ExecutePhase (cho cả buff/heal/damage skills)
        // Ưu tiên vfxEvents[0] (vì [HideInInspector] vfxPrefab không gán được trong Inspector)
        GameObject vfxToSpawn = null;
        Vector3 vfxSpawnOffset = Vector3.up * 1.5f;
        bool vfxAttachToCaster = false;

        if (skill != null)
        {
            // Check vfxEvents array first (mới, có Inspector UI)
            if (skill.vfxEvents != null && skill.vfxEvents.Length > 0 && skill.vfxEvents[0] != null && skill.vfxEvents[0].vfxPrefab != null)
            {
                vfxToSpawn = skill.vfxEvents[0].vfxPrefab;
                vfxSpawnOffset = skill.vfxEvents[0].offset;
                vfxAttachToCaster = skill.vfxEvents[0].attachToCaster;
            }
            // Fallback to legacy vfxPrefab
            else if (skill.vfxPrefab != null)
            {
                vfxToSpawn = skill.vfxPrefab;
                vfxSpawnOffset = new Vector3(0, skill.vfxOffset, 0);
            }
        }

        if (vfxToSpawn != null)
        {
            // Determine spawn position: target if exists, else actor (self-buff)
            Vector3 spawnPos;
            Transform parent = null;
            if (primaryTarget != null)
            {
                var targetView = GetViewForUnit(primaryTarget);
                if (targetView != null)
                {
                    spawnPos = targetView.transform.position + vfxSpawnOffset;
                    parent = targetView.transform;
                }
                else
                {
                    spawnPos = actorView.transform.position + vfxSpawnOffset;
                    parent = actorView.transform;
                }
            }
            else
            {
                spawnPos = actorView.transform.position + vfxSpawnOffset;
                parent = actorView.transform;
            }

            var vfx = UnityEngine.Object.Instantiate(vfxToSpawn, spawnPos, Quaternion.identity);
            if (vfxAttachToCaster && parent != null)
                vfx.transform.SetParent(parent);
            UnityEngine.Object.Destroy(vfx, 2f);
            Debug.Log($"[ActionAnimation] Spawned VFX '{vfxToSpawn.name}' at {spawnPos}");
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
        Action cleanupHandler = null;
        cleanupHandler = () => {
            actorView.OnHitAnimationEvent -= onHitHandler;
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
            return actorView.GetClipLength(skill.animationTrigger);
        }
        else
        {
            actorView.SetAnimationTrigger(AnimationConstants.Attack);
            return actorView.GetClipLength(AnimationConstants.Attack);
        }
    }

    private IEnumerator ReturnPhase(UnitView actorView, Vector3 originPosition, ActionResult result)
    {
        foreach (var outcome in result.Outcomes)
        {
            var targetView = GetViewForUnit(outcome.Target);
            if (targetView != null) targetView.PlayAnimation(AnimationConstants.Idle);
        }
        actorView.PlayAnimation(AnimationConstants.Idle);
        yield return StartCoroutine(MoveCoroutine(actorView, originPosition, returnDuration));
    }

    private IEnumerator CleanupPhase()
    {
        foreach (var view in allUnitViews)
            if (view != null) view.PlayAnimation(AnimationConstants.Idle);
        SetAllUnitAlphas(1.0f);
        if (cameraManager != null)
        {
            cameraManager.ResetCamera();
            yield return new WaitForSeconds(cameraManager.zoomOutDuration);
        }
    }

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
            if (view != null) view.SetAlpha(alpha);
    }
}