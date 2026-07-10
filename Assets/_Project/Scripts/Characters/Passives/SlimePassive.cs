using System.Linq;
using UnityEngine;

/// <summary>
/// Nội tại của Slime: Sau đòn đánh đầu tiên trong combat, làm choáng 1 nhân vật team ta.
/// Chỉ có thể có 1 nhân vật bị choáng trong team theo cách này cùng lúc.
/// Nếu team chỉ còn 1 người thì không thể choáng.
/// </summary>
public class SlimePassive : PassiveAbility
{
    private bool _firstAttackUsed = false;
    // Stun duration = 999 (vô hạn) vì CombatManager tự clear khi player turn kết thúc
    private const int STUN_DURATION = 999;

    public override void Initialize(CombatUnit owner)
    {
        base.Initialize(owner);
        if (Owner == null) return;

        Owner.OnDealDamage += OnFirstAttack;
        Debug.Log($"[SlimePassive] {Owner.UnitName} sẽ làm choáng 1 kẻ địch sau đòn đánh đầu tiên!");
    }

    private void OnFirstAttack(CombatUnit target, int damage)
    {
        if (_firstAttackUsed) return;
        _firstAttackUsed = true; // Set ngay lập tức để tránh stack call từ AoE

        if (Owner == null || !Owner.IsAlive) return;

        // Chỉ choáng nếu target là player (kẻ địch của slime)
        if (target == null || !target.IsPlayer || !target.IsAlive) return;

        // Nếu team địch chỉ còn 1 người, không thể choáng (vì không ai có thể act)
        var alivePlayers = CombatManager.Instance.PlayerUnits.Where(p => p.IsAlive).ToList();
        if (alivePlayers.Count <= 1)
        {
            Debug.Log($"[SlimePassive] Team địch chỉ còn {alivePlayers.Count} người, không thể choáng!");
            return;
        }

        // Choáng chính mục tiêu bị slime tấn công
        target.ApplyStatus(StatusEffectType.Stun, STUN_DURATION);
        Debug.Log($"[SlimePassive] {Owner.UnitName} làm choáng {target.UnitName} trong {STUN_DURATION} lượt!");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (Owner != null)
        {
            Owner.OnDealDamage -= OnFirstAttack;
        }
    }
}