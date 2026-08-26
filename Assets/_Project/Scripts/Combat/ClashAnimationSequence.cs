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

        // Flush bất kỳ hit nào còn lại (không nhất thiết chỉ khi _lastHitCounter == 0).
        // Với skill nhiều hit, nếu clip chỉ phát ít hơn hitCount lần OnHit(int) thì các hit
        // còn lại rơi vào đây → FlushPendingOutcomes vừa áp damage vừa bổ sung VFX còn thiếu
        // (SpawnRemainingVFX) để skill luôn spawn ĐỦ hitCount VFX theo đúng thứ tự.
        // ❌ SFX fallback đã bỏ: SFX giờ được spawn cùng VFX qua PlayHitVFXSequence.
        if (actorView != null)
        {
            actorView.FlushPendingOutcomes();
        }

        // Cleanup: xóa handler để tránh skill cũ dùng SFX của skill mới
        actorView.ClearAnimationEventHandlers();

        if (shouldMove)
            yield return StartCoroutine(ReturnPhase(actorView, actorOrigin, result));

        yield return StartCoroutine(CleanupPhase(result));
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

    /// <summary>
    /// Kiểm tra skill có phải đa mục tiêu (AOE) hay không.
    /// AOE = targetType AllEnemies/AllAllies HOẶC danh sách target có nhiều hơn 1.
    /// </summary>
    private bool IsAOESkill(SkillData skill, List<CombatUnit> targets)
    {
        if (skill != null)
        {
            if (skill.targetType == TargetType.AllEnemies || skill.targetType == TargetType.AllAllies)
                return true;
        }
        return targets != null && targets.Count > 1;
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
            bool isAOE = IsAOESkill(result.Skill, result.InitialTargets);
            bool isBeam = result.Skill != null && result.Skill.isBeam;
            if (isAOE)
            {
                // ── CAMERA ĐA MỤC TIÊU (AOE) ──
                // Center = tâm khung bao của TOÀN BỘ nhóm target trúng chiêu.
                // Zoom out dần, giữ nhóm target làm trung tâm.
                var aoeTargetViews = result.InitialTargets
                    .Select(t => GetViewForUnit(t))
                    .Where(v => v != null)
                    .Distinct()
                    .ToList();

                cameraManager.FocusAOEAction(
                    aoeTargetViews,
                    cameraManager.damageZoomSize,
                    cameraManager.clashZoomSize * 0.5f);
                // 🔥 THAY ĐỔI: Chờ 0.5s để camera zoom out hoàn tất (thay vì zoomInDuration 0.15s)
                yield return new WaitForSeconds(0.5f);

                // Beam: bắt đầu rung liên tục + mạnh dần theo thời gian
                if (isBeam)
                {
                    cameraManager.StartBeamShake(
                        result.Skill.beamShakeBaseIntensity,
                        result.Skill.beamShakeStepIntensity,
                        result.Skill.beamShakeDuration,
                        result.Skill.beamShakeFrequency);
                }
            }
            else if (isMoving)
            {
                // ── CAMERA ĐƠN MỤC TIÊU (melee) ──
                cameraManager.ZoomToUnit(actorView.transform, cameraManager.clashZoomSize);
                yield return new WaitForSeconds(cameraManager.zoomInDuration);
            }
            else
            {
                // ── CAMERA ĐƠN MỤC TIÊU (không di chuyển) ──
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

        // 🔥 Tính lại shouldMove — không dùng trực tiếp từ PlayAction (ngoài scope)
        bool shouldMove = ShouldCharacterMove(result.Actor, result.Skill);

        actorView.SetCurrentSkill(skill);
        if (targets.Any())
            actorView.SetCurrentTarget(targets.First());

        // CAMERA: reset buoc rung progressive ve 0 moi skill moi.
        if (cameraManager != null)
            cameraManager.ResetHitShakeProgress();


// Hit Handler - chỉ SFX + shake + hurt.
            // VFX được spawn bởi UnitView.PlayHitVFXSequence (rải đều theo thời lượng animation)
        // — KHÔNG spawn hết ở đây, tránh trùng lặp và spawn sai thứ tự.
        bool isAOE = IsAOESkill(skill, targets);
        _lastHitCounter = 0;
        Action onHitHandler = () => {
            int currentHit = _lastHitCounter++;
            // SFX được phát bởi UnitView.PlayHitVFXSequence (đồng bộ với VFX) — KHÔNG phát ở đây
            // để tránh SFX chạy 2 lần.
            // AOE: mỗi hit → camera zoom ra xa thêm (giữ center = tâm nhóm target)
            if (isAOE && cameraManager != null)
                cameraManager.AdvanceAOEZoom();
            // Beam: mỗi hit → rung mạnh dần theo thời gian (nếu skill đang rung liên tục)
            if (skill != null && skill.isBeam && cameraManager != null)
                cameraManager.AdvanceBeamShake();
            foreach (var outcome in result.Outcomes)
            {
                var targetView = GetViewForUnit(outcome.Target);
                if (targetView == null) continue;
                // CAMERA: mỗi onhit rung 1 cú (ngắn), càng về sau càng mạnh (progressive).
                // Beam dùng AdvanceBeamShake riêng (rung liên tục tăng dần).
                if (cameraManager != null && !isAOE && !(skill != null && skill.isBeam))
                    cameraManager.PlayProgressiveHitShake();
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

        float animLength;
        if (!string.IsNullOrEmpty(skill.animationTrigger))
        {
            // 🔥 SỬA LẠI: Dùng SetAnimationTrigger CHO CẢ 2 (như ban đầu) — animation luôn chạy.
            // Lỗi trước: PlayAnimation(skill.animationTrigger) dùng trigger name như state name
            // → Animator không tìm thấy state → animation không chạy.
            actorView.SetAnimationTrigger(skill.animationTrigger);
            animLength = actorView.GetClipLength(skill.animationTrigger);
        }
        else
        {
            actorView.SetAnimationTrigger(AnimationConstants.Attack);
            animLength = actorView.GetClipLength(AnimationConstants.Attack);
        }

        // 🔥 PHÂN LOẠI RIÊNG TỪNG LOẠI SKILL:
        // - Skill DI CHUYỂN (shouldMove=true): Dựa hoàn toàn vào animation events
        //   (OnSpawnVFX / OnHit) — KHÔNG gọi PlaySkillEffects để tránh VFX spawn ngay
        //   khi skill vừa kích hoạt trước khi nhân vật lao tới mục tiêu.
        // - Skill ĐỨNG YÊN (shouldMove=false): Dùng PlaySkillEffects fallback an toàn
        //   (chờ 1 frame) để đảm bảo VFX luôn có hiệu ứng kể cả khi clip thiếu events.
        if (!shouldMove)
        {
            actorView.PlaySkillEffects(animLength);
        }

        return animLength;
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

    private IEnumerator CleanupPhase(ActionResult result)
    {
        foreach (var view in allUnitViews)
            if (view != null) view.PlayAnimation(AnimationConstants.Idle);
        SetAllUnitAlphas(1.0f);
        if (cameraManager != null)
        {
            cameraManager.StopBeamShake(); // Dừng rung beam (nếu có)
            cameraManager.EndAOEFocus(); // Kết thúc chế độ camera AOE
            cameraManager.ResetCamera();
            yield return new WaitForSeconds(cameraManager.zoomOutDuration);
        }
    }
}