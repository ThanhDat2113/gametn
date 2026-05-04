using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Điều phối toàn bộ chuỗi animation khi Clash xảy ra.
///
/// Thứ tự:
///   1. Cả 2 nhân vật lao vào nhau (Rush)
///   2. ClashVisualController hiện xúc xắc + kết quả
///   3. Bên thua play KnockBack
///   4. Bên thắng play Skill animation (damage được xử lý qua Animation Event)
///   5. Cả 2 trở về vị trí gốc (Idle)
/// </summary>
public class ClashAnimationSequence : MonoBehaviour
{
    [Header("References")]
    public ClashVisualController clashVisual;
    
    [Tooltip("CombatCameraManager để trigger camera effects")]
    public CombatCameraManager cameraManager;

    [Header("Timing")]
    [Tooltip("Thời gian 2 nhân vật lao vào nhau (giây)")]
    public float rushDuration = 0.5f;

    [Tooltip("Khoảng cách dừng lại khi đứng đối mặt (world units)")]
    public float faceOffDistance = 2.0f;

    [Tooltip("Thời gian chờ sau skill animation trước khi về vị trí")]
    public float postSkillWait = 0.1f;

    [Tooltip("Thời gian chờ sau KnockBack trước khi bắt đầu Skill animation")]
    public float postKnockbackWait = 0.2f;

    [Tooltip("Thời gian di chuyển về vị trí gốc")]
    public float returnDuration = 0.4f;

    [Header("Clash Effects")]
    [Tooltip("Kích thước camera khi zoom vào gần nhất")]
    public float clashZoomSize = 5f;
    [Tooltip("Khoảng cách đẩy lùi của người thua")]
    public float knockbackDistance = 1.5f;
    [Tooltip("Khoảng cách đẩy lùi nhẹ cho đòn đánh thường")]
    public float lightKnockbackDistance = 0.5f;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Auto find camera manager nếu chưa assign
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Chơi toàn bộ chuỗi clash animation.
    /// </summary>
    /// <param name="playerView">UnitView của player</param>
    /// <param name="enemyView">UnitView của enemy</param>
    /// <param name="result">Kết quả clash (chứa Winner, Loser, WinnerSkill, LoserSkill…)</param>
    /// <param name="onComplete">Callback khi chuỗi kết thúc</param>
    public IEnumerator PlayFullClashSequence(
        UnitView playerView,
        UnitView enemyView,
        ClashResult result,
        Action onComplete = null)
    {
        // Xác định winner/loser view
        bool playerWon = result.Winner == playerView.LinkedUnit;
        var winnerView = playerWon ? playerView : enemyView;
        var loserView = playerWon ? enemyView : playerView;

        // Lưu vị trí gốc
        Vector3 playerOrigin = playerView.transform.position;
        Vector3 enemyOrigin = enemyView.transform.position;

        // Cache a camera reference
        var cam = cameraManager;
        float startOrthoSize = cam != null ? cam.defaultOrthoSize : 15f;

        // ── Phase 1: Rush vào nhau ────────────────────────────────────────
        playerView.SetAnimationTrigger("Rush");
        enemyView.SetAnimationTrigger("Rush");

        // Fade out các unit không liên quan (giảm alpha xuống 20% - tức giảm 80%)
        var allUnits = FindObjectsByType<UnitView>(FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            if (unit != playerView && unit != enemyView)
            {
                StartCoroutine(FadeUnit(unit, 0.2f, 0.3f)); // Fade to 20% alpha in 0.3s
            }
        }

        // Camera effect: Zoom vào clash point
        if (cameraManager != null)
            cameraManager.PlayClashZoom(playerView.transform, enemyView.transform);

        // Tính midpoint giữa 2 nhân vật
        Vector3 mid = (playerOrigin + enemyOrigin) * 0.5f;
        Vector3 playerDir = (mid - playerOrigin).normalized;
        Vector3 enemyDir = (mid - enemyOrigin).normalized;

        Vector3 playerTarget = mid - playerDir * faceOffDistance * 0.5f;
        Vector3 enemyTarget = mid - enemyDir * faceOffDistance * 0.5f;

        float elapsed = 0f;
        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rushDuration);
            playerView.transform.position = Vector3.Lerp(playerOrigin, playerTarget, t);
            enemyView.transform.position = Vector3.Lerp(enemyOrigin, enemyTarget, t);

            // Dynamically adjust camera
            if (cam != null)
            {
                float distance = Vector3.Distance(playerView.transform.position, enemyView.transform.position);
                float zoomT = 1f - Mathf.Clamp01((distance - faceOffDistance) / (Vector3.Distance(playerOrigin, enemyOrigin) - faceOffDistance));
                
                // Dịch camera lên một chút và zoom ra nhẹ (7f)
                float newSize = Mathf.Lerp(startOrthoSize, 7f, zoomT);
                
                Vector3 midpoint = (playerView.transform.position + enemyView.transform.position) * 0.5f;
                Vector3 clashCameraPosition = midpoint + new Vector3(0, 1.5f, 0);
                cam.SetCameraPositionAndSize(clashCameraPosition, newSize);
            }

            yield return null;
        }

        playerView.transform.position = playerTarget;
        enemyView.transform.position = enemyTarget;

        // Chuyển sang ClashIdle (2 bên đứng đối mặt)
        playerView.SetAnimationTrigger("ClashIdle");
        enemyView.SetAnimationTrigger("ClashIdle");

        // ── Phase 2: Hiện ClashVisualController (xúc xắc + kết quả) ──────
        if (clashVisual != null)
        {
            Coroutine shakeCoroutine = StartCoroutine(ContinuousShakeCoroutine(2.0f)); // Hardcoded duration
            yield return StartCoroutine(
                clashVisual.PlayClashSequence(result.VisualData, null));
            StopCoroutine(shakeCoroutine);
        }
        else
        {
            yield return new WaitForSeconds(0.8f);
        }

        // Phase 3: Bên thua KnockBack ───────────────────────────────────
        if (cameraManager != null) cameraManager.PlayImpactShake(); // Shake on clash result
        loserView.SetAnimationTrigger("KnockBack");

        // Move loser back
        Vector3 knockbackDir = (loserView.transform.position - winnerView.transform.position).normalized;
        Vector3 knockbackTarget = loserView.transform.position + knockbackDir * knockbackDistance;
        
        float knockbackAnimLength = loserView.GetClipLength("KnockBack");
        if (knockbackAnimLength <= 0f) knockbackAnimLength = 0.6f;

        // Move during the knockback animation
        float kbElapsed = 0f;
        Vector3 loserStartPos = loserView.transform.position;
        while (kbElapsed < knockbackAnimLength)
        {
            kbElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(kbElapsed / knockbackAnimLength);
            loserView.transform.position = Vector3.Lerp(loserStartPos, knockbackTarget, t);
            yield return null;
        }
        
        yield return new WaitForSeconds(postKnockbackWait);

        // ── Phase 4: Bên thắng play Skill animation ───────────────────────
        // Move winner to loser
        Vector3 attackPos = loserView.transform.position - (loserView.transform.position - winnerView.transform.position).normalized * faceOffDistance;
        float moveElapsed = 0f;
        float moveDuration = 0.2f;
        Vector3 winnerStartPos = winnerView.transform.position;
        while (moveElapsed < moveDuration)
        {
            moveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(moveElapsed / moveDuration);
            winnerView.transform.position = Vector3.Lerp(winnerStartPos, attackPos, t);
            yield return null;
        }
        winnerView.transform.position = attackPos;

        // Damage/VFX được xử lý bên trong qua Animation Events trên winner
        var winnerSkill = result.WinnerSkill;

        if (winnerSkill != null && !string.IsNullOrEmpty(winnerSkill.animationTrigger))
        {
            winnerView.SetCurrentSkill(winnerSkill);

            // Chuẩn bị hit data cho loser
            var hits = BuildHitsForLoser(result);
            winnerView.SetPendingHits(hits, loserView.LinkedUnit);

            winnerView.SetAnimationTrigger(winnerSkill.animationTrigger);

            // Chờ clip chạy hết hoàn toàn rồi mới tiếp tục
            yield return StartCoroutine(
                winnerView.WaitUntilAnimationDone(winnerSkill.animationTrigger));

            winnerView.ClearPendingHits();
        }
        else
        {
            // Fallback: áp damage thẳng nếu không có animation
            var hits = BuildHitsForLoser(result);
            foreach (var hit in hits)
                result.Loser.TakeDamage(result.Winner, hit.Damage, hit.HitIndex);

            // Camera impact effect for fallback
            if (cameraManager != null)
            {
                cameraManager.PlayFinalHitShake();
            }

            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(postSkillWait);

        // ── Phase 5: Cả 2 trở về vị trí gốc ─────────────────────────────
        playerView.SetAnimationTrigger("Idle");
        enemyView.SetAnimationTrigger("Idle");

        // Camera will be reset manually or by other systems

        elapsed = 0f;
        Vector3 playerCurrent = playerView.transform.position;
        Vector3 enemyCurrent = enemyView.transform.position;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            playerView.transform.position = Vector3.Lerp(playerCurrent, playerOrigin, t);
            enemyView.transform.position = Vector3.Lerp(enemyCurrent, enemyOrigin, t);
            yield return null;
        }

        playerView.transform.position = playerOrigin;
        enemyView.transform.position = enemyOrigin;

        // Fade back tất cả units về alpha = 1.0
        var allUnitsEnd = FindObjectsByType<UnitView>(FindObjectsSortMode.None);
        foreach (var unit in allUnitsEnd)
        {
            if (unit != playerView && unit != enemyView)
            {
                StartCoroutine(FadeUnit(unit, 1.0f, 0.3f)); // Fade back to full alpha
            }
        }

        onComplete?.Invoke();
    }

    private IEnumerator ContinuousShakeCoroutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (cameraManager != null)
            {
                cameraManager.PlayImpactShake();
            }
            yield return new WaitForSeconds(0.1f); // Adjust this value to change shake frequency
            elapsed += 0.1f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helper: Fade unit alpha
    private IEnumerator FadeUnit(UnitView unitView, float targetAlpha, float duration)
    {
        var spriteRenderer = unitView.spriteRenderer;
        if (spriteRenderer == null) yield break;

        Color startColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(startColor.a, targetAlpha, t);
            spriteRenderer.color = newColor;
            yield return null;
        }

        Color finalColor = spriteRenderer.color;
        finalColor.a = targetAlpha;
        spriteRenderer.color = finalColor;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helper: Tính danh sách hit cho loser dựa trên ClashResult
    private System.Collections.Generic.List<HitData> BuildHitsForLoser(ClashResult result)
    {
        var hits = new System.Collections.Generic.List<HitData>();
        var skill = result.WinnerSkill;
        int hitCount = skill != null ? Mathf.Max(1, skill.hitCount) : 1;

        int raw = Mathf.RoundToInt(result.Winner.ATK
                       * result.Winner.GetBuffMultiplier(StatType.ATK));
        int defend = result.Loser.PDEF;
        int totalDmg = Mathf.Max(hitCount, raw - defend);

        for (int i = 0; i < hitCount; i++)
        {
            int dmg = (i == hitCount - 1)
                ? totalDmg - (totalDmg / hitCount) * (hitCount - 1)
                : totalDmg / hitCount;

            hits.Add(new HitData { Damage = dmg, HitIndex = i });
        }

        return hits;
    }
}