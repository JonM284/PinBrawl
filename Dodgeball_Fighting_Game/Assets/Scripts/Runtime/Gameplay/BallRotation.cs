using Project.Scripts.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.Gameplay
{
    public class BallRotation: MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField] private float rotationTimeMin = 30f; // time of rotation
        
        [SerializeField] private float rotationTimeMax = 30f; // time of rotation

        [SerializeField] private float idleRotationSpeed = 5f;
        
        #endregion
        

        #region Private Fields

        private Vector3 m_direction;

        private float m_currentRotationTime;

        private float m_randomTime;

        private Vector3 m_lastPosition;

        private BallBehavior m_ballRef;
        
        #endregion

        #region Accessors

        private BallBehavior ballRef => CommonUtils.GetRequiredComponent(ref m_ballRef, () =>
        {
            var b = GetComponentInParent<BallBehavior>();
            return b;
        });

        #endregion

        #region Unity Events

        void Start()
        {
            m_currentRotationTime = 0;
            m_randomTime = GetRandomTime();
            m_direction = GetRandomDirection();
        }

        void LateUpdate()
        {
            IdleRotation();
        }
        

        #endregion


        #region Class Implementation

        private void IdleRotation()
        {
            var perc = m_currentRotationTime / m_randomTime;
            
            if (perc <= 0.9)
            {
                m_currentRotationTime += Time.deltaTime;
                transform.Rotate(m_direction, idleRotationSpeed * Time.deltaTime);
            }
            else
            {
                m_direction = GetRandomDirection();
                m_currentRotationTime = 0;
                m_randomTime = GetRandomTime();
            }
        }

        Vector3 GetRandomDirection()
        {
            return Random.insideUnitSphere.normalized;
        }

        float GetRandomTime()
        {
            return Random.Range(rotationTimeMin, rotationTimeMax);
        }

        #endregion
        
    }
}