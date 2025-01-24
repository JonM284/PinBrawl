using Runtime.Character;
using UnityEngine;

namespace Runtime.GameplayInterfaces
{
    public interface IDamagable
    {
        public void OnRevive();

        public void OnHeal(float _healAmount);
        
        public void OnDealDamage(Transform _attacker, float _damageAmount, BaseCharacter _attackingCharacter = null);

        public void OnKillPlayer(Vector3 _deathPosition, Vector3 _deathDirection);
    }
}