using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Passive của Edward (Boss 2) — "Giả kim thuật hút sinh":
/// Mỗi lần gây sát thương, Edward hồi máu bằng 60% sát thương gây ra (lifesteal).
/// Edward có thể hành động 3 lần mỗi lượt (MaxActionsPerTurn = 3).
/// Mọi skill của Edward đều giảm 20% ATK mục tiêu trúng đòn trong 2 lượt (AtkDebuff.asset).
/// EdwardAI riêng: Skill 3 AoE chiếm 40% lựa chọn, Skill 1/2 mỗi skill 30%.
/// COUNTER STANCE: sau khi hành động xong 3 đòn trong lượt enemy, mọi sát thương
/// Edward nhận trong lượt player sẽ bị phản ngay bằng Skill 1 (tối đa 20 dmg/đòn) —
/// đảm bảo ít nhất 2 đòn counter khi team đánh hắn từ 2 lần trở lên.
/// </summary>
public class EdwardPassive : PassiveAbility
{
    // Lifesteal: hồi máu theo % sát thương gây ra (đang để 60% — user tự tuning).
    // Với ATK 70 × 1.5x ≈ 105 dmg/hit → hồi ~63 HP/hit trên pool 4000 HP.
    private const float HEAL_PERCENT = 0.60f;

    // ── Counter Stance (CombatManager bật sau khi Edward hành động xong lượt enemy) ──
    // Mỗi lần Edward nhận sát thương trong lượt player → phản công bằng Skill 1,
    // sát thương đòn counter KHÔNG VƯỢT QUÁ COUNTER_MAX_DAMAGE (20).
    private const int COUNTER_MAX_DAMAGE = 20;
    private bool _counterStanceActive;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        // Edward có thể act 3 lần mỗi turn
        owner.MaxActionsPerTurn = 3;
        Debug.Log($"[{Owner.UnitName}'s Passive] Giả kim hút sinh kích hoạt! Hồi {HEAL_PERCENT * 100}% sát thương gây ra. (3 actions/turn)");
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        // Lượt của Edward bắt đầu lại → tắt counter stance
        // (CombatManager sẽ bật lại ngay sau khi hắn hành động xong).
        _counterStanceActive = false;
    }

    /// <summary>
    /// Bật Counter Stance — được CombatManager gọi ngay sau khi Edward hành động xong
    /// chuỗi 3 đòn trong lượt enemy. Từ đó tới đầu lượt kế tiếp của hắn, MỖI lần nhận
    /// sát thương từ player sẽ kích hoạt 1 đòn phản công bằng Skill 1 (cap 20 dmg) qua
    /// hệ thống Interrupt → đảm bảo "ít nhất 2 đòn counter" khi team đánh hắn từ 2 lần.
    /// </summary>
    public void EnableCounterStance()
    {
        if (Owner == null || !Owner.IsAlive || _counterStanceActive) return;
        _counterStanceActive = true;
        Debug.Log($"[{Owner.UnitName}'s Passive] COUNTER STANCE BẬT! Sát thương nhận vào sẽ bị phản bằng Skill 1 (tối đa {COUNTER_MAX_DAMAGE} dmg/đòn).");
    }

    public override void OnTakeDamage(CombatUnit attacker, int damage)
    {
        if (Owner == null || !Owner.IsAlive || damage <= 0) return;
        if (!_counterStanceActive || attacker == null || !attacker.IsPlayer) return;

        // Không phản đòn ngay tại đây (tránh đè lên luồng damage đang chạy) —
        // chỉ ghi nhận yêu cầu, ProcessInterrupt sẽ thực thi ngay sau action hiện tại.
        Debug.Log($"[{Owner.UnitName}'s Passive] Bị {attacker.UnitName} đánh → YÊU CẦU PHẢN CÔNG bằng Skill 1.");
        CombatManager.Instance?.RequestInterrupt(Owner, attacker);
    }

    /// <summary>
    /// Chuẩn bị skill phản công cho CombatManager.ProcessInterrupt: bản clone runtime
    /// của Skill 1 (giữ NGUYÊN animation/VFX/SFX như đòn đánh thường) với DamageEffect
    /// clone có maxDamage = COUNTER_MAX_DAMAGE (20) → đòn counter chạy qua ResolveAction
    /// đầy đủ animation nhưng KHÔNG VƯỢT QUÁ 20. Lifesteal vẫn tự áp dụng qua OnDealDamage.
    /// Clone runtime để không đụng vào asset gốc (Damage.asset được share giữa 3 skill).
    /// </summary>
    public SkillData PrepareCounterSkill()
    {
        var skill1 = Owner != null && Owner.AvailableSkills.Count > 0 ? Owner.AvailableSkills[0] : null;
        if (skill1 == null) return null;

        var counterSkill = Object.Instantiate(skill1);
        counterSkill.skillName = skill1.skillName + " (Phản công)";

        var cappedEffects = new List<SkillEffect>();
        if (skill1.effects != null)
        {
            foreach (var effect in skill1.effects)
            {
                if (effect is DamageEffect damageEffect)
                {
                    var capped = Object.Instantiate(damageEffect);
                    capped.maxDamage = COUNTER_MAX_DAMAGE;
                    cappedEffects.Add(capped);
                }
                else
                {
                    cappedEffects.Add(effect);
                }
            }
        }
        counterSkill.effects = cappedEffects.ToArray();
        return counterSkill;
    }

    /// <summary>Text "PHẢN CÔNG!" phía trên Edward khi kích hoạt đòn phản công.</summary>
    public void PlayCounterFeedback()
    {
        var selfView = CombatManager.Instance != null ? CombatManager.Instance.GetUnitView(Owner) : null;
        if (selfView != null && DamageTextManager.Instance != null)
            DamageTextManager.Instance.ShowStatusText("PHẢN CÔNG!", selfView.GetDamageTextPosition(), DamageTextManager.Instance.ironColor, Vector2.up);
    }

    public override void OnDealDamage(CombatUnit target, int damage)
    {
        if (Owner == null || !Owner.IsAlive || damage <= 0) return;

        int heal = Mathf.RoundToInt(damage * HEAL_PERCENT);
        if (heal > 0)
        {
            Owner.Heal(heal);
            Debug.Log($"[{Owner.UnitName}'s Passive] Lifesteal: heal {heal} HP from {damage} damage dealt to {target?.UnitName}.");
        }
    }
}
