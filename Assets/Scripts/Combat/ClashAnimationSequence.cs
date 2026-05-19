using System;
using System.Collections;
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

    private void Awake()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
    }

    // Phương thức chính để chạy animation cho một hành động
    public IEnumerator PlayAction(ActionResult result)
    {
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
        yield return StartCoroutine(MoveCoroutine(actorView, attackPosition, moveToTargetDuration));

        // --- Phase 2: Thực hiện Skill Animation và xử lý từng Hit ---
        var skill = result.Skill;
        float animLength = 0.5f; // Thời gian animation mặc định

        if (skill != null && !string.IsNullOrEmpty(skill.animationTrigger))
        {
            actorView.SetAnimationTrigger(skill.animationTrigger);
            animLength = actorView.GetClipLength(skill.animationTrigger);
        }

        // >>> LOGIC MỚI: CHIA NHỎ SÁT THƯƠNG VÀ HIỆU ỨNG THEO TỪNG HIT <<<
        int hitCount = (skill != null && skill.hitCount > 0) ? skill.hitCount : 1;
        float timePerHit = animLength / hitCount;

        // Áp dụng từng outcome (cho từng mục tiêu)
        foreach (var outcome in result.Outcomes)
        {
            var targetView = GetViewForUnit(outcome.Target);
            if (targetView == null) continue;

            // Chia nhỏ sát thương cho mỗi hit
            int damagePerHit = outcome.Damage / hitCount;
            int remainingDamage = outcome.Damage % hitCount; // Sát thương còn lại nếu không chia hết

            for (int i = 0; i < hitCount; i++)
            {
                // Đợi nửa thời gian để hiệu ứng xảy ra giữa animation
                yield return new WaitForSeconds(timePerHit / 2f);

                // Camera effect và sát thương xảy ra gần như cùng lúc
                if (cameraManager != null) cameraManager.PlayImpactShake();
                
                int currentHitDamage = damagePerHit;
                if (i == hitCount - 1)
                {
                    currentHitDamage += remainingDamage;
                }

                if (currentHitDamage > 0)
                {
                    outcome.Target.TakeDamage(currentHitDamage);
                    targetView.SetAnimationTrigger("Hurt");
                }

                // Đợi nốt nửa thời gian còn lại
                yield return new WaitForSeconds(timePerHit / 2f);
            }
        }

        // Đảm bảo tất cả các hiệu ứng khác (không phải sát thương) được áp dụng
        result.ApplyNonDamageOutcomes();

        yield return new WaitForSeconds(postSkillWait);

        // --- Phase 3: Quay về vị trí cũ ---
        actorView.SetAnimationTrigger("Idle");
        yield return StartCoroutine(MoveCoroutine(actorView, actorOrigin, returnDuration));
        actorView.SetAnimationTrigger("Idle");

        // --- Camera Phase 2: Reset về chế độ xem toàn cảnh ---
        if (cameraManager != null)
        {
            cameraManager.ResetCamera();
            yield return new WaitForSeconds(cameraManager.zoomOutDuration); // Đợi camera reset xong
        }
    }

    private IEnumerator MoveCoroutine(UnitView unitView, Vector3 targetPos, float duration)
    {
        Vector3 startPos = unitView.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            unitView.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        unitView.transform.position = targetPos;
    }

    private UnitView GetViewForUnit(CombatUnit unit)
    {
        if (unit == null) return null;
        return FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LinkedUnit == unit);
    }
}