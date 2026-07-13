using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Combat;

public class CombatUnit
{
    private static int nextId = 0;
    public int Id { get; private set; }
    public int GridRow { get; set; } = 2;
    public int GridSlot { get; set; } = 0;

    public CharacterData Data { get; set; }
    public string UnitName { get; set; }
    public bool IsPlayer { get; set; }
    public int Level { get; set; }

    public int MaxHP { get; set; }
    public int CurrentHP { get; set; }
    public int ATK { get; set; }
    public int PDEF { get; set; }
    public int MDEF { get; set; }

    public float CritChance { get; set; } = 0f;
    public float CritDamage { get; set; } = 1.5f;
    public float ArmorPenetration { get; set; } = 0f;

    public bool IsAlive => CurrentHP > 0;
    public bool IgnoreTaunt { get; set; } = false;
    public bool AlwaysActsFirst { get; set; } = false;
    public bool HasActedThisTurn { get; set; } = false;
    public int MaxActionsPerTurn { get; set; } = 1;
    public int ActionsRemainingThisTurn { get; set; } = 1;
    public bool CanActThisTurn => ActionsRemainingThisTurn > 0 && IsAlive && !HasStatus(StatusEffectType.Stun);
    public bool IsTargetable { get; set; } = true;
    public string PassiveClassName { get; set; } // để lưu tên class passive sau khi spawn

    private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    private List<ActiveStatus> activeStatuses = new List<ActiveStatus>();

    private int _damageReductionCharges = 0;
    private float _damageReductionPercent = 0f;
    public int DamageReductionChargesRemaining => _damageReductionCharges;

    public void AddDamageReductionCharges(int charges, float percent)
    {
        _damageReductionCharges += charges;
        _damageReductionPercent = percent;
        Debug.Log($"[{UnitName}] Nhận {charges} lớp giáp, mỗi lớp giảm {percent*100}% sát thương.");
    }

    public ChallengeStack ChallengeStack { get; private set; } = new();

    public List<SkillData> AvailableSkills { get; set; } = new();
    public SkillData SelectedSkill { get; private set; }
    public List<CombatUnit> SelectedTargets { get; private set; } = new();
    public PassiveAbility Passive { get; private set; }
    private PlannedAction _plannedAction;

    // ── Events ── ✅ ĐÃ SỬA: thêm DamageType
    public event System.Action<CombatUnit, int, DamageType> OnDamageTaken;
    public event System.Action<CombatUnit, int> OnDealDamage;
    public event System.Action<int> OnHealed;
    public event System.Action OnDied;
    public event System.Action<CombatUnit> OnKill;
    public event System.Action<int> OnSpendAP;
    public event System.Action<CombatUnit, SkillData, List<CombatUnit>> OnActionConfirmed;
    public event System.Action OnTurnStart;

    public void RaiseActionConfirmed(SkillData skill, List<CombatUnit> targets)
    {
        OnActionConfirmed?.Invoke(this, skill, targets);
    }

    public void Initialize(CharacterData data, int level, bool isPlayer,
        int hpBonus = 0, int atkBonus = 0, int pdefBonus = 0, int mdefBonus = 0)
    {
        Id = nextId++;
        Data = data;
        Level = level;
        IsPlayer = isPlayer;
        UnitName = data.characterName;

        MaxHP = data.GetHP(level) + hpBonus;
        CurrentHP = MaxHP;
        ATK = data.GetATK(level) + atkBonus;
        PDEF = data.GetPDEF(level) + pdefBonus;
        MDEF = data.GetMDEF(level) + mdefBonus;

        AvailableSkills.Clear();
        if (data.skills != null)
        {
            foreach (var skillAsset in data.skills)
            {
                if (skillAsset != null)
                {
                    var skillInstance = Object.Instantiate(skillAsset);
                    skillInstance.name = skillAsset.name;
                    
                    // Deep clone array references để tránh các skill clone share cùng mảng âm thanh/VFX
                    if (skillAsset.sfxClips != null)
                        skillInstance.sfxClips = (AudioClip[])skillAsset.sfxClips.Clone();
                    if (skillAsset.vfxEvents != null)
                        skillInstance.vfxEvents = (VFXEvent[])skillAsset.vfxEvents.Clone();
                    if (skillAsset.hitVfxEvents != null)
                        skillInstance.hitVfxEvents = (VFXEvent[])skillAsset.hitVfxEvents.Clone();
                    if (skillAsset.rangedVfxEvents != null)
                        skillInstance.rangedVfxEvents = (VFXEvent[])skillAsset.rangedVfxEvents.Clone();
                    if (skillAsset.voiceLines != null)
                        skillInstance.voiceLines = (AudioClip[])skillAsset.voiceLines.Clone();
                    if (skillAsset.effects != null)
                        skillInstance.effects = (SkillEffect[])skillAsset.effects.Clone();
                    
                    AvailableSkills.Add(skillInstance);
                }
            }
        }
    }

    // ── Damage ── ✅ ĐÃ SỬA: truyền DamageType
    public void TakeDamage(CombatUnit caster, int amount, DamageType damageType = DamageType.Physical)
    {
        // Invincible: chặn mọi sát thương, sau đó tự xóa
        if (HasStatus(StatusEffectType.Invincible))
        {
            Debug.Log($"  {UnitName} đang Invincible! Chặn {amount} dmg.");
            ClearStatus(StatusEffectType.Invincible);
            return;
        }

        int actualDamage = amount;
        bool isTrueDamage = (damageType == DamageType.True);

        if (!isTrueDamage)
        {
            if (_damageReductionCharges > 0)
            {
                actualDamage = Mathf.RoundToInt(amount * (1f - _damageReductionPercent));
                _damageReductionCharges--;
                Debug.Log($"[{UnitName}] Lớp giáp chặn! Còn {_damageReductionCharges} lớp. Sát thương: {amount} → {actualDamage}");
            }
            else
            {
                actualDamage = Mathf.RoundToInt(amount * GetDamageReductionMultiplier());
            }
        }

        actualDamage = Mathf.Max(1, actualDamage);
        CurrentHP = Mathf.Max(0, CurrentHP - actualDamage);

        // ✅ Kích hoạt sự kiện với DamageType
        OnDamageTaken?.Invoke(caster, actualDamage, damageType);
        caster?.OnDealDamage?.Invoke(this, actualDamage);
        Passive?.OnTakeDamage(caster, actualDamage);
        caster?.Passive?.OnDealDamage(this, actualDamage);

        Debug.Log($"  {UnitName} nhận {actualDamage} dmg (Type: {damageType}) → HP {CurrentHP}/{MaxHP}");

        if (!isTrueDamage)
        {
            var reflectStatus = GetActiveStatus(StatusEffectType.ReflectDamage);
            if (caster != null && caster.IsAlive && reflectStatus != null)
            {
                int reflectDamage = Mathf.RoundToInt(actualDamage * reflectStatus.Value);
                if (reflectDamage > 0)
                {
                    Debug.Log($"  [{UnitName}] phản {reflectDamage} dmg lại cho [{caster.UnitName}]!");
                    caster.TakeDamage(null, reflectDamage, DamageType.True);
                }
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

    // Overload đơn giản để tương thích
    public void TakeDamage(int amount)
    {
        TakeDamage(null, amount, DamageType.Physical);
    }

    // ── Các phương thức khác giữ nguyên ──
    public bool IsAlly(CombatUnit other) => this.IsPlayer == other.IsPlayer;

    public void Heal(int amount)
    {
        int actual = Mathf.Min(amount, MaxHP - CurrentHP);
        CurrentHP += actual;
        OnHealed?.Invoke(actual);
        Passive?.OnHeal(actual);
        Debug.Log($"  {UnitName} hồi {actual} HP → HP {CurrentHP}/{MaxHP}");
    }

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
        _plannedAction = null;
        return action;
    }

    public void ExecuteSelectedSkill(int apCost)
    {
        if (SelectedSkill == null || SelectedTargets.Count == 0) return;
        if (apCost > 0) OnSpendAP?.Invoke(apCost);
        Debug.Log($"[{UnitName}] dùng [{SelectedSkill.skillName}]");
        foreach (var effect in SelectedSkill.effects)
        {
            effect.Apply(this, SelectedTargets.ToArray());
        }
    }

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

    public void ClearStatus(StatusEffectType type)
    {
        activeStatuses.RemoveAll(s => s.Type == type);
    }

    public bool HasStatus(StatusEffectType type)
    {
        return activeStatuses.Exists(s => s.Type == type);
    }

    public bool HasAnyDebuff()
    {
        var debuffTypes = new HashSet<StatusEffectType>
        {
            StatusEffectType.Stun,
            StatusEffectType.Taunt,
            StatusEffectType.ThieuDot,
            StatusEffectType.DiemYeu
        };
        return activeStatuses.Any(s => debuffTypes.Contains(s.Type));
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
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].Duration == 0) continue;
            activeBuffs[i].Duration--;
            if (activeBuffs[i].Duration <= 0)
                activeBuffs.RemoveAt(i);
        }
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
        if (Passive != null) Passive.Initialize(this);
    }

    public void RecalculateStatsForLevel(int newLevel)
    {
        Level = newLevel;
        int oldMaxHP = MaxHP;
        MaxHP = Data.GetHP(Level);
        CurrentHP += (MaxHP - oldMaxHP);
        ATK = Data.GetATK(Level);
        PDEF = Data.GetPDEF(Level);
        MDEF = Data.GetMDEF(Level);
        Debug.Log($"[{UnitName}] Level Up stats: HP {MaxHP} | ATK {ATK} | PDEF {PDEF} | MDEF {MDEF}");
    }

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

    public void ClearEmpowerStacks()
    {
        var empowerStatus = GetActiveStatus(StatusEffectType.Empowered);
        if (empowerStatus != null)
        {
            activeStatuses.Remove(empowerStatus);
            Debug.Log($"[{UnitName}] đã xóa {empowerStatus.Stacks} stack Empowered.");
        }
    }
}

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