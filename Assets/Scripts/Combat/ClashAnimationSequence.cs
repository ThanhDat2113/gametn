using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Tên tệp vẫn là ClashAnimationSequence.cs, nhưng logic bên trong đã được viết lại
// để xử lý animation hành động turn-based đơn giản.
public class ClashAnimationSequence : MonoBehaviour
{
    [Header("References")]
    public CombatCameraManager cameraManager;

    [Header("Timing")]
    public float moveToTargetDuration = 0.3f;
    public float returnDuration = 0.4f;
    public float postSkillWait = 0.2f;

    [Header("Effects")]
    public float faceOffDistance = 4.0f; // TĂNG KHOẢNG CÁCH ĐỂ KHÔNG ĐỨNG QUÁ SÁT
    [Range(0, 1)]
    public float dimAlpha = 0.5f; // Mức độ mờ (0: trong suốt, 1: rõ nét)

    private void Awake()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
    }

    // Phương thức chính để chạy animation cho một hành động
    public IEnumerator PlayAction(ActionResult result)
    {
        var actor = result.Actor;
        var targets = result.Outcomes.Select(o => o.Target).ToList();

        var actorView = GetViewForUnit(result.Actor);
        var primaryTargetView = GetViewForUnit(result.InitialTargets.FirstOrDefault());

        if (actorView == null || primaryTargetView == null)
        {
            Debug.LogWarning("[ActionAnimation] Actor hoặc Target View không tồn tại. Bỏ qua animation.");
            result.ApplyOutcomes(); // Vẫn áp dụng kết quả dù không có animation
            yield break;
        }

        // --- Camera Phase 1: Zoom và follow người tấn công ---
        if (cameraManager != null)
        {
            cameraManager.ZoomToUnit(actorView.transform, cameraManager.clashZoomSize);
            yield return new WaitForSeconds(cameraManager.zoomInDuration); // Đợi camera zoom xong
        }

        // Lưu vị trí gốc
        Vector3 actorOrigin = actorView.transform.position;

        // --- Phase 1: Di chuyển đến mục tiêu ---
        Vector3 targetPosition = primaryTargetView.transform.position;
        Vector3 direction = (targetPosition - actorOrigin).normalized;
        Vector3 attackPosition = targetPosition - direction * faceOffDistance;

        actorView.SetAnimationTrigger("Rush");
        yield return StartCoroutine(MoveAndDimCoroutine(actorView, attackPosition, moveToTargetDuration, targets, true));

        // --- Phase 2: Thực hiện Skill Animation và xử lý từng Hit ---
        var skill = result.Skill;
        if (skill == null) {
            // Nếu không có skill, vẫn thực hiện 1 hit cơ bản
            result.ApplyOutcomes();
            yield break;
        }

        // >>> LOGIC MỚI: SỬ DỤNG ANIMATION EVENTS <<<
        int hitCount = skill.hitCount > 0 ? skill.hitCount : 1;
        bool animationFinished = false;

        // Hàm xử lý khi nhận được event OnHit
        Action onHitHandler = () => {
            // Áp dụng sát thương và hiệu ứng cho TẤT CẢ các mục tiêu mỗi khi có hit
            foreach (var outcome in result.Outcomes)
            {
                var targetView = GetViewForUnit(outcome.Target);
                if (targetView == null) continue;

                // Camera effect
                if (cameraManager != null) cameraManager.PlayImpactShake();

                // Chia đều sát thương cho mỗi hit
                int damagePerHit = outcome.Damage / hitCount;
                if (damagePerHit > 0) {
                    outcome.Target.TakeDamage(damagePerHit);
                    targetView.SetAnimationTrigger("Hurt");
                }
            }
        };

        // Đăng ký lắng nghe sự kiện OnHit từ actor
        actorView.OnHitAnimationEvent += onHitHandler;

        // Kích hoạt animation
        if (!string.IsNullOrEmpty(skill.animationTrigger))
        {
            actorView.SetAnimationTrigger(skill.animationTrigger);
            float animLength = actorView.GetClipLength(skill.animationTrigger);
            // Dùng coroutine để biết khi nào animation kết thúc
            StartCoroutine(WaitForAnimationToEnd(animLength, () => animationFinished = true));
        } else {
            animationFinished = true; // Không có animation, coi như xong luôn
        }

        // Chờ cho đến khi animation kết thúc
        yield return new WaitUntil(() => animationFinished);

        // Hủy đăng ký sự kiện để tránh lỗi
        actorView.OnHitAnimationEvent -= onHitHandler;

        // Xử lý phần sát thương còn lại (do chia không hết) và các hiệu ứng khác
        foreach(var outcome in result.Outcomes) {
            int remainingDamage = outcome.Damage % hitCount;
            if (remainingDamage > 0) {
                outcome.Target.TakeDamage(remainingDamage);
            }
        }
        result.ApplyNonDamageOutcomes();

        yield return new WaitForSeconds(postSkillWait);

        // --- Phase 3: Quay về vị trí cũ ---
        actorView.SetAnimationTrigger("Idle");
        yield return StartCoroutine(MoveAndDimCoroutine(actorView, actorOrigin, returnDuration, targets, false));
        actorView.SetAnimationTrigger("Idle");

        // --- Khôi phục màu sắc NGAY LẬP TỨC ---
        ResetAllUnitColors();

        // --- Camera Phase 2: Reset về chế độ xem toàn cảnh ---
        if (cameraManager != null)
        {
            cameraManager.ResetCamera();
            yield return new WaitForSeconds(cameraManager.zoomOutDuration); // Đợi camera reset xong
        }
    }

    private IEnumerator MoveAndDimCoroutine(UnitView actorView, Vector3 targetPos, float duration, List<CombatUnit> targets, bool isMovingForward)
    {
        Vector3 startPos = actorView.transform.position;
        float elapsed = 0f;

        // Lấy danh sách các unit không liên quan
        var allUnitViews = FindObjectsByType<UnitView>(FindObjectsSortMode.None);
        var involvedUnits = new HashSet<CombatUnit>(targets);
        involvedUnits.Add(actorView.LinkedUnit);
        var uninvolvedViews = allUnitViews.Where(v => v.LinkedUnit != null && !involvedUnits.Contains(v.LinkedUnit)).ToList();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Di chuyển actor
            actorView.transform.position = Vector3.Lerp(startPos, targetPos, t);

            // Tính toán và áp dụng độ mờ
            float currentAlpha;
            if (isMovingForward)
            {
                // Mờ dần đi khi lao tới
                currentAlpha = Mathf.Lerp(1f, dimAlpha, t);
            }
            else
            {
                // Rõ dần lại khi lùi về
                currentAlpha = Mathf.Lerp(dimAlpha, 1f, t);
            }

            foreach (var view in uninvolvedViews)
            {
                view.SetAlpha(currentAlpha);
            }

            yield return null;
        }

        // Đảm bảo vị trí và độ mờ cuối cùng chính xác
        actorView.transform.position = targetPos;
        float finalAlpha = isMovingForward ? dimAlpha : 1f;
        foreach (var view in uninvolvedViews)
        {
            view.SetAlpha(finalAlpha);
        }
    }

    private IEnumerator WaitForAnimationToEnd(float duration, Action onEnd)
    {
        yield return new WaitForSeconds(duration);
        onEnd?.Invoke();
    }

    private UnitView GetViewForUnit(CombatUnit unit)
    {
        if (unit == null) return null;
        return FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LinkedUnit == unit);
    }

    private void ResetAllUnitColors()
    {
        var allUnitViews = FindObjectsByType<UnitView>(FindObjectsSortMode.None);
        foreach (var unitView in allUnitViews)
        {
            unitView.SetAlpha(1f);
        }
    }
}