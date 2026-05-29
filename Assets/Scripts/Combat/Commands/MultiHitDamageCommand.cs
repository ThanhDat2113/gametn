using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game.Combat
{
    public class MultiHitDamageCommand : ICombatCommand
    {
        private readonly CombatUnit _actor;
        private readonly CombatUnit _target;
        private readonly int _totalDamage;
        private readonly int _hitCount;

        public MultiHitDamageCommand(CombatUnit actor, CombatUnit target, int totalDamage, int hitCount)
        {
            _actor = actor;
            _target = target;
            _totalDamage = totalDamage;
            _hitCount = hitCount <= 0 ? 1 : hitCount;
        }

        public IEnumerator Execute()
        {
            // Trả về Coroutine để CombatManager có thể đợi nó hoàn thành
            return ExecuteMultiHit();
        }

        private IEnumerator ExecuteMultiHit()
        {
            if (_target == null || !_target.IsAlive)
                yield break;

            int damagePerHit = _totalDamage / _hitCount;
            int damageDealtSoFar = 0;

            // Lấy vị trí của mục tiêu
            UnitView targetView = GetViewForUnit(_target);
            if (targetView == null) yield break;
            Vector3 targetPosition = targetView.transform.position + Vector3.up * 2; // Vị trí số bay lên

            // Thực hiện các đòn đánh "ảo" để hiển thị số
            for (int i = 0; i < _hitCount - 1; i++)
            {
                int currentHitDamage = Mathf.Min(damagePerHit, _totalDamage - damageDealtSoFar);
                if (currentHitDamage <= 0) break;

                // Hiển thị số sát thương
                DamageTextManager.Instance.ShowDamage(currentHitDamage, targetPosition, false, false);
                
                damageDealtSoFar += currentHitDamage;
                yield return new WaitForSeconds(0.1f); // Khoảng cách giữa các hit
            }

            // Đòn cuối cùng: Gây sát thương thực sự và hiển thị số bùng nổ
            int finalDamage = _totalDamage - damageDealtSoFar;
            if (finalDamage > 0)
            {
                // Gói sát thương thực sự vào một DamageCommand
                var finalHitCommand = new DamageCommand(_target, finalDamage);
                finalHitCommand.Execute(); // Thực thi ngay lập tức
                
                // Hiển thị số sát thương cuối cùng
                DamageTextManager.Instance.ShowDamage(finalDamage, targetPosition, true, false);
            }
        }
        
        private UnitView GetViewForUnit(CombatUnit unit)
        {
            return CombatManager.Instance.GetAllUnitViews().FirstOrDefault(v => v.LinkedUnit == unit);
        }
    }
}