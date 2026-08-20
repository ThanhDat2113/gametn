using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Passive của Goblin Bé (Map 2):
/// "Bầy Đàn" — Mỗi đồng minh còn sống (kể cả bản thân) tăng 10% sát thương.
/// </summary>
public class GoblinPassive : PassiveAbility
{
    private const float DMG_PER_ALLY = 0.10f;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        if (Owner == null) return;

        owner.OnActionConfirmed += OnAction;
        Debug.Log($"[GoblinPassive] {Owner.UnitName} Bầy Đàn! Mỗi đồng minh còn sống tăng 10% sát thương.");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnActionConfirmed -= OnAction;
        }
    }

    private void OnAction(CombatUnit caster, SkillData skill, List<CombatUnit> targets)
    {
        if (Owner == null || !Owner.IsAlive) return;
        if (caster != Owner) return;

        int alliesAlive = CombatManager.Instance.EnemyUnits.Count(u => u.IsAlive);
        float multiplier = 1f + (alliesAlive - 1) * DMG_PER_ALLY;
        Owner.ApplyBuff(StatType.ATK, multiplier, 1);
        Debug.Log($"[GoblinPassive] {Owner.UnitName} Bầy Đàn! {alliesAlive} đồng minh → sát thương x{multiplier:F2}");

        // Text hiệu ứng
        var view = CombatManager.Instance?.GetUnitView(Owner);
        if (view != null)
            DamageTextManager.Instance?.ShowStatusText("BẦY ĐÀN!", view.GetDamageTextPosition(), DamageTextManager.Instance.packColor, Vector2.up);
    }
}