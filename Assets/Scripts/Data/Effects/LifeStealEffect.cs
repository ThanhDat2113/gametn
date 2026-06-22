using UnityEngine;

[CreateAssetMenu(fileName = "NewLifeStealEffect", menuName = "Skill/Effects/LifeSteal")]
public class LifeStealEffect : SkillEffect
{
    [Tooltip("Phần trăm sát thương chuyển thành máu")]
    public float percentage = 0.2f;

    public override void Apply(CombatUnit user, CombatUnit[] targets)
    {
        // Logic được xử lý trong LulukaPassive.cs
    }
}