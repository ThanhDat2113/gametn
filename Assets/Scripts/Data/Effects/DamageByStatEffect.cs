using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageByStatEffect", menuName = "RPG/SkillEffect/DamageByStat")]
public class DamageByStatEffect : SkillEffect
{
    [Header("Damage Calculation")]
    public StatType scalingStat = StatType.ATK;
    public float multiplier = 1.0f;
    public DamageType damageType = DamageType.Physical;

    [Header("Targeting")]
    public bool isAoE = false;

    public override void Apply(CombatUnit caster, CombatUnit[] targets)
    {
        int baseValue = 0;
        switch (scalingStat)
        {
            case StatType.ATK:
                baseValue = caster.ATK;
                break;
            case StatType.HP:
                baseValue = caster.CurrentHP;
                break;
            case StatType.MaxHP:
                baseValue = caster.MaxHP;
                break;
            case StatType.PDEF:
                baseValue = caster.PDEF;
                break;
            case StatType.MDEF:
                baseValue = caster.MDEF;
                break;
            // Speed đã bị xóa khỏi hệ thống
        }

        // Lấy hệ số nhân từ Empowered stacks mà không xóa chúng
        float empowerMultiplier = caster.GetEmpowerMultiplier();

        int damageAmount = Mathf.RoundToInt(baseValue * multiplier);

        // Chỉ log nếu có bonus thực sự
        if (empowerMultiplier > 1f)
        {
            damageAmount = Mathf.RoundToInt(damageAmount * empowerMultiplier);
            Debug.Log($"[{caster.UnitName}]'s {scalingStat} is {baseValue}. Skill multiplier {multiplier}. Empower bonus x{empowerMultiplier:F2}. Base damage: {damageAmount}.");
        }
        else
        {
            Debug.Log($"[{caster.UnitName}]'s {scalingStat} is {baseValue}. Skill multiplier {multiplier}. No empower bonus. Base damage: {damageAmount}.");
        }


        foreach (var target in targets)
        {
            if (target.IsAlive)
            {
                int finalDamage = damageAmount;
                // You can add defense calculations here based on damageType
                if (damageType == DamageType.Physical)
                {
                    finalDamage = Mathf.Max(1, finalDamage - target.PDEF);
                }
                else if (damageType == DamageType.Magical)
                {
                    finalDamage = Mathf.Max(1, finalDamage - target.MDEF);
                }

                Debug.Log($"Applying {finalDamage} {damageType} damage to {target.UnitName}.");
                target.TakeDamage(caster, finalDamage);
            }
        }

        // Sau khi tất cả các mục tiêu đã nhận sát thương, xóa stack
        if (empowerMultiplier > 1f)
        {
            caster.ClearEmpowerStacks();
        }
    }
}