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
    
    /// <summary>
    /// Xóa tất cả handlers để tránh skill cũ dùng sai SFX cho skill mới.
    /// </summary>
    public void ClearAnimationEventHandlers()
    {
        OnHitAnimationEvent = null;
        OnAnimationEndEvent = null;
    }

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
    // Bộ đếm VFX riêng — đảm bảo spawn ĐÚNG hitCount VFX theo đúng thứ tự vfxEvents,
    // không phụ thuộc vào số lượng animation hit event (clip có thể thiếu hit frame).
    private int vfxIndex = 0;
    // FORCE: danh dau khi an motion event cua chinh che do (vfxTriggerMode) da spawn VFX.
    // Neu true, cac fallback theo thoi gian PHÁI bo qua VFX (chi giu SFX) de dam bao
    // skill co ca OnHit va OnSpawnVFX chi tuan theo dung che do da setup, khong spa nen 2.
    private bool _eventVFXSeenForMode = false;

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

            // Nếu SuppressDamageText được bật (burn damage đầu lượt),
            // hiển thị "BURN! {dmg}" để vừa thấy hiệu ứng vừa thấy số sát thương.
            if (unit.SuppressDamageText)
            {
                DamageTextManager.Instance?.ShowStatusText($"BURN! {dmg}", GetDamageTextPosition(), StatusEffectType.ThieuDot, direction);
                unit.SuppressDamageText = false;
            }
            else
            {
                Vector3 textPos = GetDamageTextPosition();
                DamageTextManager.Instance?.ShowDamage(dmg, textPos, damageType, direction);
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

// ── Status text (STUN!, BURN!, v.v.) ──
        // Bay ngược hướng với damage text để tách biệt
        unit.OnStatusApplied += (text, status) =>
        {
            if (DamageTextManager.Instance == null) return;
            Vector2 direction = Vector2.up;
            if (spriteRenderer != null)
            {
                float dirX = spriteRenderer.flipX ? 1f : -1f;
                // Status text bay ngược hướng X so với damage text
                direction = new Vector2(-dirX, 0.5f).normalized;
            }
            // Status text offset cao hơn damage text một chút
            DamageTextManager.Instance.ShowStatusText(text, GetDamageTextPosition() + Vector3.up * 0.3f, status, direction);
        };

// ── Buff text (DMG UP!/DEF UP!) ──
        // Bay lên nhẹ nhàng, vị trí thấp hơn damage text
        // Delay 0.1s để xuất hiện cùng lúc với VFX spawn
        unit.OnBuffApplied += (text, stat, isBuff) =>
        {
            if (DamageTextManager.Instance == null) return;
            Vector2 direction = Vector2.up;
            if (spriteRenderer != null)
            {
                float dirX = spriteRenderer.flipX ? 1f : -1f;
                // Buff text bay lên chính giữa, không bay ngang
                direction = new Vector2(dirX * 0.2f, 1f).normalized;
            }
            var capturedText = text;
            var capturedStat = stat;
            var capturedIsBuff = isBuff;
            var capturedDir = direction;
            var capturedPos = GetDamageTextPosition() + Vector3.down * 0.5f;
            // Delay 0.1s để hiển thị cùng lúc VFX
            StartCoroutine(DelayedShowBuffText(capturedText, capturedPos, capturedStat, capturedIsBuff, capturedDir, 0.1f));
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
            // Fires damage per hit-frame.
            hitReceiver.OnHitFrame += ProcessHitAtFrame;
            // 🔥 Đăng ký OnVFXFrame: spawn VFX theo animation event OnSpawnVFX
            hitReceiver.OnVFXFrame += SpawnVFXAtFrame;
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
            hitReceiver.OnVFXFrame -= SpawnVFXAtFrame;
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
        vfxIndex = 0;
        _eventVFXSeenForMode = false; // FORCE: reset moi skill
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

        // Fallback VFX: nếu animation clip có ÍT hit frame hơn hitCount (vd skill 4 hit nhưng
        // clip chỉ có 3 OnHit), các hit còn lại rơi vào nhánh flush này → bổ sung VFX còn thiếu
        // để spawn ĐỦ hitCount VFX theo đúng thứ tự vfxEvents.
        SpawnRemainingVFX();

        pendingOutcomes.Clear();
        pendingCaster = null;
        pendingHitCount = 1;
        currentHitIndex = 0;
        vfxIndex = 0;
    }

    public void OnHit() { OnHitAnimationEvent?.Invoke(); }

    /// <summary>
    /// Spawn VFX tại frame được chỉ định bởi animation event OnSpawnVFX.
    /// 🔥 SFX theo option sfxTriggerMode:
    /// - OnSpawnVFX: phát SFX theo vfxIndex (mỗi VFX spawn phát 1 SFX).
    /// - OnHit: KHÔNG phát SFX ở đây (SFX do ProcessHitAtFrame xử lý).
    /// </summary>
    private void SpawnVFXAtFrame(int vfxIndex)
    {
        if (currentSkill == null) return;
        // If VFX index was already spawned (by ProcessHitAtFrame), skip
        if (vfxIndex < this.vfxIndex) return;

        // VFX follows vfxTriggerMode: only spawn here when mode == OnSpawnVFX.
        bool vfxOnSpawnVFX = currentSkill.vfxTriggerMode == VFXTriggerMode.OnSpawnVFX;
        // SFX follows sfxTriggerMode independently of VFX.
        bool sfxOnSpawnVFX = currentSkill.sfxTriggerMode == SFXTriggerMode.OnSpawnVFX;

        // SFX on OnSpawnVFX event (independent of VFX spawn choice)
        if (sfxOnSpawnVFX) PlaySFXAt(vfxIndex);

        // In OnHit mode, do NOT spawn VFX here and do NOT advance vfxIndex
        // (so OnHit spawning is not disturbed).
        if (!vfxOnSpawnVFX) return;

        _eventVFXSeenForMode = true; // FORCE: VFX cua che do OnSpawnVFX da spawn
        VFXEvent evt = GetVFXEvent(vfxIndex);
        if (evt != null) SpawnVFX(evt);

        // Mark this VFX as spawned
        this.vfxIndex = Mathf.Max(this.vfxIndex, vfxIndex + 1);
    }

    /// <summary>
    /// Xử lý hit: áp damage + spawn VFX/SFX.
    /// 🔥 SFX theo option sfxTriggerMode:
    /// - OnHit: phát SFX theo hitIndex (mỗi hit phát 1 SFX).
    /// - OnSpawnVFX: KHÔNG phát SFX ở đây (SFX do SpawnVFXAtFrame xử lý).
    /// </summary>
    private void ProcessHitAtFrame(int hitIndex)
    {
        if (pendingOutcomes.Count == 0 || pendingCaster == null) return;
        if (currentHitIndex >= pendingHitCount) return;

        // VFX follows vfxTriggerMode: spawn VFX from OnHit when mode == OnHit.
        bool vfxOnHit = currentSkill != null && currentSkill.vfxTriggerMode == VFXTriggerMode.OnHit;
        // SFX follows sfxTriggerMode independently of VFX.
        bool sfxOnHit = currentSkill != null && currentSkill.sfxTriggerMode == SFXTriggerMode.OnHit;

        // VFX from OnHit (each hit spawns 1 VFX, cycling vfxEvents if fewer than hitCount)
        if (vfxOnHit)
        {
            if (hitIndex >= this.vfxIndex)
            {
                _eventVFXSeenForMode = true; // FORCE: VFX cua che do OnHit da spawn
                VFXEvent evt = GetVFXEvent(hitIndex);
                if (evt != null) SpawnVFX(evt);
                this.vfxIndex = Mathf.Max(this.vfxIndex, hitIndex + 1);
            }
        }

        // SFX on OnHit (each hit plays 1 SFX)
        if (sfxOnHit) PlaySFXAt(hitIndex);

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

    /// <summary>Phát SFX tại index (helper method dùng chung).</summary>
    private void PlaySFXAt(int index)
    {
        if (currentSkill == null || currentSkill.sfxClips == null || currentSkill.sfxClips.Length == 0 || !currentSkill.playSFX) return;
        int sfxIndex = index % currentSkill.sfxClips.Length;
        if (currentSkill.sfxClips[sfxIndex] != null)
            CombatAudioManager.Instance?.PlaySFX(currentSkill.sfxClips[sfxIndex]);
    }

    /// <summary>
    /// 🔥 Fallback spawn VFX theo thời gian chia đều — AN TOÀN không trùng lặp.
    /// ⚠️ KHÔNG phát SFX — SFX chỉ do animation events xử lý:
    ///    - 1 vfx → OnSpawnVFX
    ///    - >1 vfx → OnHit
    /// Chờ 1 frame đầu tiên (yield return null) để animation events chạy trước,
    /// sau đó chỉ spawn các VFX còn thiếu (vfxIndex chưa cover).
    /// </summary>
        /// <summary>
    /// Skill tan cong da duoc designer setup VFX (hitCount > 0 + vfxEvents/vfxPrefab + vfxTriggerMode)
    /// -> VFX phai theo DUNG animation event cua che do da chon, TAT fallback thoi gian.
    /// Skill buff/heal (hitCount <= 0) -> KHONG force, van dung fallback thoi gian de co VFX/SFX.
    /// </summary>
    private bool HasAttackConfiguredVFX()
    {
        if (currentSkill == null) return false;
        if (currentSkill.hitCount <= 0) return false;
        return (currentSkill.vfxEvents != null && currentSkill.vfxEvents.Length > 0) || currentSkill.vfxPrefab != null;
    }
public void PlaySkillEffects(float duration)
    {
        if (currentSkill == null) return;

        // FORCE: skill da duoc designer cau hinh VFX (vfxEvents/vfxPrefab + vfxTriggerMode)
        // -> VFX CHI do animation event cua che do kiem soat. KHONG chay fallback thoi gian o day
        // (tranh lam tang/lam on vfxIndex lam pha che do spawn theo event, gay VFX "theo on hit").
        bool hasConfiguredVFX = HasAttackConfiguredVFX();
        if (hasConfiguredVFX) return;

        int targetCount = GetVFXTargetCount();
        if (targetCount <= 0) return;
        StartCoroutine(SpawnSkillEffectsCoroutine(duration, targetCount));
    }

    private IEnumerator SpawnSkillEffectsCoroutine(float duration, int targetCount)
    {
        // 🔥 Chờ 1 frame — để animation events (OnSpawnVFX/OnHit) được xử lý trước,
        // vfxIndex đã được cập nhật → timer KHÔNG spawn trùng lặp.
        yield return null;

        // FORCE: chi spawn VFX theo fallback thoi gian CHO SKILL CHUA CAU HINH VFX
        // (buff skill khong co vfxEvents/vfxPrefab). Neu skill da duoc designer setup VFX
        // (vfxEvents/ vfxPrefab + vfxTriggerMode), VFX phai theo dung animation event cua che do
        // da chon — TAT fallback thoi gian de khong spawn VFX "theo hit"/"theo thoi gian" sai setup.
        bool hasConfiguredVFX = HasAttackConfiguredVFX();
        bool canSpawnVFXFallback = !hasConfiguredVFX && !_eventVFXSeenForMode;

        // 🔥 Nếu sfxTriggerMode == OnSpawnVFX → phát SFX fallback cho skill buff
        // (skill buff thường không có OnHit/OnSpawnVFX events → cần fallback)
        bool sfxOnSpawnVFX = currentSkill != null && currentSkill.sfxTriggerMode == SFXTriggerMode.OnSpawnVFX;

        if (duration <= 0f)
        {
            while (vfxIndex < targetCount)
            {
                if (canSpawnVFXFallback)
                {
                    VFXEvent evt = GetVFXEvent(vfxIndex);
                    if (evt != null) SpawnVFX(evt);
                }
                vfxIndex++;
                // 🔥 THÊM: Phát SFX fallback nếu skill buff không có events
                if (sfxOnSpawnVFX) PlaySFXAt(vfxIndex - 1);
            }
            yield break;
        }

        float interval = duration / targetCount;
        for (int i = 0; i < targetCount; i++)
        {
            if (vfxIndex >= targetCount) break;
            if (canSpawnVFXFallback)
            {
                VFXEvent evt = GetVFXEvent(vfxIndex);
                if (evt != null) SpawnVFX(evt);
            }
            vfxIndex++;
            // 🔥 THÊM: Phát SFX fallback nếu skill buff không có events
            if (sfxOnSpawnVFX) PlaySFXAt(vfxIndex - 1);
            if (i < targetCount - 1)
                yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>Trả về index VFX hiện tại (dùng để debug).</summary>
    public int GetCurrentVFXIndex() => vfxIndex;

    /// <summary>Bổ sung toàn bộ via VFX còn thiếu (dùng trong fallback flush).</summary>
    private void SpawnRemainingVFX()
    {
        if (currentSkill == null) return;

        bool hasConfiguredVFX = HasAttackConfiguredVFX();

        // FORCE: skill da duoc designer cau hinh VFX -> VFX CHI theo dung animation event cua
        // che do da chon (OnHit hoac OnSpawnVFX). Khong fill them VFX nao o day — ngay ca khi
        // clip phat it event hon hitCount — de trai ve dung setup event, khong "spawn on hit".
        if (hasConfiguredVFX) return;

        int targetCount = GetVFXTargetCount();
        while (vfxIndex < targetCount)
        {
            VFXEvent evt = GetVFXEvent(vfxIndex);
            vfxIndex++;
            if (evt != null) SpawnVFX(evt);
        }
    }

    /// <summary>Số VFX cần spawn = max(hitCount, vfxEvents.Length).</summary>
    private int GetVFXTargetCount()
    {
        int count = pendingHitCount;
        if (currentSkill != null && currentSkill.vfxEvents != null)
            count = Mathf.Max(count, currentSkill.vfxEvents.Length);
        return count;
    }

/// <summary>
    /// Lấy VFXEvent theo index. Nếu skill có ÍT VFX event hơn hitCount (vd hitCount=4 nhưng
    /// chỉ 1 vfxEvents), sẽ CYCLE/REUSE các event có sẵn để mỗi hit đều có VFX (index n % len).
    /// Fallback vfxPrefab đơn lẻ nếu không có vfxEvents.
    /// </summary>
    private VFXEvent GetVFXEvent(int index)
    {
        if (currentSkill == null) return null;

        if (currentSkill.vfxEvents != null && currentSkill.vfxEvents.Length > 0)
        {
            int i = index % currentSkill.vfxEvents.Length;
            return currentSkill.vfxEvents[i];
        }

        if (currentSkill.vfxPrefab != null)
        {
            return new VFXEvent
            {
                vfxPrefab = currentSkill.vfxPrefab,
                offset = new Vector3(0, currentSkill.vfxOffset, 0),
                spawnMode = VFXSpawnMode.AtTarget
            };
        }

        return null;
    }

    /// <summary>Instantiate 1 VFX event tại vị trí phù hợp theo spawnMode.</summary>
    private void SpawnVFX(VFXEvent evt)
    {
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
                // 🔥 FIX (kẹt animation khi hết stun): khi bắt đầu bị stun, trên người nhân vật
                // có thể còn animation Knockback/Hurt từ chính đòn gây stun. SetStunVisual chỉ
                // đổi màu, KHÔNG reset animation — nên sau khi hết stun nhân vật vẫn kẹt ở pose đó.
                // Khi phát hiện stun vừa KẾT THÚC, đưa animation về Idle để hết kẹt.
                if (!isStunned && wasStunned)
                    SetAnimationTrigger(AnimationConstants.Idle);
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

    /// <summary>
    /// Hiển thị buff text sau một khoảng delay (để đồng bộ với VFX).
    /// </summary>
    private IEnumerator DelayedShowBuffText(string text, Vector3 position, StatType stat, bool isBuff, Vector2 direction, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ShowBuffText(text, position, stat, isBuff, direction);
        }
    }
}
