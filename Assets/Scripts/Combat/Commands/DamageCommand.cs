using System.Collections;

namespace Game.Combat
{
    public class DamageCommand : ICombatCommand
    {
        private readonly CombatUnit _target;
        private readonly int _damage;

        public DamageCommand(CombatUnit target, int damage)
        {
            _target = target;
            _damage = damage;
        }

        public IEnumerator Execute()
        {
            if (_target != null && _target.IsAlive)
            {
                _target.TakeDamage(_damage);
            }
            yield return null;
        }
    }
}