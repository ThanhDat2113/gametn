using UnityEngine;

[CreateAssetMenu(fileName = "ShieldEffect", menuName = "RPG/Effects/Shield")]
public class ShieldEffect : SkillEffect
{
    public float shieldPercent = 0.2f;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        foreach (var target in targets)
        {
            int shieldAmount = Mathf.RoundToInt(target.MaxHP * shieldPercent);
            // Bạn cần thêm biến `currentShield` trong CombatUnit và phương thức ApplyShield
            // target.ApplyShield(shieldAmount);
            Debug.Log($"[Shield] {target.UnitName} gains {shieldAmount} shield.");
        }
    }
}