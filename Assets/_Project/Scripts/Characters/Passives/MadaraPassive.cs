using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Passive của Madara Uchiha (Boss) - Đã sửa theo yêu cầu:
/// 1. Tạo 1 clone có HP = MaxHP của Madara
/// 2. Clone lặp lại chính xác skill Madara đang dùng lên cùng target
/// 3. Clone tồn tại đến khi bị tiêu diệt, không thể tạo lại
/// 4. Izanagi (Phase 3 - 1 lần): Khi HP về 0, hồi 100% máu, clear debuff
///    - Giảm 20% ATK/PDEF/MDEF nhưng tăng MaxActions lên 3
/// </summary>
public class MadaraPassive : PassiveAbility
{
    private const float IZANAGI_STAT_PENALTY = 0.20f;
    private const int IZANAGI_MAX_ACTIONS = 3;

    private bool _izanagiUsed = false;
    private bool _isDead = false;

    // Clone
    private CombatUnit _clone = null;
    private UnitView _cloneView = null;
    private bool _cloneSpawned = false;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        owner.MaxActionsPerTurn = 2;

        // Đăng ký sự kiện OnActionConfirmed để clone lặp lại skill
        owner.OnActionConfirmed += OnMadaraActionConfirmed;

        Debug.Log($"[{Owner.UnitName}'s Passive] Shadow Clone! Tạo 1 clone có HP ngang với Madara. Clone lặp lại skill của Madara.");
    }

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (Owner == null || !Owner.IsAlive) return;

        // Khi HP < 50% và chưa spawn clone → spawn clone
        float hpPercent = (float)Owner.CurrentHP / Owner.MaxHP;
        if (!_cloneSpawned && hpPercent < 0.50f)
        {
            SpawnClone();
        }
    }

    public override void OnDied()
    {
        if (_isDead) return;

        // Izanagi: hồi sinh 1 lần
        if (!_izanagiUsed)
        {
            _izanagiUsed = true;
            _isDead = false;

            // Hồi 100% máu
            Owner.CurrentHP = Owner.MaxHP;

            // Clear tất cả debuff
            Owner.ClearStatus(StatusEffectType.Stun);
            Owner.ClearStatus(StatusEffectType.ThieuDot);
            Owner.ClearStatus(StatusEffectType.DiemYeu);
            Owner.ClearStatus(StatusEffectType.Taunt);
            Owner.ClearStatus(StatusEffectType.GiamSatThuong);

            // Giảm 20% stats
            Owner.ATK = Mathf.RoundToInt(Owner.ATK * (1f - IZANAGI_STAT_PENALTY));
            Owner.PDEF = Mathf.RoundToInt(Owner.PDEF * (1f - IZANAGI_STAT_PENALTY));
            Owner.MDEF = Mathf.RoundToInt(Owner.MDEF * (1f - IZANAGI_STAT_PENALTY));

            // Tăng MaxActions lên 3
            Owner.MaxActionsPerTurn = IZANAGI_MAX_ACTIONS;

// Invincible: miễn thương đúng 1 đòn duy nhất.
            // duration = 0 (không đếm lượt) để đảm bảo tồn tại cho tới khi bị 1 đòn
            // đánh tiêu thụ (không bị TickStatuses xóa sớm theo lượt).
            Owner.ApplyStatus(StatusEffectType.Invincible, 0);

            Debug.Log($"[{Owner.UnitName}'s Passive] IZANAGI! Hồi sinh với {Owner.CurrentHP} HP! Stats giảm 20% nhưng MaxActions = {IZANAGI_MAX_ACTIONS}!");

        // Text hiệu ứng Izanagi
        var view = CombatManager.Instance?.GetUnitView(Owner);
        if (view != null)
            DamageTextManager.Instance?.ShowStatusText("IZANAGI!", view.GetDamageTextPosition(), DamageTextManager.Instance.reviveColor, Vector2.up);

// Cập nhật visual - khôi phục đầy đủ view giống cách Hassan hồi sinh.
            // DeathFade (kích hoạt bởi OnDied event) đã tắt GameObject và fade alpha về 0.
            // Nếu không khôi phục ở đây, Madara sau hồi sinh sẽ vô hình và không thể
            // được chọn/tấn công (view không active trong scene).
            if (view != null)
            {
                // Dừng DeathFade (và mọi coroutine đang chạy) trước khi khôi phục view
                view.StopAllCoroutines();
                view.gameObject.SetActive(true);
                view.SetAlpha(1f);
                view.UpdateHealthBar();
                view.TriggerReviveFlash();
            }
        }
        else
        {
            _isDead = true;
            // Xóa clone khi Madara chết thật sự
            DestroyClone();
        }
    }

    /// <summary>
    /// Khi Madara dùng skill, clone cũng dùng skill đó lên cùng target
    /// </summary>
    private void OnMadaraActionConfirmed(CombatUnit caster, SkillData skill, List<CombatUnit> targets)
    {
        if (_clone == null || !_clone.IsAlive || !_cloneSpawned) return;
        if (caster != Owner) return;

        Debug.Log($"[{Owner.UnitName}'s Passive] Clone lặp lại skill [{skill.skillName}]!");

        // Clone dùng skill lên cùng target
        _clone.SelectSkill(skill, targets);
        _clone.ExecuteSelectedSkill(0); // AP cost = 0 cho clone
    }

    private void SpawnClone()
    {
        if (_cloneSpawned) return;
        _cloneSpawned = true;

        Debug.Log($"[{Owner.UnitName}'s Passive] Triệu hồi Shadow Clone!");

        if (CombatManager.Instance == null) return;

        // Tạo 1 clone
        var clone = new CombatUnit();
        clone.Initialize(Owner.Data, Owner.Level, false);
        clone.UnitName = "Afterimage";
        clone.IsTargetable = true;

        // Copy 100% HP của Madara
        clone.MaxHP = Owner.MaxHP;
        clone.CurrentHP = Owner.MaxHP;
        clone.ATK = Owner.ATK;
        clone.PDEF = Owner.PDEF;
        clone.MDEF = Owner.MDEF;

        // Grid slot: bên cạnh Madara
        int cloneSlot = Mathf.Clamp(Owner.GridSlot + 1, 0, 8);
        clone.GridSlot = cloneSlot;
        clone.GridRow = Owner.GridRow;

        // Copy toàn bộ skill của Madara
        if (Owner.AvailableSkills != null)
        {
            clone.AvailableSkills = new List<SkillData>(Owner.AvailableSkills);
        }

        _clone = clone;
        CombatManager.Instance.EnemyUnits.Add(clone);

        // Spawn view
        var prefab = Owner.Data.prefab;
        if (prefab != null)
        {
            var gridSlots = CombatManager.Instance.enemyGridSlots;
            if (gridSlots != null && cloneSlot < gridSlots.Length && gridSlots[cloneSlot] != null)
            {
                var go = UnityEngine.Object.Instantiate(prefab, gridSlots[cloneSlot].position, Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, CombatManager.Instance.gameObject.scene);
                var cloneView = go.GetComponent<UnitView>();
                if (cloneView != null)
                {
                    cloneView.Setup(clone);
                    cloneView.StoreOriginalPosition(gridSlots[cloneSlot].position);
                    CombatManager.Instance.AddUnitView(cloneView);
                    _cloneView = cloneView;
                }
            }
        }

        Debug.Log($"[{Owner.UnitName}'s Passive] Shadow Clone đã được triệu hồi! HP: {clone.CurrentHP}/{clone.MaxHP}");

        // Text hiệu ứng
        var view = CombatManager.Instance?.GetUnitView(Owner);
        if (view != null)
            DamageTextManager.Instance?.ShowStatusText("PHÂN THÂN!", view.GetDamageTextPosition(), DamageTextManager.Instance.illusionColor, Vector2.up);
    }

    private void DestroyClone()
    {
        if (_cloneView != null)
        {
            UnityEngine.Object.Destroy(_cloneView.gameObject);
            _cloneView = null;
        }

        if (_clone != null)
        {
            CombatManager.Instance?.EnemyUnits.Remove(_clone);
            _clone = null;
        }

        Debug.Log($"[{Owner.UnitName}'s Passive] Shadow Clone đã bị tiêu diệt.");
    }
}