using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseAPEffect", menuName = "RPG/Effects/Increase AP")]
public class IncreaseAPEffect : SkillEffect
{
    public int amount = 1;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        // TODO: Sẽ implement sau khi có hệ thống AP
        Debug.Log($"[IncreaseAP] {caster.UnitName} would gain {amount} AP, but not implemented yet.");
        // if (CombatManager.Instance != null)
        // {
        //     CombatManager.Instance.AddAP(caster, amount);
        // }
    }
}