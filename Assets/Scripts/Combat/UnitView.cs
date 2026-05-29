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

    public event System.Action OnHitAnimationEvent;

    public CombatUnit LinkedUnit { get; private set; }

    private CombatUnit currentTarget;
    private List<HitData> pendingHits = new();
    private SkillData currentSkill;
    private CombatCameraManager cameraManager;
    private ClashAnimationSequence clashSequence;
    private Vector3 originalPosition;
    private bool originalPositionHasBeenSet = false;

    // Per-hit damage tracking
    private List<ActionOutcome> pendingOutcomes = new();
    private CombatUnit pendingCaster;
    private int pendingHitCount = 1;
    private int currentHitIndex = 0;

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
        spriteRenderer.flipX = !unit.IsPlayer;

        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
        if (clashSequence == null)
            clashSequence = FindFirstObjectByType<ClashAnimationSequence>();

        unit.OnDamageTaken += (caster, dmg) => 
        {
            SetAnimationTrigger("Knockback");
            TriggerHitFlash();
            UpdateHealthBar();
            if (FloatingTextController.Instance != null)
            {
                FloatingTextController.Instance.ShowFloatingText($"-{dmg}", transform.position + Vector3.up * 1.5f, Color.white);
            }
            if (cameraManager != null)
            {
                cameraManager.ZoomToUnit(transform, cameraManager.damageZoomSize);
                if (caster != null && caster.IsPlayer)
                    cameraManager.PlayPlayerImpactEffect(transform);
                else
                    cameraManager.PlayImpactShake();
            }
        };
        unit.OnHealed += (amount) =>
        {
            UpdateHealthBar();
            TriggerHealFlash();
            if (FloatingTextController.Instance != null)
            {
                FloatingTextController.Instance.ShowFloatingText($"+{amount}", transform.position + Vector3.up * 1.5f, Color.green);
            }
        };
        unit.OnDied += () => StartCoroutine(DeathFade());

        if (hitReceiver != null)
        {
            hitReceiver.OnHitFrame += ProcessHitAtFrame;
            hitReceiver.OnVFXFrame += ProcessVFXAtFrame;
        }

        if (healthBar != null)
        {
            Image fillImage = null;
            if (healthBar.fillRect != null)
                fillImage = healthBar.fillRect.GetComponentInChildren<Image>();
            if (fillImage == null)
                fillImage = healthBar.GetComponentInChildren<Image>();
            
            if (fillImage != null)
            {
                fillImage.color = unit.IsPlayer ? Color.green : Color.red;
                fillImage.enabled = true;
                fillImage.color = new Color(fillImage.color.r, fillImage.color.g, fillImage.color.b, 1f);
            }
            
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
            healthBar.value = (float)LinkedUnit.CurrentHP / LinkedUnit.MaxHP;
    }

    public void SetCurrentSkill(SkillData skill) { currentSkill = skill; }
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

    public void PlayAnimation(string stateName)
    {
        if (animator != null && !string.IsNullOrEmpty(stateName))
            animator.Play(stateName, -1, 0f);
    }

    public void SetAnimationTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;
        foreach (var trigger in animator.parameters)
            if (trigger.type == AnimatorControllerParameterType.Trigger)
                animator.ResetTrigger(trigger.name);
        animator.SetTrigger(triggerName);
    }

    public IEnumerator WaitUntilAnimationDone(string triggerName)
    {
        if (animator == null) yield break;
        int prevStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        float waitTimeout = 0.5f, waited = 0f;
        while (waited < waitTimeout)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.fullPathHash != prevStateHash) break;
            waited += Time.deltaTime;
            yield return null;
        }
        if (waited >= waitTimeout) yield break;
        var ns = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(ns.length - ns.normalizedTime * ns.length);
    }

    /// <summary>
    /// Gán danh sách outcomes và hitCount để animation per-hit có thể apply damage
    /// </summary>
    public void SetPendingOutcomes(List<ActionOutcome> outcomes, CombatUnit caster, int hitCount)
    {
        pendingOutcomes = outcomes;
        pendingCaster = caster;
        pendingHitCount = Mathf.Max(1, hitCount);
        currentHitIndex = 0;
    }

    /// <summary>
    /// Force apply tất cả pending outcomes còn lại (fallback khi animation không có hit events)
    /// </summary>
    public void FlushPendingOutcomes()
    {
        if (pendingOutcomes.Count == 0 || pendingCaster == null) return;

        // Apply damage còn lại cho tất cả outcomes chưa được process
        while (currentHitIndex < pendingHitCount)
        {
            foreach (var outcome in pendingOutcomes)
            {
                if (outcome.Target == null || !outcome.Target.IsAlive) continue;

                int baseDamage = outcome.Damage / pendingHitCount;
                int damageThisHit = baseDamage;
                if (currentHitIndex == pendingHitCount - 1)
                {
                    int remainder = outcome.Damage - baseDamage * (pendingHitCount - 1);
                    damageThisHit = remainder;
                }

                if (damageThisHit > 0)
                {
                    outcome.Target.TakeDamage(pendingCaster, damageThisHit, currentHitIndex);
                    Debug.Log($"[Flush Hit {currentHitIndex}] {outcome.Target.UnitName} nhận {damageThisHit} damage (fallback).");
                }
            }
            currentHitIndex++;
        }

        pendingOutcomes.Clear();
        pendingCaster = null;
        pendingHitCount = 1;
        currentHitIndex = 0;
    }

    public void OnHit() { OnHitAnimationEvent?.Invoke(); }

    private void ProcessHitAtFrame(int hitIndex)
    {
        // Apply damage per-hit từ pendingOutcomes
        if (pendingOutcomes.Count == 0 || pendingCaster == null) return;

        // Skip nếu đã xử lý hết hit count rồi (tránh duplicate từ animation)
        if (currentHitIndex >= pendingHitCount) return;

        int damageThisHit = 0;
        foreach (var outcome in pendingOutcomes)
        {
            if (outcome.Target == null || !outcome.Target.IsAlive) continue;

            // Chia damage đều cho các hit
            int baseDamage = outcome.Damage / pendingHitCount;
            // Hit cuối nhận phần dư
            if (currentHitIndex == pendingHitCount - 1)
            {
                int remainder = outcome.Damage - baseDamage * (pendingHitCount - 1);
                damageThisHit = remainder;
            }
            else
            {
                damageThisHit = baseDamage;
            }

            if (damageThisHit > 0)
            {
                outcome.Target.TakeDamage(pendingCaster, damageThisHit, currentHitIndex);
                Debug.Log($"[Hit {currentHitIndex}] {outcome.Target.UnitName} nhận {damageThisHit} damage. HP: {outcome.Target.CurrentHP}");
            }
        }

        currentHitIndex++;
    }
    private void ProcessVFXAtFrame(int vfxIndex)
    {
        if (currentSkill?.vfxPrefab == null || currentTarget == null) return;
        var targetView = FindObjectsByType<UnitView>(FindObjectsSortMode.None).FirstOrDefault(v => v.LinkedUnit == currentTarget);
        Vector3 pos = targetView != null ? targetView.transform.position : Vector3.zero;
        pos += Vector3.up * currentSkill.vfxOffset;
        var vfx = Instantiate(currentSkill.vfxPrefab, pos, Quaternion.identity);
        Destroy(vfx, 2f);
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

    public void TriggerHealFlash() => StartCoroutine(HealFlash());
    private IEnumerator HealFlash()
    {
        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        spriteRenderer.color = Color.green;
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
            spriteRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;
        }
        gameObject.SetActive(false);
    }

    public IEnumerator KnockbackCoroutine(Vector3 direction, float distance, float duration)
    {
        Vector3 start = transform.position;
        Vector3 end = start + direction * distance;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        transform.position = end;
    }

    public void ResetPosition() { transform.position = originalPosition; }
    public void SetAnimationBool(string n, bool v) => animator?.SetBool(n, v);
    public void SetAnimationFloat(string n, float v) => animator?.SetFloat(n, v);
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