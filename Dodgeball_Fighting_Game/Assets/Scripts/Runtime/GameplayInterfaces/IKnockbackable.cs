using Runtime.Character;
using Runtime.Gameplay;
using UnityEngine;

namespace Runtime.GameplayInterfaces
{
    public interface IKnockbackable
    {
        public void ApplyKnockback(Transform _attackerTransform, BaseCharacter _lastAttacker, 
            float _baseKnockbackAmount, Vector3 _forcedDirection, bool _isBallHit = false);
    }
}