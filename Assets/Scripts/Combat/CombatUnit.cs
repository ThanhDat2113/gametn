using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Combat;

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
    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    private List<ActiveStatus> activeStatuses = new List<ActiveStatus>();

    // ── Challenge Stack ───────────────────────────────────────
    public ChallengeStack ChallengeStack { get; private set; } = new();

    // ── Round selection ───────────────────────────────────────
    public List<SkillData> AvailableSkills { get; private set; } = new();
    public SkillData SelectedSkill { get; private set; }
    public List<CombatUnit> SelectedTargets { get; private set; } = new();
    public PassiveAbility Passive { get; private set; }
    private PlannedAction _plannedAction;

    // ── Events ────────────────────────────────────────────────
    public event System.Action<CombatUnit, int> OnDamageTaken; // (attacker, damage)
    public event System.Action<CombatUnit, int> OnDealDamage; // (target, damage)
    public event System.Action<int> OnHealed; // (amount)
    public event System.Action OnDied;
    public event System.Action<CombatUnit> OnKill; // (target)
    public event System.Action<int> OnSpendAP; // (amount)
    public event System.Action<SkillData, List<CombatUnit>> OnActionConfirmed; // (skill, targets)

    public void RaiseActionConfirmed(SkillData skill, List<CombatUnit> targets)
    {
        OnActionConfirmed?.Invoke(skill, targets);
    }

    public event System.Action OnTurnStart;

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

        // Instantiate skills to make them unique to this unit
        AvailableSkills.Clear();
        if (data.skills != null)
        {
            foreach (var skillAsset in data.skills)
            {
                if (skillAsset != null)
                {
                    var skillInstance = Object.Instantiate(skillAsset);
                    skillInstance.name = skillAsset.name; // Remove "(Clone)" from name
                    AvailableSkills.Add(skillInstance);
                }
            }
        }
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
        Passive?.OnTakeDamage(caster, actual);
        caster?.Passive?.OnDealDamage(this, actual);

        Debug.Log($"  {UnitName} nhận {actual} dmg → HP {CurrentHP}/{MaxHP}");

        // Xử lý phản sát thương
        var reflectStatus = GetActiveStatus(StatusEffectType.ReflectDamage);
        if (caster != null && caster.IsAlive && reflectStatus != null)
        {
            int reflectDamage = Mathf.RoundToInt(actual * reflectStatus.Value);
            if (reflectDamage > 0)
            {
                Debug.Log($"  [{UnitName}] phản {reflectDamage} dmg lại cho [{caster.UnitName}]!");
                caster.TakeDamage(null, reflectDamage);
            }
        }

        if (CurrentHP <= 0)
        {
            caster?.OnKill?.Invoke(this);
            caster?.Passive?.OnKill(this);
            OnDied?.Invoke();
            Passive?.OnDied();
        }
    }

    // ── Heal ──────────────────────────────────────────────────
    public void Heal(int amount)
    {
        int actual = Mathf.Min(amount, MaxHP - CurrentHP);
        CurrentHP += actual;
        OnHealed?.Invoke(actual);
        Passive?.OnHeal(actual);
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

    public void SetPlannedAction(PlannedAction action)
    {
        _plannedAction = action;
    }

    public PlannedAction GetPlannedAction()
    {
        var action = _plannedAction;
        _plannedAction = null; // Clear action after getting it
        return action;
    }

    // ── Execute skill ─────────────────────────────────────────
    public void ExecuteSelectedSkill(int apCost)
    {
        if (SelectedSkill == null || SelectedTargets.Count == 0) return;
        if (apCost > 0) OnSpendAP?.Invoke(apCost);
        Debug.Log($"[{UnitName}] dùng [{SelectedSkill.skillName}]");
        foreach (var effect in SelectedSkill.effects)
        {
            Debug.Log($"[{UnitName}] Chuẩn bị áp dụng hiệu ứng: {effect.GetType().Name}");
            effect.Apply(this, SelectedTargets.ToArray());
            Debug.Log($"[{UnitName}] Đã áp dụng hiệu ứng: {effect.GetType().Name}");
        }
    }

    // ── Buff & Status Management ──────────────────────────────

    public void ApplyBuff(StatType stat, float multiplier, int duration)
    {
        var existing = activeBuffs.Find(b => b.Stat == stat);
        if (existing != null)
        {
            if (duration != 0) existing.Duration = duration;
            existing.Multiplier = multiplier;
        }
        else
        {
            activeBuffs.Add(new ActiveBuff(stat, multiplier, duration));
        }
        Debug.Log($"  {UnitName} nhận buff {stat} x{multiplier} ({(duration == 0 ? "vĩnh viễn" : duration + " lượt")})");
    }

    public void ApplyStatus(StatusEffectType status, int duration, float value = 0, int stacks = 1)
    {
        var existing = activeStatuses.Find(s => s.Type == status);
        if (existing != null)
        {
            if (duration != 0) existing.Duration = duration;
            existing.Value = value;
            existing.Stacks += stacks;
            Debug.Log($"  [{UnitName}] trạng thái {status} cộng dồn lên {existing.Stacks} stacks");
        }
        else
        {
            activeStatuses.Add(new ActiveStatus(status, duration, value, stacks));
            Debug.Log($"  [{UnitName}] nhận trạng thái {status} ({(duration == 0 ? "vĩnh viễn" : duration + " lượt")}, {stacks} stacks, value: {value})");
        }
    }

    public ActiveStatus GetActiveStatus(StatusEffectType type)
    {
        return activeStatuses.Find(s => s.Type == type);
    }

    public bool HasStatus(StatusEffectType type)
    {
        return activeStatuses.Exists(s => s.Type == type);
    }

    public float GetStatMultiplier(StatType stat)
    {
        float total = 1f;
        foreach (var buff in activeBuffs.Where(b => b.Stat == stat))
            total *= buff.Multiplier;
        return total;
    }

    public float GetDamageMultiplier()
    {
        float multiplier = 1f;
        var sieuViet = GetActiveStatus(StatusEffectType.SieuViet);
        if (sieuViet != null) multiplier += sieuViet.Stacks * sieuViet.Value;
        var buiSao = GetActiveStatus(StatusEffectType.BuiSao);
        if (buiSao != null) multiplier += buiSao.Stacks * buiSao.Value;
        var yChi = GetActiveStatus(StatusEffectType.YChi);
        if (yChi != null) multiplier += yChi.Stacks * yChi.Value;
        return multiplier;
    }

    public float GetDamageTakenMultiplier()
    {
        float multiplier = 1f;
        var diemYeu = GetActiveStatus(StatusEffectType.DiemYeu);
        if (diemYeu != null) multiplier += diemYeu.Stacks * diemYeu.Value;
        return multiplier;
    }

    public float GetDamageReductionMultiplier()
    {
        float reduction = 0f;
        var giamSatThuong = GetActiveStatus(StatusEffectType.GiamSatThuong);
        if (giamSatThuong != null)
        {
            int stacks = Mathf.Min(giamSatThuong.Stacks, 5);
            reduction += stacks * giamSatThuong.Value;
        }
        return 1f - reduction;
    }

    public void SpendAP(int amount)
    {
        OnSpendAP?.Invoke(amount);
        Passive?.OnSpendAP(amount);
    }

    public void TriggerTurnStart()
    {
        OnTurnStart?.Invoke();
        Passive?.OnTurnStart();
    }

    public void TickStatuses()
    {
        // Buffs
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].Duration == 0) continue;
            activeBuffs[i].Duration--;
            if (activeBuffs[i].Duration <= 0)
                activeBuffs.RemoveAt(i);
        }

        // Statuses
        for (int i = activeStatuses.Count - 1; i >= 0; i--)
        {
            if (activeStatuses[i].Duration == 0) continue;
            activeStatuses[i].Duration--;
            if (activeStatuses[i].Duration <= 0)
                activeStatuses.RemoveAt(i);
        }
    }

    public void SetPassive(PassiveAbility passive)
    {
        Passive = passive;
        if (Passive != null)
        {
            Passive.Initialize(this);
        }
    }

    /// <summary>
    /// Lấy hệ số nhân sát thương từ Empowered stacks mà KHÔNG tiêu thụ chúng.
    /// </summary>
    public float GetEmpowerMultiplier()
    {
        var empowerStatus = GetActiveStatus(StatusEffectType.Empowered);
        if (empowerStatus != null)
        {
            int stacks = empowerStatus.Stacks;
            float bonusPerStack = empowerStatus.Value;
            return 1f + (stacks * bonusPerStack);
        }
        return 1f;
    }

    /// <summary>
    /// Xóa tất cả các stack Empowered.
    /// </summary>
    public void ClearEmpowerStacks()
    {
        var empowerStatus = GetActiveStatus(StatusEffectType.Empowered);
        if (empowerStatus != null)
        {
            activeStatuses.Remove(empowerStatus);
            Debug.Log($"[{UnitName}] đã xóa {empowerStatus.Stacks} stack Empowered sau khi tấn công.");
        }
    }
}

// Lớp lưu trữ buff
public class ActiveBuff
{
    public StatType Stat { get; }
    public float Multiplier { get; set; }
    public int Duration { get; set; }

    public ActiveBuff(StatType stat, float multiplier, int duration)
    {
        Stat = stat;
        Multiplier = multiplier;
        Duration = duration;
    }
}

// Lớp lưu trữ trạng thái
public class ActiveStatus
{
    public StatusEffectType Type { get; }
    public int Duration { get; set; }
    public float Value { get; set; }
    public int Stacks { get; set; }

    public ActiveStatus(StatusEffectType type, int duration, float value = 0, int stacks = 1)
    {
        Type = type;
        Duration = duration;
        Value = value;
        Stacks = stacks;
    }
}