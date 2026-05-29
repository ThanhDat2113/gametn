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
        var targets = result.InitialTargets;

        if (actorView == null) return 0.5f;

        // Gán skill và target để UnitView có thể truy cập nếu cần
        actorView.SetCurrentSkill(skill);
        if (targets.Any())
            actorView.SetCurrentTarget(targets.First());

        // 1. Spawn VFX for AtCaster and AtTarget modes
        SpawnSkillVFX(skill, actorView, targets);

        // 2. Handle Ranged Projectiles
        if (skill != null && skill.isRanged && skill.projectilePrefab != null && targets.Any())
        {
            StartCoroutine(FireProjectile(actorView, targets.First(), skill));
        }

        // 3. Setup Hit Handler to spawn HitOnEachTarget VFX
        Action onHitHandler = () => {
            foreach (var outcome in result.Outcomes)
            {
                var targetView = GetViewForUnit(outcome.Target);
                if (targetView == null) continue;

                if (cameraManager != null) cameraManager.PlayImpactShake();
                targetView.SetAnimationTrigger(AnimationConstants.Hurt);
                
                // Spawn VFX for HitOnEachTarget mode
                SpawnHitVFX(skill, targetView);
            }
        };

        // Register and cleanup handlers
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

        // 4. Play Animation
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

    private void SpawnSkillVFX(SkillData skill, UnitView actorView, List<CombatUnit> targets)
    {
        if (skill == null) return;

        // --- New Unified System ---
        if (skill.vfxEvents != null)
        {
            foreach (var evt in skill.vfxEvents)
            {
                if (evt.spawnMode == VFXSpawnMode.AtCaster || evt.spawnMode == VFXSpawnMode.AtTarget)
                {
                    Vector3 pos = GetVFXPosition(skill, evt, actorView, targets);
                    InstantiateVFX(evt, pos, actorView.transform);
                }
            }
        }

        // --- Backward Compatibility ---
        // 1. Legacy single vfxPrefab (treated as AtTarget)
        if (skill.vfxPrefab != null)
        {
            var fakeEvent = new VFXEvent { vfxPrefab = skill.vfxPrefab, offset = new Vector3(0, skill.vfxOffset, 0), spawnMode = VFXSpawnMode.AtTarget };
            Vector3 pos = GetVFXPosition(skill, fakeEvent, actorView, targets);
            InstantiateVFX(fakeEvent, pos, null);
        }

        // 2. Legacy rangedVfxEvents (treated as AtCaster)
        if (skill.rangedVfxEvents != null)
        {
            foreach (var evt in skill.rangedVfxEvents)
            {
                if (evt == null || evt.vfxPrefab == null) continue;
                var fakeEvent = new VFXEvent { vfxPrefab = evt.vfxPrefab, offset = evt.offset, spawnMode = VFXSpawnMode.AtCaster, attachToCaster = evt.attachToCaster };
                Vector3 pos = GetVFXPosition(skill, fakeEvent, actorView, targets);
                InstantiateVFX(fakeEvent, pos, actorView.transform);
            }
        }
    }

    private void SpawnHitVFX(SkillData skill, UnitView targetView)
    {
        if (skill == null || targetView == null) return;

        // --- New Unified System ---
        if (skill.vfxEvents != null)
        {
            foreach (var evt in skill.vfxEvents)
            {
                if (evt.spawnMode == VFXSpawnMode.HitOnEachTarget)
                {
                    InstantiateVFX(evt, targetView.transform.position + evt.offset, targetView.transform);
                }
            }
        }

        // --- Backward Compatibility ---
        // 1. Legacy hitVfxEvents
        if (skill.hitVfxEvents != null)
        {
            foreach (var evt in skill.hitVfxEvents)
            {
                if (evt == null || evt.vfxPrefab == null) continue;
                InstantiateVFX(evt, targetView.transform.position + evt.offset, targetView.transform);
            }
        }
    }

    private Vector3 GetVFXPosition(SkillData skill, VFXEvent evt, UnitView actorView, List<CombatUnit> targets)
    {
        switch (evt.spawnMode)
        {
            case VFXSpawnMode.AtCaster:
                return actorView.transform.position + evt.offset;

            case VFXSpawnMode.AtTarget:
                if (targets == null || !targets.Any())
                    return actorView.transform.position + evt.offset; // Fallback to caster if no target

                // For single target or non-AoE, use the first target
                if (skill == null || targets.Count == 1 || (targets.Count > 1 && skill.targetType != TargetType.AllEnemies && skill.targetType != TargetType.AllAllies))
                {
                    var targetView = GetViewForUnit(targets.First());
                    return targetView != null ? targetView.transform.position + evt.offset : actorView.transform.position + evt.offset;
                }
                else // AoE: find center point
                {
                    Vector3 center = Vector3.zero;
                    int count = 0;
                    foreach (var unit in targets)
                    {
                        var view = GetViewForUnit(unit);
                        if (view != null)
                        {
                            center += view.transform.position;
                            count++;
                        }
                    }
                    return count > 0 ? (center / count) + evt.offset : actorView.transform.position + evt.offset;
                }

            default:
                return actorView.transform.position + evt.offset;
        }
    }

    private void InstantiateVFX(VFXEvent evt, Vector3 position, Transform potentialParent)
    {
        if (evt.vfxPrefab == null) return;

        var vfx = Instantiate(evt.vfxPrefab, position, Quaternion.identity);
        if (evt.attachToCaster && potentialParent != null)
        {
            vfx.transform.SetParent(potentialParent);
        }

        var visualEffect = vfx.GetComponent<UnityEngine.VFX.VisualEffect>();
        if (visualEffect != null)
        {
            visualEffect.Play();
        }
        Destroy(vfx, 2f);
    }

    private IEnumerator FireProjectile(UnitView casterView, CombatUnit targetUnit, SkillData skill)
    {
        var targetView = GetViewForUnit(targetUnit);
        if (targetView == null) yield break;

        Vector3 startPos = casterView.transform.position + skill.projectileOffset;
        var projectile = UnityEngine.Object.Instantiate(skill.projectilePrefab, startPos, Quaternion.identity);

        float elapsed = 0f;
        while (elapsed < skill.projectileTravelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / skill.projectileTravelTime);
            projectile.transform.position = Vector3.Lerp(startPos, targetView.transform.position, t);
            yield return null;
        }

        projectile.transform.position = targetView.transform.position;
        
        // Hit VFX is now handled by the OnHit event, which is triggered after the projectile lands.
        // We just need to spawn the "HitOnEachTarget" VFX here for ranged attacks.
        SpawnHitVFX(skill, targetView);

        Destroy(projectile, 2f);
    }

    // Return the caster to its original position after the attack
    private IEnumerator ReturnPhase(UnitView actorView, Vector3 originPosition, ActionResult result)
    {
        // Move actor back to original position
        yield return StartCoroutine(MoveCoroutine(actorView, originPosition, returnDuration));
        // Play idle animation after return
        actorView.SetAnimationTrigger(AnimationConstants.Idle);
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