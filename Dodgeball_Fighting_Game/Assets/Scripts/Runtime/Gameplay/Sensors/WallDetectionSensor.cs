using System;
using UnityEngine;

namespace Runtime.Gameplay.Sensors
{
    [RequireComponent(typeof(SphereCollider))]
    public class WallDetectionSensor: MonoBehaviour
    {
        [SerializeField] private SphereCollider collider;
        
        public event Action OnWallHit;

        public void SetColliderRadius(float newRadius)
        {
            collider.radius = newRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.gameObject.layer != LayerMask.NameToLayer("Wall")) return;
            OnWallHit?.Invoke();
        }
    }
}