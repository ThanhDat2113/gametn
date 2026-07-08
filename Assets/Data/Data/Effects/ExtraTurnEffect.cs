using UnityEngine;

[CreateAssetMenu(fileName = "ExtraTurnEffect", menuName = "RPG/Effects/Extra Action")]
public class ExtraTurnEffect : SkillEffect
{
    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // Cho caster act thêm 1 lần nữa trong lượt hiện tại
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.GrantExtraAction(caster);
            Debug.Log($"[ExtraAction] {caster.UnitName} được act thêm lần nữa!");
        }
    }
}