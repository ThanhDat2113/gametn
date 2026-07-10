using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitView : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public HitEventReceiver hitReceiver;
    public Slider healthBar;

    [Header("Health Bar Text")]
    public TextMeshProUGUI healthText;
    public string healthTextFormat = "{0}/{1}";
    public Color healthyColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    public event System.Action OnHitAnimationEvent;
    public event System.Action OnAnimationEndEvent;

    public CombatUnit LinkedUnit { get; private set; }

    private CombatUnit currentTarget;
    private List<HitData> pendingHits = new();
    private SkillData currentSkill;
    private CombatCameraManager cameraManager;
    private ClashAnimationSequence clashSequence;
    private Vector3 originalPosition;
    private bool originalPositionHasBeenSet = false;

    private List<ActionOutcome> pendingOutcomes = new();
    private CombatUnit pendingCaster;
    private int pendingHitCount = 1;
    private int currentHitIndex = 0;

    // ─── Damage Text Position ────────────────────────────────────
    [Header("Damage Text Offset")]
    [Tooltip("Độ cao so với vị trí transform của nhân vật (world units)")]
    public float damageTextHeightOffset = 2.5f;

    public void OnAnimationEnd() { OnAnimationEndEvent?.Invoke(); }

    public Vector3 GetOriginalPosition() => originalPosition;
    public void StoreOriginalPosition(Vector3 position)
    {
        originalPosition = position;
        originalPositionHasBeenSet = true;
    }
    public void StoreOriginalPosition() => StoreOriginalPosition(transform.position);

    /// <summary>
    /// Lấy vị trí world để hiển thị damage text.
    /// </summary>
    public Vector3 GetDamageTextPosition()
    {
        return transform.position + Vector3.up * damageTextHeightOffset;
    }

    public void Setup(CombatUnit unit)
    {
        LinkedUnit = unit;

        if (unit.Data.battleSprite != null)
            spriteRenderer.sprite = unit.Data.battleSprite;

        if (unit.Data.flipOnSpawn)
            spriteRenderer.flipX = !unit.IsPlayer;
        else
            spriteRenderer.flipX = false;

        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CombatCameraManager>();
        if (clashSequence == null)
            clashSequence = FindFirstObjectByType<ClashAnimationSequence>();

        // ── ĐÃ SỬA: delegate đúng 3 tham số ──
        unit.OnDamageTaken += (caster, dmg, damageType) => 
        {
            SetAnimationTrigger("Knockback");
            TriggerHitFlash();
            UpdateHealthBar();

            Vector2 direction = Vector2.up;
            if (spriteRenderer != null)
            {
                float dirX = spriteRenderer.flipX ? 1f : -1f;
                direction = new Vector2(dirX, 0.5f).normalized;
            }

            Vector3 textPos = GetDamageTextPosition();
            DamageTextManager.Instance?.ShowDamage(dmg, textPos, damageType, direction);

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

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TextMeshProUGUI>();
            if (healthText == null && healthBar != null)
                healthText = healthBar.GetComponentInChildren<TextMeshProUGUI>();
        }

        UpdateHealthBar();

        var worldCanvas = GetComponentInChildren<Canvas>();
        if (worldCanvas != null && worldCanvas.renderMode == RenderMode.WorldSpace && worldCanvas.worldCamera == null)
        {
            if (cameraManager != null)
                worldCanvas.worldCamera = cameraManager.GetComponent<Camera>();
            else
                worldCanvas.worldCamera = Camera.main;
        }

        // Bắt đầu monitor Stun status
        StartCoroutine(MonitorStunStatus());
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
        if (LinkedUnit == null) return;
        if (healthBar != null)
            healthBar.value = (float)LinkedUnit.CurrentHP / LinkedUnit.MaxHP;

        if (healthText != null)
        {
            int currentHP = LinkedUnit.CurrentHP;
            int maxHP = LinkedUnit.MaxHP;
            healthText.text = string.Format(healthTextFormat, currentHP, maxHP);
            float ratio = (float)currentHP / maxHP;
            if (ratio <= 0.25f)
                healthText.color = dangerColor;
            else if (ratio <= 0.5f)
                healthText.color = warningColor;
            else
                healthText.color = healthyColor;
        }
    }

    public void SetCurrentSkill(SkillData skill) { currentSkill = skill; }
    public void SetCurrentTarget(CombatUnit target) { currentTarget = target; }
    
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

    public void SetPendingOutcomes(List<ActionOutcome> outcomes, CombatUnit caster, int hitCount)
    {
        pendingOutcomes = outcomes;
        pendingCaster = caster;
        pendingHitCount = Mathf.Max(1, hitCount);
        currentHitIndex = 0;
    }

    public void FlushPendingOutcomes()
    {
        if (pendingOutcomes.Count == 0 || pendingCaster == null) return;

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
                    outcome.Target.TakeDamage(pendingCaster, damageThisHit, DamageType.Physical);
                    string logMessage = $"[Flush Hit {currentHitIndex}] {outcome.Target.UnitName} nhận {damageThisHit} damage (fallback).";
                    if (outcome.EmpowerMultiplier > 1f)
                        logMessage += $" ({outcome.EmpowerMultiplier:F1}x)";
                    Debug.Log(logMessage);
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
        if (pendingOutcomes.Count == 0 || pendingCaster == null) return;
        if (currentHitIndex >= pendingHitCount) return;

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
                outcome.Target.TakeDamage(pendingCaster, damageThisHit, DamageType.Physical);
                string logMessage = $"[Hit {currentHitIndex}] {outcome.Target.UnitName} nhận {damageThisHit} damage.";
                if (outcome.EmpowerMultiplier > 1f)
                    logMessage += $" ({outcome.EmpowerMultiplier:F1}x)";
                Debug.Log(logMessage + $" HP: {outcome.Target.CurrentHP}");
            }
        }
        currentHitIndex++;
    }

    private void ProcessVFXAtFrame(int vfxIndex)
    {
        if (currentSkill == null) return;

        VFXEvent evt = null;
        if (currentSkill.vfxEvents != null && vfxIndex >= 0 && vfxIndex < currentSkill.vfxEvents.Length)
            evt = currentSkill.vfxEvents[vfxIndex];
        else if (vfxIndex == 0 && currentSkill.vfxPrefab != null)
        {
            evt = new VFXEvent 
            { 
                vfxPrefab = currentSkill.vfxPrefab, 
                offset = new Vector3(0, currentSkill.vfxOffset, 0),
                spawnMode = VFXSpawnMode.AtTarget
            };
        }

        if (evt == null || evt.vfxPrefab == null) return;

        Vector3 spawnPos;
        Transform parent = null;

        switch (evt.spawnMode)
        {
            case VFXSpawnMode.AtCaster:
                spawnPos = transform.position + evt.offset;
                parent = transform;
                break;
            case VFXSpawnMode.AtTarget:
            case VFXSpawnMode.HitOnEachTarget:
                var targetView = FindViewForUnit(currentTarget);
                if (targetView != null)
                {
                    spawnPos = targetView.transform.position + evt.offset;
                    parent = evt.attachToCaster ? transform : null;
                }
                else
                {
                    spawnPos = transform.position + evt.offset;
                    parent = transform;
                }
                break;
            default:
                spawnPos = transform.position + evt.offset;
                parent = transform;
                break;
        }

        GameObject vfx = Instantiate(evt.vfxPrefab, spawnPos, Quaternion.identity);
        if (evt.attachToCaster && parent != null)
            vfx.transform.SetParent(parent);
        
        var visualEffect = vfx.GetComponent<UnityEngine.VFX.VisualEffect>();
        if (visualEffect != null) visualEffect.Play();

        Destroy(vfx, 2f);
    }

    private UnitView FindViewForUnit(CombatUnit unit)
    {
        if (unit == null) return null;
        foreach (var view in FindObjectsByType<UnitView>(FindObjectsSortMode.None))
            if (view.LinkedUnit == unit)
                return view;
        return null;
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
        RestoreColorAfterFlash();
        yield return new WaitForSeconds(0.05f);
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        RestoreColorAfterFlash();
    }

    public void TriggerHealFlash() => StartCoroutine(HealFlash());
    private IEnumerator HealFlash()
    {
        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.08f);
        RestoreColorAfterFlash();
        yield return new WaitForSeconds(0.05f);
        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.08f);
        RestoreColorAfterFlash();
    }

    /// <summary>Khôi phục màu sau flash - nếu đang Stun thì tím, nếu không thì trắng.</summary>
    private void RestoreColorAfterFlash()
    {
        if (LinkedUnit != null && LinkedUnit.HasStatus(StatusEffectType.Stun) && LinkedUnit.IsAlive)
            spriteRenderer.color = new Color(0.7f, 0.4f, 0.9f, 1f); // Tím
        else
            spriteRenderer.color = Color.white;
    }

    /// <summary>
    /// Hiệu ứng nháy vàng - dùng cho Skeleton hồi sinh.
    /// </summary>
    public void TriggerReviveFlash() => StartCoroutine(ReviveFlash());
    private IEnumerator ReviveFlash()
    {
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = Color.yellow;
            yield return new WaitForSeconds(0.12f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.12f);
        }
    }

    /// <summary>
    /// Hiệu ứng tím cho trạng thái Stun - persistent (duy trì đến khi hết choáng).
    /// </summary>
    public void SetStunVisual(bool isStunned)
    {
        if (spriteRenderer == null) return;
        if (isStunned)
            spriteRenderer.color = new Color(0.7f, 0.4f, 0.9f, 1f);
        else
            spriteRenderer.color = Color.white;
    }

    /// <summary>
    /// Coroutine monitor trạng thái Stun, tự động cập nhật visual.
    /// </summary>
    private IEnumerator MonitorStunStatus()
    {
        bool wasStunned = false;
        while (LinkedUnit != null && LinkedUnit.IsAlive)
        {
            bool isStunned = LinkedUnit.HasStatus(StatusEffectType.Stun);
            if (isStunned != wasStunned)
            {
                SetStunVisual(isStunned);
                wasStunned = isStunned;
            }
            yield return new WaitForSeconds(0.2f);
        }
        // Khi unit chết, reset màu
        if (spriteRenderer != null)
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