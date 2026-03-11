using System.Collections.Generic;
using UnityEngine;

public class CombatUnit
{
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
    public int Luck { get; private set; }

    public float BaseCritRate { get; private set; } = 0.05f;
    public float BaseCritDmg { get; private set; } = 1.50f;

    public bool IsAlive => CurrentHP > 0;

    // ── Cooldowns ─────────────────────────────────────────────
    public int[] SkillCooldowns { get; private set; }

    // ── Buff multipliers ──────────────────────────────────────
    private Dictionary<StatType, float> buffMultipliers = new();

    // ── Challenge Stack ───────────────────────────────────────
    public ChallengeStack ChallengeStack { get; private set; } = new();

    // ── Round selection ───────────────────────────────────────
    public SkillData SelectedSkill { get; private set; }
    public List<CombatUnit> SelectedTargets { get; private set; } = new();

    // ── Events ────────────────────────────────────────────────
    public event System.Action<int, int> OnDamageTaken; // (damage, hitIndex)
    public event System.Action<int> OnHealed;
    public event System.Action OnDied;

    // ── Initialize ────────────────────────────────────────────
    public void Initialize(CharacterData data, int level, bool isPlayer)
    {
        Data = data;
        Level = level;
        IsPlayer = isPlayer;
        UnitName = data.characterName;

        MaxHP = data.GetHP(level);
        CurrentHP = MaxHP;
        ATK = data.GetATK(level);
        PDEF = data.GetPDEF(level);
        MDEF = data.GetMDEF(level);
        Luck = data.GetLuck(level);

        SkillCooldowns = new int[data.skills.Length];
    }

    // ── Damage ────────────────────────────────────────────────
    public void TakeDamage(int amount, int hitIndex = 0)
    {
        int actual = Mathf.Max(1, amount);
        CurrentHP = Mathf.Max(0, CurrentHP - actual);
        OnDamageTaken?.Invoke(actual, hitIndex);

        Debug.Log($"  {UnitName} nhận {actual} dmg → HP {CurrentHP}/{MaxHP}");

        if (CurrentHP <= 0) OnDied?.Invoke();
    }

    // ── Heal ──────────────────────────────────────────────────
    public void Heal(int amount)
    {
        int actual = Mathf.Min(amount, MaxHP - CurrentHP);
        CurrentHP += actual;
        OnHealed?.Invoke(actual);

        Debug.Log($"  {UnitName} hồi {actual} HP → HP {CurrentHP}/{MaxHP}");
    }

    // ── Buff ──────────────────────────────────────────────────
    public void AddBuff(StatType stat, float multiplier, int duration)
    {
        if (buffMultipliers.ContainsKey(stat))
            buffMultipliers[stat] *= multiplier;
        else
            buffMultipliers[stat] = multiplier;

        Debug.Log($"  {UnitName} buff {stat} x{multiplier} ({duration} turns)");
    }

    public float GetBuffMultiplier(StatType stat) =>
        buffMultipliers.TryGetValue(stat, out float v) ? v : 1f;

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

    // ── Cooldown ──────────────────────────────────────────────
    public bool IsSkillReady(int index) =>
        index < SkillCooldowns.Length && SkillCooldowns[index] <= 0;

    public void PutOnCooldown(int index)
    {
        if (index < SkillCooldowns.Length)
            SkillCooldowns[index] = Data.skills[index].cooldown;
    }

    public void TickCooldowns()
    {
        for (int i = 0; i < SkillCooldowns.Length; i++)
            if (SkillCooldowns[i] > 0) SkillCooldowns[i]--;
    }

    // ── Execute skill ─────────────────────────────────────────
    public void ExecuteSelectedSkill()
    {
        if (SelectedSkill == null || SelectedTargets.Count == 0) return;

        Debug.Log($"[{UnitName}] dùng [{SelectedSkill.skillName}]");

        foreach (var effect in SelectedSkill.effects)
            effect.Apply(this, SelectedTargets.ToArray());
    }
}