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
    public UnityEngine.UI.Slider healthBar;

    // Event cho Animation-driven hits
    public event System.Action OnHitAnimationEvent;

    // ── Public ────────────────────────────────────────────────
    public CombatUnit LinkedUnit { get; private set; }

    // ── Runtime data ──────────────────────────────────────────
    private CombatUnit currentTarget;
    private List<HitData> pendingHits = new();
    private SkillData currentSkill;
    private CombatCameraManager cameraManager;
    private ClashAnimationSequence clashSequence;
    private Vector3 originalPosition;
    private bool originalPositionHasBeenSet = false;

    // ─────────────────────────────────────────────────────────

    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }

    public void StoreOriginalPosition(Vector3 position)
    {
        originalPosition = position;
        originalPositionHasBeenSet = true;
    }

    public void StoreOriginalPosition()
    {
        StoreOriginalPosition(transform.position);
    }

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

        if (clashSequence == null)
            clashSequence = FindFirstObjectByType<ClashAnimationSequence>();

        // Lắng nghe events từ CombatUnit
        unit.OnDamageTaken += (caster, dmg, hitIndex) => 
        {
            // Kích hoạt animation bị đánh
            SetAnimationTrigger("Knockback");

            TriggerHitFlash();
            UpdateHealthBar();
            // Camera effect: Zoom vào unit bị damage
            if (cameraManager != null)
            {
                cameraManager.ZoomToUnit(transform, cameraManager.damageZoomSize);

                // Nếu là player tấn công, dùng hiệu ứng đặc biệt
                if (caster != null && caster.IsPlayer)
                {
                    var targetView = FindObjectsByType<UnitView>(FindObjectsSortMode.None)
                        .FirstOrDefault(v => v.LinkedUnit == this.LinkedUnit);
                    if(targetView != null)
                        cameraManager.PlayPlayerImpactEffect(targetView.transform);
                }
                else
                {
                    cameraManager.PlayImpactShake();
                }
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

    public void UpdateHealthBar()
    {
        if (healthBar != null && LinkedUnit != null)
        {
            healthBar.value = (float)LinkedUnit.CurrentHP / LinkedUnit.MaxHP;
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

    public void ForcePlayAnimationState(string stateName)
    {
        if (animator != null)
        {
            animator.Play(stateName);
        }
    }

    public void ResetAnimationTrigger(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.ResetTrigger(triggerName);
        }
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

    public void OnHit()
    {
        OnHitAnimationEvent?.Invoke();
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
        bool isFinalHit = (hitIndex == pendingHits.Count - 1);

        // Gây sát thương
        currentTarget.TakeDamage(LinkedUnit, hit.Damage, hitIndex);

        // Hiệu ứng camera và knockback
        if (cameraManager != null)
        {
            // Áp dụng hiệu ứng camera cho mỗi hit, hit cuối sẽ mạnh hơn
            StartCoroutine(HitImpactEffectCoroutine(cameraManager, isFinalHit));

            if (isFinalHit)
            {
                // Knockback chỉ ở đòn cuối
                var targetView = FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LinkedUnit == currentTarget);
                if (targetView != null && clashSequence != null)
                {
                    Vector3 direction = (targetView.transform.position - transform.position).normalized;
                    targetView.StartCoroutine(targetView.KnockbackCoroutine(direction, 0.5f, 0.2f));
                }
            }
        }

        Debug.Log($"[Hit {hitIndex + 1}/{pendingHits.Count}] " +
                  $"{LinkedUnit.UnitName} → {currentTarget.UnitName}: {hit.Damage} dmg");
    }

    private IEnumerator HitImpactEffectCoroutine(CombatCameraManager cam, bool isFinalHit)
    {
        // 1. Rung camera
        if (isFinalHit)
            cam.PlayFinalHitShake();
        else
            cam.PlayImpactShake();

        // 2. Xác định thông số zoom dựa trên loại hit
        float zoomInMultiplier = isFinalHit ? 0.75f : 0.90f; // Hit cuối zoom 25%, hit thường zoom 10%
        float zoomInDuration = isFinalHit ? 0.03f : 0.04f;   // Hit cuối nhanh hơn. Giảm giá trị để tăng tác động.
        float zoomOutDuration = isFinalHit ? 0.06f : 0.08f;

        // 3. Zoom nhanh vào và ra
        float originalSize = cam.GetCurrentOrthoSize();
        float zoomInSize = originalSize * zoomInMultiplier;

        // Zoom vào
        float elapsed = 0f;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInDuration);
            cam.SetCameraSize(Mathf.Lerp(originalSize, zoomInSize, t));
            yield return null;
        }
        cam.SetCameraSize(zoomInSize);

        // Zoom ra
        elapsed = 0f;
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomOutDuration);
            cam.SetCameraSize(Mathf.Lerp(zoomInSize, originalSize, t));
            yield return null;
        }
        cam.SetCameraSize(originalSize);
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

    public IEnumerator KnockbackCoroutine(Vector3 direction, float distance, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * distance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
    }

    /// <summary>
    /// Đưa unit trở về vị trí gốc đã được lưu.
    /// </summary>
    public void ResetPosition()
    {
        Debug.LogWarning($"[UnitView] RESET POSITION được gọi cho {LinkedUnit.UnitName}. Vị trí gốc: {originalPosition}", this.gameObject);
        transform.position = originalPosition;
    }

    // ── Animation Parameters ─────────────────────────────────────
    public void SetAnimationBool(string boolName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(boolName, value);
        }
    }

    public void SetAnimationFloat(string floatName, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(floatName, value);
        }
    }

    public void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    public void DisableRootMotion()
    {
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }
}