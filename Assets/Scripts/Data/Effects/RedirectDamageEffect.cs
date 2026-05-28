using UnityEngine;

[CreateAssetMenu(fileName = "NewRedirectDamageEffect", menuName = "Skill/Effects/RedirectDamage")]
public class RedirectDamageEffect : SkillEffect
{
    [Tooltip("Phần trăm sát thương chuyển hướng")]
    public float percentage = 0.3f;

    public override void Apply(CombatUnit user, CombatUnit[] targets)
    {
        // Logic được xử lý trong KlarisPassive.cs
    }
}