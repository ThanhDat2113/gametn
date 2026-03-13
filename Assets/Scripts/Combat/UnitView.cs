using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public HitEventReceiver hitReceiver;

    // ── Public ────────────────────────────────────────────────
    public CombatUnit LinkedUnit { get; private set; }

    // ── Runtime data ──────────────────────────────────────────
    private CombatUnit currentTarget;
    private List<HitData> pendingHits = new();
    private SkillData currentSkill;
    private CombatCameraManager cameraManager;

    // ─────────────────────────────────────────────────────────
    public void Setup(CombatUnit unit)
    {
        LinkedUnit = unit;

        if (unit.Data.battleSprite != null)
            spriteRenderer.sprite = unit.Data.battleSprite;

        // Enemy nhìn sang trái
        spriteRenderer.flipX = !unit.IsPlayer;

        // Find camera manager
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();

        // Lắng nghe events từ CombatUnit
        unit.OnDamageTaken += (dmg, hitIndex) => 
        {
            TriggerHitFlash();
            // Camera effect: Zoom vào unit bị damage
            if (cameraManager != null)
            {
                cameraManager.ZoomToUnit(transform, cameraManager.damageZoomSize);
                cameraManager.PlayImpactShake();
            }
        };
        unit.OnDied += () => StartCoroutine(DeathFade());

        // Lắng nghe Animation Events
        if (hitReceiver != null)
        {
            hitReceiver.OnHitFrame += ProcessHitAtFrame;
            hitReceiver.OnVFXFrame += ProcessVFXAtFrame;
        }
    }

    private void OnDestroy()
    {
        if (hitReceiver != null)
        {
            hitReceiver.OnHitFrame -= ProcessHitAtFrame;
            hitReceiver.OnVFXFrame -= ProcessVFXAtFrame;
        }
    }

    // ── Set data trước khi animation chạy ────────────────────
    public void SetCurrentSkill(SkillData skill)
    {
        currentSkill = skill;
    }

    public void SetPendingHits(List<HitData> hits, CombatUnit target)
    {
        pendingHits = new List<HitData>(hits);
        currentTarget = target;
    }

    public void ClearPendingHits()
    {
        pendingHits.Clear();
        currentTarget = null;
        currentSkill = null;
    }

    // ── Animation Trigger ─────────────────────────────────────
    public void SetAnimationTrigger(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);
    }

    // ── Chờ animation clip chạy xong hoàn toàn ─────────────
    // Cơ chế:
    //   1. Lưu hash của state HIỆN TẠI (trước khi trigger)
    //   2. Polling đến khi Animator chuyển sang state KHÁC (state mới)
    //   3. Đọc length của state mới, chờ hết
    //   Timeout 4s phòng trường hợp Animator không chuyển state
    public IEnumerator WaitUntilAnimationDone(string triggerName)
    {
        if (animator == null) yield break;

        // Lưu hash state hiện tại trước khi trigger
        int prevStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;

        // Bước 1: chờ Animator chuyển sang state MỚI (khác state cũ)
        float waitTimeout = 0.5f;
        float waited = 0f;
        while (waited < waitTimeout)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.fullPathHash != prevStateHash)
                break;
            waited += Time.deltaTime;
            yield return null;
        }

        if (waited >= waitTimeout)
        {
            Debug.LogWarning($"[UnitView] '{triggerName}': Animator không đổi state " +
                             $"sau {waitTimeout}s. Trigger có đúng tên không?");
            yield break;
        }

        // Bước 2: đọc length của state mới, chờ hết
        var newStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = newStateInfo.length;

        // Tính thời gian đã chạy rồi, chờ phần còn lại
        float alreadyElapsed = newStateInfo.normalizedTime * clipLength;
        float remaining = Mathf.Max(0f, clipLength - alreadyElapsed);

        Debug.Log($"[UnitView] '{triggerName}' clipLength={clipLength:F2}s " +
                  $"remaining={remaining:F2}s");

        yield return new WaitForSeconds(remaining);
    }

    // ── Được gọi từ Animation Event — OnHit ──────────────────
    private void ProcessHitAtFrame(int hitIndex)
    {
        if (currentTarget == null)
        {
            Debug.LogWarning($"[UnitView] ProcessHitAtFrame: currentTarget is null");
            return;
        }

        if (hitIndex >= pendingHits.Count)
        {
            Debug.LogWarning($"[UnitView] hitIndex {hitIndex} vượt quá " +
                             $"pendingHits.Count {pendingHits.Count}");
            return;
        }

        var hit = pendingHits[hitIndex];
        currentTarget.TakeDamage(hit.Damage, hitIndex);

        // Shake mạnh hơn cho hit cuối cùng
        bool isFinalHit = (hitIndex == pendingHits.Count - 1);
        if (isFinalHit && cameraManager != null)
        {
            cameraManager.PlayFinalImpactShake();
        }

        Debug.Log($"[Hit {hitIndex + 1}/{pendingHits.Count}] " +
                  $"{LinkedUnit.UnitName} → {currentTarget.UnitName}: {hit.Damage} dmg");
    }

    // ── Được gọi từ Animation Event — OnSpawnVFX ─────────────
    private void ProcessVFXAtFrame(int vfxIndex)
    {
        if (currentSkill?.vfxPrefab == null) return;
        if (currentTarget == null) return;

        Vector3 spawnPos = Vector3.zero;

        var targetView = FindObjectsByType<UnitView>(FindObjectsSortMode.None)
            .FirstOrDefault(v => v.LinkedUnit == currentTarget);

        if (targetView != null)
            spawnPos = targetView.transform.position;

        spawnPos += Vector3.up * currentSkill.vfxOffset;

        var vfx = Instantiate(currentSkill.vfxPrefab, spawnPos, Quaternion.identity);
        Destroy(vfx, 2f);

        Debug.Log($"[VFX] Spawn {currentSkill.vfxPrefab.name} tại {spawnPos}");
    }

    // ── Helper: lấy độ dài clip ───────────────────────────────
    public float GetClipLength(string clipName)
    {
        if (animator?.runtimeAnimatorController == null) return 0.5f;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;

        return 0.5f;
    }

    // ── Hit Flash ─────────────────────────────────────────────
    public void TriggerHitFlash()
    {
        StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = Color.white;
    }

    // ── Death ─────────────────────────────────────────────────
    private IEnumerator DeathFade()
    {
        SetAnimationTrigger("Die");

        float elapsed = 0f;
        float duration = 0.6f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}