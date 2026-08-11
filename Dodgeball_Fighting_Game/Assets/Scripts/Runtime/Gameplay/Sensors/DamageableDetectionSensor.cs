using System;
using Runtime.GameplayInterfaces;
using UnityEngine;

namespace Runtime.Gameplay.Sensors
{
    [RequireComponent(typeof(SphereCollider))]
    public class DamageableDetectionSensor: MonoBehaviour
    {
        [SerializeField] private SphereCollider collider;
        
        public event Action<IDamagable> OnDamageableEnter;
        public event Action<IDamagable> OnDamageableExit;

        public void SetColliderRadius(float newRadius)
        {
            collider.radius = newRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out IDamagable damageable)) return;
            OnDamageableEnter?.Invoke(damageable);
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out IDamagable damageable)) return;
            OnDamageableExit?.Invoke(damageable);
        }
    }
}