using System;
using Runtime.Character;
using UnityEngine;

namespace Runtime.Gameplay.Sensors
{
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerDetectionSensor: MonoBehaviour
    {

        [SerializeField] private SphereCollider collider;
        
        public event Action<BaseCharacter> OnCharacterEnter;
        public event Action<BaseCharacter> OnCharacterExit;

        public void SetColliderRadius(float newRadius)
        {
            collider.radius = newRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out BaseCharacter baseCharacter)) return;
            OnCharacterEnter?.Invoke(baseCharacter);
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out BaseCharacter baseCharacter)) return;
            OnCharacterExit?.Invoke(baseCharacter);
        }
    }
}