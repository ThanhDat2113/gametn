using UnityEngine;

[CreateAssetMenu(fileName = "ExtraTurnEffect", menuName = "RPG/Effects/Extra Turn")]
public class ExtraTurnEffect : SkillEffect
{
    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // TODO: Sẽ implement sau khi có hệ thống Extra Turn
        Debug.Log($"[ExtraTurn] {caster.UnitName} would get an extra turn, but not implemented yet.");
        // if (CombatManager.Instance != null)
        // {
        //     CombatManager.Instance.GrantExtraTurn(caster);
        // }
    }
}