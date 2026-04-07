using System;
using UnityEngine;

namespace Runtime.Gameplay.Sensors
{
    [RequireComponent(typeof(SphereCollider))]
    public class BallDetectionSensor: MonoBehaviour
    {
        [SerializeField] private SphereCollider collider;
        
        public event Action<BallBehavior> OnBallEnter;
        public event Action<BallBehavior> OnBallExit;

        public void SetColliderRadius(float newRadius)
        {
            collider.radius = newRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out BallBehavior ball)) return;
            OnBallEnter?.Invoke(ball);
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out BallBehavior ball)) return;
            OnBallExit?.Invoke(ball);
        }
    }
}