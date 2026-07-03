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
    private int _lastHitCounter = 0;

    private void Awake()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
    }

    public IEnumerator PlayAction(ActionResult result)
    {
        if (CombatManager.Instance != null)
            allUnitViews = CombatManager.Instance.GetAllUnitViews();
        else
            allUnitViews = FindObjectsByType<UnitView>(FindObjectsSortMode.None).ToList();

        var actorView = GetViewForUnit(result.Actor);
        var primaryTargetView = GetViewForUnit(result.InitialTargets.FirstOrDefault());

        if (actorView == null)
        {
            Debug.LogWarning("[ActionAnimation] Actor View không tồn tại. Bỏ qua animation.");
            result.ApplyOutcomes();
            yield break;
        }

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

        _lastHitCounter = 0;
        float animationLength = ExecutePhase(result);
        yield return new WaitForSeconds(animationLength + postSkillWait);

        if (_lastHitCounter == 0 && actorView != null)
        {
            Debug.Log($"[Anim] {result.Skill?.skillName} không có Hit event — force flush fallback.");
            // SFX hit 0 đã phát trong ExecutePhase, không phát lại
            actorView.FlushPendingOutcomes();
        }

        if (shouldMove)
            yield return StartCoroutine(ReturnPhase(actorView, actorOrigin, result));

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
            if (view.LinkedUnit != null && !involvedUnits.Contains(view.LinkedUnit))
                view.SetAlpha(dimAlpha);

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

        // KHÔNG set currentSkill để vô hiệu hóa ProcessVFXAtFrame (tránh duplicate VFX)
        // VFX được spawn hoàn toàn từ SpawnHitVFX
        if (targets.Any())
            actorView.SetCurrentTarget(targets.First());

        // Spawn VFX + SFX hit 0 ngay (cho skill buff không có OnHit event)
        SpawnHitVFX(skill, actorView, targets, 0);
        if (CombatAudioManager.Instance != null && skill != null)
            CombatAudioManager.Instance.PlaySkillSFX(skill.sfxClips, 0);

        // Hit Handler - VFX hit 1+ + SFX hit 1+ + shake + hurt
        _lastHitCounter = 0;
        Action onHitHandler = () => {
            int currentHit = _lastHitCounter++;

            // Hit 0 đã spawn VFX + SFX ở trên, handler chỉ xử lý hit 1+
            if (currentHit > 0)
            {
                SpawnHitVFX(skill, actorView, targets, currentHit);
                if (CombatAudioManager.Instance != null && skill != null)
                    CombatAudioManager.Instance.PlaySkillSFX(skill.sfxClips, currentHit);
            }
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

    private void SpawnHitVFX(SkillData skill, UnitView actorView, List<CombatUnit> targets, int hitIndex)
    {
        if (skill == null) return;

        // Legacy vfxPrefab: spawn ở hit 0
        if (hitIndex == 0 && skill.vfxPrefab != null)
        {
            var fakeEvent = new VFXEvent { vfxPrefab = skill.vfxPrefab, offset = new Vector3(0, skill.vfxOffset, 0), spawnMode = VFXSpawnMode.AtTarget };
            Vector3 pos = GetVFXPosition(skill, fakeEvent, actorView, targets);
            InstantiateVFX(fakeEvent, pos, null);
        }

        if (skill.vfxEvents != null)
        {
            for (int i = 0; i < skill.vfxEvents.Length; i++)
            {
                var evt = skill.vfxEvents[i];
                if (evt == null || evt.vfxPrefab == null) continue;

                if (hitIndex == 0)
                {
                    // Hit 0: spawn AtCaster (tất cả) + AtTarget[0] (VFX đầu)
                    if (evt.spawnMode == VFXSpawnMode.AtCaster || (evt.spawnMode == VFXSpawnMode.AtTarget && i == 0))
                    {
                        Vector3 pos = GetVFXPosition(skill, evt, actorView, targets);
                        InstantiateVFX(evt, pos, actorView.transform);
                    }
                }
                else
                {
                    // Hit 1+: spawn AtTarget tương ứng với hitIndex
                    if (evt.spawnMode == VFXSpawnMode.AtTarget && i == hitIndex)
                    {
                        Vector3 pos = GetVFXPosition(skill, evt, actorView, targets);
                        InstantiateVFX(evt, pos, actorView.transform);
                    }
                }
            }
        }
        if (skill.rangedVfxEvents != null && hitIndex == 0)
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

    private Vector3 GetVFXPosition(SkillData skill, VFXEvent evt, UnitView actorView, List<CombatUnit> targets)
    {
        switch (evt.spawnMode)
        {
            case VFXSpawnMode.AtCaster:
                return actorView.transform.position + evt.offset;
            case VFXSpawnMode.AtTarget:
                if (targets == null || !targets.Any())
                    return actorView.transform.position + evt.offset;
                var targetView = GetViewForUnit(targets.First());
                return targetView != null ? targetView.transform.position + evt.offset : actorView.transform.position + evt.offset;
            default:
                return actorView.transform.position + evt.offset;
        }
    }

    private void InstantiateVFX(VFXEvent evt, Vector3 position, Transform potentialParent)
    {
        if (evt.vfxPrefab == null) return;
        var vfx = Instantiate(evt.vfxPrefab, position, Quaternion.identity);
        if (evt.attachToCaster && potentialParent != null)
            vfx.transform.SetParent(potentialParent);
        var visualEffect = vfx.GetComponent<UnityEngine.VFX.VisualEffect>();
        if (visualEffect != null) visualEffect.Play();
        Destroy(vfx, 2f);
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

    private IEnumerator ReturnPhase(UnitView actorView, Vector3 originPosition, ActionResult result)
    {
        yield return StartCoroutine(MoveCoroutine(actorView, originPosition, returnDuration));
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
}