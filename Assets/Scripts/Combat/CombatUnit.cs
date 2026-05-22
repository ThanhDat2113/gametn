using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatUnit
{
    private static int nextId = 0;
    public int Id { get; private set; }
    public int GridRow { get; set; } = 2;  // 0=Back, 1=Mid, 2=Front
    public int GridSlot { get; set; } = 0;  // 0-8, vị trí trong lưới 3x3

    // ── Identity ─────────────────────────────────────────────
    public CharacterData Data { get; private set; }
    public string UnitName { get; private set; }
    public bool IsPlayer { get; private set; }
    public int Level { get; private set; }

    // ── Stats ─────────────────────────────────────────────────
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int ATK { get; private set; }
    public int PDEF { get; private set; }
    public int MDEF { get; private set; }
    public int Speed { get; private set; }

    public float CritChance { get; set; } = 0f;
    public float CritDamage { get; set; } = 1.5f;

    public bool IsAlive => CurrentHP > 0;

    // ── Buff & Status ───────────────────────────────────────
    private List<ActiveBuff> activeBuffs = new();
    private List<ActiveStatus> activeStatuses = new();

    // ── Challenge Stack ───────────────────────────────────────
    public ChallengeStack ChallengeStack { get; private set; } = new();

    // ── Round selection ───────────────────────────────────────
    public SkillData SelectedSkill { get; private set; }
    public List<CombatUnit> SelectedTargets { get; private set; } = new();

    // ── Events ────────────────────────────────────────────────
    public event System.Action<CombatUnit, int> OnDamageTaken; // (attacker, damage)
    public event System.Action<CombatUnit, int> OnDealDamage; // (target, damage)
    public event System.Action<int> OnHealed; // (amount)
    public event System.Action OnDied;
    public event System.Action<CombatUnit> OnKill; // (target)
    public event System.Action<int> OnSpendAP; // (amount)
    public event System.Action OnTurnStart;

    public void SpendAP(int amount)
    {
        OnSpendAP?.Invoke(amount);
    }

    // ── Initialize ────────────────────────────────────────────
    public void Initialize(CharacterData data, int level, bool isPlayer)
    {
        Id = nextId++;
        Data = data;
        Level = level;
        IsPlayer = isPlayer;
        UnitName = data.characterName;

        MaxHP = data.GetHP(level);
        CurrentHP = MaxHP;
        ATK = data.GetATK(level);
        PDEF = data.GetPDEF(level);
        MDEF = data.GetMDEF(level);
        Speed = data.GetSpeed(level);
    }

    // ── Damage ────────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        TakeDamage(null, amount, 0);
    }

    public void TakeDamage(CombatUnit caster, int amount, int hitIndex = 0)
    {
        // Áp dụng giảm sát thương
        int modifiedAmount = Mathf.RoundToInt(amount * GetDamageReductionMultiplier());

        int actual = Mathf.Max(1, modifiedAmount);
        CurrentHP = Mathf.Max(0, CurrentHP - actual);
        
        // Kích hoạt sự kiện
        OnDamageTaken?.Invoke(caster, actual);
        caster?.OnDealDamage?.Invoke(this, actual);

        Debug.Log($"  {UnitName} nhận {actual} dmg → HP {CurrentHP}/{MaxHP}");

        // Xử lý phản sát thương
        var reflectStatus = activeStatuses.FirstOrDefault(s => s.Type == StatusEffectType.ReflectDamage);
        if (caster != null && caster.IsAlive && reflectStatus != null)
        {
            int reflectDamage = Mathf.RoundToInt(actual * reflectStatus.Value);
            if (reflectDamage > 0)
            {
                Debug.Log($"  [{UnitName}] phản {reflectDamage} dmg lại cho [{caster.UnitName}]!");
                caster.TakeDamage(null, reflectDamage); // caster tự nhận dmg, ko có caster
            }
        }

        if (CurrentHP <= 0)
        {
            caster?.OnKill?.Invoke(this);
            OnDied?.Invoke();
        }
    }

    // ── Heal ──────────────────────────────────────────────────
    public void Heal(int amount)
    {
        int actual = Mathf.Min(amount, MaxHP - CurrentHP);
        CurrentHP += actual;
        OnHealed?.Invoke(actual);

        Debug.Log($"  {UnitName} hồi {actual} HP → HP {CurrentHP}/{MaxHP}");
    }

    // ── Skill selection ───────────────────────────────────────
    public void SelectSkill(SkillData skill, List<CombatUnit> targets)
    {
        SelectedSkill = skill;
        SelectedTargets = targets;
    }

    public void ClearSelection()
    {
        SelectedSkill = null;
        SelectedTargets.Clear();
    }

    // ── Execute skill ─────────────────────────────────────────
    public void ExecuteSelectedSkill(int apCost)
    {
        if (SelectedSkill == null || SelectedTargets.Count == 0) return;

        if (apCost > 0)
        {
            OnSpendAP?.Invoke(apCost);
        }

        Debug.Log($"[{UnitName}] dùng [{SelectedSkill.skillName}]");

        foreach (var effect in SelectedSkill.effects)
        {
            effect.Apply(this, SelectedTargets.ToArray());
        }
    }

    // ── Buff & Status Management ──────────────────────────────

    public void ApplyBuff(StatType stat, float multiplier, int duration)
    {
        // Nếu đã có buff cùng loại, reset thời gian
        var existing = activeBuffs.FirstOrDefault(b => b.Stat == stat);
        if (existing != null)
        {
            existing.Duration = duration;
        }
        else
        {
            activeBuffs.Add(new ActiveBuff(stat, multiplier, duration));
        }
        Debug.Log($"  {UnitName} nhận buff {stat} x{multiplier} ({duration} lượt)");
    }

    public void ApplyStatus(StatusEffectType status, int duration, float value = 0, int stacks = 1)
    {
        var existing = activeStatuses.FirstOrDefault(s => s.Type == status);
        if (existing != null)
        {
            existing.Duration = duration;
            existing.Value = value; 
            existing.Stacks += stacks; // Cộng dồn stack
            Debug.Log($"  [{UnitName}] trạng thái {status} cộng dồn lên {existing.Stacks} stacks ({duration} lượt, value: {value})");
        }
        else
        {
            activeStatuses.Add(new ActiveStatus(status, duration, value, stacks));
            Debug.Log($"  [{UnitName}] nhận trạng thái {status} ({duration} lượt, {stacks} stacks, value: {value})");
        }
    }

    public bool HasStatus(StatusEffectType status)
    {
        return activeStatuses.Any(s => s.Type == status);
    }

    public ActiveStatus GetActiveStatus(StatusEffectType status)
    {
        return activeStatuses.FirstOrDefault(s => s.Type == status);
    }

    public float GetStatMultiplier(StatType stat)
    {
        float total = 1f;
        foreach (var buff in activeBuffs.Where(b => b.Stat == stat))
        {
            total *= buff.Multiplier;
        }
        return total;
    }

    /// <summary>
    /// Tính tổng hệ số nhân sát thương gây ra từ các hiệu ứng (Bụi sao, Ý chí, Siêu việt, v.v.)
    /// </summary>
    public float GetDamageMultiplier()
    {
        float multiplier = 1.0f;

        // Ý chí (Lei Heng): +5% sát thương mỗi tầng
        var yChi = GetActiveStatus(StatusEffectType.YChi);
        if (yChi != null)
        {
            multiplier += yChi.Stacks * 0.05f;
        }

        // Bụi sao (Lilith): +5% sát thương mỗi tầng
        var buiSao = GetActiveStatus(StatusEffectType.BuiSao);
        if (buiSao != null)
        {
            multiplier += buiSao.Stacks * 0.05f;
        }

        // Siêu việt (Lucio): +10% sát thương mỗi tầng
        var sieuViet = GetActiveStatus(StatusEffectType.SieuViet);
        if (sieuViet != null)
        {
            multiplier += sieuViet.Stacks * 0.10f;
        }

        return multiplier;
    }

    /// <summary>
    /// Tính tổng hệ số nhân sát thương nhận vào từ các hiệu ứng (Điểm yếu, v.v.)
    /// </summary>
    public float GetDamageTakenMultiplier()
    {
        float multiplier = 1.0f;

        // Điểm yếu (Lucio): +10% sát thương nhận vào mỗi tầng
        var diemYeu = GetActiveStatus(StatusEffectType.DiemYeu);
        if (diemYeu != null)
        {
            multiplier += diemYeu.Stacks * 0.10f;
        }

        return multiplier;
    }

    /// <summary>
    /// Tính tổng hệ số giảm sát thương từ các hiệu ứng (Nội tại Celine, v.v.)
    /// </summary>
    public float GetDamageReductionMultiplier()
    {
        float reduction = 0.0f;

        // Nội tại Celine: 5% mỗi tầng, tối đa 5 tầng
        var giamSatThuong = GetActiveStatus(StatusEffectType.GiamSatThuong);
        if (giamSatThuong != null)
        {
            int stacks = Mathf.Min(giamSatThuong.Stacks, 5); // Giới hạn 5 tầng
            reduction += stacks * 0.05f;
        }

        return 1.0f - reduction; // Trả về hệ số nhân (vd: 0.75 cho 25% giảm)
    }

    // Được gọi vào cuối lượt của nhân vật này
    public void TickStatuses()
    {
        // Giảm thời gian của buff và xóa nếu hết hạn
        activeBuffs.ForEach(b => b.Duration--);
        int buffsRemoved = activeBuffs.RemoveAll(b => b.Duration <= 0);
        if (buffsRemoved > 0) Debug.Log($"  [{UnitName}] {buffsRemoved} buff đã hết hạn.");

        // Giảm thời gian của status và xóa nếu hết hạn
        activeStatuses.ForEach(s => s.Duration--);
        int statusesRemoved = activeStatuses.RemoveAll(s => s.Duration <= 0);
        if (statusesRemoved > 0) Debug.Log($"  [{UnitName}] {statusesRemoved} trạng thái đã hết hạn.");
    }
}

// Lớp lưu trữ thông tin về một buff đang hoạt động
public class ActiveBuff
{
    public StatType Stat { get; }
    public float Multiplier { get; }
    public int Duration { get; set; } // Số lượt còn lại

    public ActiveBuff(StatType stat, float multiplier, int duration)
    {
        Stat = stat;
        Multiplier = multiplier;
        Duration = duration;
    }
}

// Lớp lưu trữ thông tin về một trạng thái đặc biệt đang hoạt động
public class ActiveStatus
{
    public StatusEffectType Type { get; }
    public int Duration { get; set; }
    public float Value { get; set; } // Dùng cho các trạng thái có giá trị, vd: % phản sát thương
    public int Stacks { get; set; } // Số tầng cộng dồn

    public ActiveStatus(StatusEffectType type, int duration, float value = 0, int stacks = 1)
    {
        Type = type;
        Duration = duration;
        Value = value;
        Stacks = stacks;
    }
}