using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    [TextArea] public string description;
    public SkillEffectTrigger trigger = SkillEffectTrigger.OnUse;

    // Gọi khi effect được thực thi
    // caster   = người dùng skill
    // targets  = danh sách mục tiêu
    public abstract void Apply(CombatUnit caster, CombatUnit[] targets);
}