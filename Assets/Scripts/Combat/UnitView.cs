using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UnitView : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public HitEventReceiver hitReceiver;
    public Slider healthBar;

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
        unit.OnDamageTaken += (caster, dmg) => 
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

        // === CẢI THIỆN: ĐẶT MÀU THANH MÁU NGAY LẬP TỨC ===
        if (healthBar != null)
        {
            // Tìm Image Fill (màu của phần máu)
            Image fillImage = null;
            if (healthBar.fillRect != null)
                fillImage = healthBar.fillRect.GetComponentInChildren<Image>();
            if (fillImage == null)
                fillImage = healthBar.GetComponentInChildren<Image>();
            
            if (fillImage != null)
            {
                fillImage.color = unit.IsPlayer ? Color.green : Color.red;
                fillImage.enabled = true;
                // Ép alpha = 1 để chắc chắn hiển thị
                fillImage.color = new Color(fillImage.color.r, fillImage.color.g, fillImage.color.b, 1f);
            }
            else
            {
                Debug.LogWarning($"UnitView: Không tìm thấy Fill Image cho {unit.UnitName}");
            }
            
            // Cập nhật giá trị slider ngay
            healthBar.value = (float)unit.CurrentHP / unit.MaxHP;
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

    // ── Animation Control ─────────────────────────────────────
    
    /// <summary>
    /// (Mới) Buộc Animator phải chạy một state cụ thể.
    /// Rất mạnh mẽ, bỏ qua các transition, đi thẳng vào state.
    /// </summary>
    public void PlayAnimation(string stateName)
    {
        if (animator != null && !string.IsNullOrEmpty(stateName))
        {
            // Tham số thứ hai (-1) là layer index, -1 có nghĩa là layer base.
            // Tham số thứ ba (0f) là normalized time, 0f để bắt đầu từ đầu.
            animator.Play(stateName, -1, 0f);
        }
    }

    /// <summary>
    /// (Cải tiến) Kích hoạt một trigger.
    /// Sẽ reset các trigger khác để đảm bảo chỉ có trigger này được kích hoạt.
    /// </summary>
    public void SetAnimationTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;

        // Reset tất cả các trigger khác trước khi đặt trigger mới
        // để tránh các hành vi không mong muốn.
        foreach (var trigger in animator.parameters)
        {
            if (trigger.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(trigger.name);
            }
        }
        animator.SetTrigger(triggerName);
    }


    public void ForcePlayAnimationState(string stateName)
    {
        if (animator != null)
            animator.Play(stateName);
    }

    public void ResetAnimationTrigger(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.ResetTrigger(triggerName);
    }

    // ── Chờ animation clip chạy xong ─────────────────────────
    public IEnumerator WaitUntilAnimationDone(string triggerName)
    {
        if (animator == null) yield break;

        int prevStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        float waitTimeout = 0.5f;
        float waited = 0f;
        while (waited < waitTimeout)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.fullPathHash != prevStateHash) break;
            waited += Time.deltaTime;
            yield return null;
        }

        if (waited >= waitTimeout)
        {
            Debug.LogWarning($"[UnitView] '{triggerName}': Animator không đổi state sau {waitTimeout}s.");
            yield break;
        }

        var newStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = newStateInfo.length;
        float alreadyElapsed = newStateInfo.normalizedTime * clipLength;
        float remaining = Mathf.Max(0f, clipLength - alreadyElapsed);
        Debug.Log($"[UnitView] '{triggerName}' clipLength={clipLength:F2}s remaining={remaining:F2}s");
        yield return new WaitForSeconds(remaining);
    }

    public void OnHit()
    {
        OnHitAnimationEvent?.Invoke();
    }

    // ── Xử lý hit từ Animation Event ──────────────────────────
    private void ProcessHitAtFrame(int hitIndex)
    {
        if (currentTarget == null) return;
        if (hitIndex >= pendingHits.Count) return;

        var hit = pendingHits[hitIndex];
        bool isFinalHit = (hitIndex == pendingHits.Count - 1);
        currentTarget.TakeDamage(LinkedUnit, hit.Damage, hitIndex);

        if (cameraManager != null)
        {
            StartCoroutine(HitImpactEffectCoroutine(cameraManager, isFinalHit));
            if (isFinalHit)
            {
                var targetView = FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LinkedUnit == currentTarget);
                if (targetView != null && clashSequence != null)
                {
                    Vector3 direction = (targetView.transform.position - transform.position).normalized;
                    targetView.StartCoroutine(targetView.KnockbackCoroutine(direction, 0.5f, 0.2f));
                }
            }
        }
        Debug.Log($"[Hit {hitIndex + 1}/{pendingHits.Count}] {LinkedUnit.UnitName} → {currentTarget.UnitName}: {hit.Damage} dmg");
    }

    private IEnumerator HitImpactEffectCoroutine(CombatCameraManager cam, bool isFinalHit)
    {
        if (isFinalHit) cam.PlayFinalHitShake();
        else cam.PlayImpactShake();

        float zoomInMultiplier = isFinalHit ? 0.75f : 0.90f;
        float zoomInDuration = isFinalHit ? 0.03f : 0.04f;
        float zoomOutDuration = isFinalHit ? 0.06f : 0.08f;

        float originalSize = cam.GetCurrentOrthoSize();
        float zoomInSize = originalSize * zoomInMultiplier;

        float elapsed = 0f;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomInDuration);
            cam.SetCameraSize(Mathf.Lerp(originalSize, zoomInSize, t));
            yield return null;
        }
        cam.SetCameraSize(zoomInSize);

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

    private void ProcessVFXAtFrame(int vfxIndex)
    {
        if (currentSkill?.vfxPrefab == null) return;
        if (currentTarget == null) return;

        Vector3 spawnPos = Vector3.zero;
        var targetView = FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LinkedUnit == currentTarget);
        if (targetView != null) spawnPos = targetView.transform.position;
        spawnPos += Vector3.up * currentSkill.vfxOffset;

        var vfx = Instantiate(currentSkill.vfxPrefab, spawnPos, Quaternion.identity);
        Destroy(vfx, 2f);
        Debug.Log($"[VFX] Spawn {currentSkill.vfxPrefab.name} tại {spawnPos}");
    }

    public float GetClipLength(string clipName)
    {
        if (animator?.runtimeAnimatorController == null) return 0.5f;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;
        return 0.5f;
    }

    public void TriggerHitFlash() => StartCoroutine(HitFlash());
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

    private IEnumerator DeathFade()
    {
        SetAnimationTrigger("Die");
        float elapsed = 0f, duration = 0.6f;
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

    public void ResetPosition()
    {
        Debug.LogWarning($"[UnitView] RESET POSITION gọi cho {LinkedUnit.UnitName}. Vị trí gốc: {originalPosition}", this.gameObject);
        transform.position = originalPosition;
    }

    public void SetAnimationBool(string boolName, bool value) => animator?.SetBool(boolName, value);
    public void SetAnimationFloat(string floatName, float value) => animator?.SetFloat(floatName, value);
    public void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
    public void DisableRootMotion() { if (animator != null) animator.applyRootMotion = false; }
}