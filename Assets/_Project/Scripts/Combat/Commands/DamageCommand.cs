using System.Collections;

namespace Game.Combat
{
    public class DamageCommand : ICombatCommand
    {
        private readonly CombatUnit _target;
        private readonly int _damage;
        private readonly DamageType _damageType;

        public DamageCommand(CombatUnit target, int damage, DamageType damageType = DamageType.Physical)
        {
            _target = target;
            _damage = damage;
            _damageType = damageType;
        }

        public IEnumerator Execute()
        {
            if (_target != null && _target.IsAlive)
            {
                _target.TakeDamage(null, _damage, _damageType);
            }
            yield return null;
        }
    }
}