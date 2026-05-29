using UnityEngine;
using System.Linq;
using Game.Combat;

namespace Game.AI.BehaviorTree
{
    public class AttackClosestEnemyNode : Node
    {
        private CombatUnit _unit;
        private float _attackRange;

        public AttackClosestEnemyNode(CombatUnit unit, float attackRange)
        {
            _unit = unit;
            _attackRange = attackRange;
        }

        public override NodeState Evaluate()
        {
            // Tìm UnitView của đơn vị hiện tại
            UnitView currentUnitView = GetViewForUnit(_unit);
            if (currentUnitView == null) return NodeState.Failure;

            // Tìm tất cả kẻ địch còn sống
            var enemies = _unit.IsPlayer ? CombatManager.Instance.EnemyUnits : CombatManager.Instance.PlayerUnits;
            var aliveEnemies = enemies.Where(e => e.IsAlive).ToList();

            if (aliveEnemies.Count == 0)
            {
                return NodeState.Failure; // Không có kẻ địch
            }

            // Tìm kẻ địch gần nhất
            CombatUnit closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in aliveEnemies)
            {
                UnitView enemyView = GetViewForUnit(enemy);
                if (enemyView == null) continue;

                float distance = Vector3.Distance(currentUnitView.transform.position, enemyView.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }

            if (closestEnemy != null && minDistance <= _attackRange)
            {
                // Tạo và thêm lệnh tấn công vào hàng đợi
                int damage = _unit.ATK; 
                CombatManager.Instance.AddCommand(new DamageCommand(closestEnemy, damage));
                return NodeState.Success;
            }

            return NodeState.Failure; // Không có kẻ địch trong tầm đánh
        }

        private UnitView GetViewForUnit(CombatUnit unit)
        {
            return CombatManager.Instance.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == unit);
        }
    }
}