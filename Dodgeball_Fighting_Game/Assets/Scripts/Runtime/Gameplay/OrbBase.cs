using System;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.VFX;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.Gameplay
{
    
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class OrbBase: MonoBehaviour
    {
        
        #region Serialized Fields
        
        [Header("Fields")] 

        [SerializeField] protected CharacterController cc;

        [SerializeField] protected AudioSource aSource;
        
        [Header("Ball Variables")]
        [SerializeField] protected float m_wallRayLegnth = 0.3f;

        [SerializeField] protected float m_ballSpeed = 6f;
        
        [SerializeField] protected LayerMask wallLayers;
        
        [SerializeField] protected float playerCheckRadius;
        
        [SerializeField] protected LayerMask playerCheckLayer;

        [SerializeField] protected VFXPlayer endVFX;

        [SerializeField] protected AnimationCurve m_speedCurve;

        [SerializeField] protected float m_speedBuildUpTimeMax;
        
        #endregion
        
        #region Protected Fields

        protected Vector3 m_currentWallBounceNormal, m_currentBallBouncePosition;
        
        protected Vector3 m_lastFramePosition, m_backupCheckDirection;
        
        protected Vector3 m_stageMinPosition, m_stageMaxPosition;
        
        protected float m_currentSpeed, m_speedBuildUpTimeCurrent;

        protected float m_speedPercentage;

        protected Vector3 m_moveDirection = new Vector3(1,0,1);
        
        protected Collider[] m_hitColliders = new Collider[6];
        protected int m_amountHit;

        protected string m_poolIdentifier;
        protected bool m_hasInteracted, m_isInitialized;

        #endregion

        #region Accessors

        private float m_distThreshold => 0.1f + (m_currentSpeed / m_ballSpeed);

        #endregion

        #region Unity Events
        
        private void Update()
        {
            if (!m_isInitialized || m_hasInteracted)
            {
                return;
            }

            if (m_currentSpeed < m_ballSpeed)
            {
                UpdateSpeed();
            }
            
            if (HasReachedDistance() || HasSetOffBackupCheck())
            {
                Reflect();
            }

            CheckForPlayers();
            
            m_moveDirection = m_moveDirection.normalized * (m_currentSpeed * Time.deltaTime);
            cc.Move(m_moveDirection);

            CheckPosition();
        }
        
        private void LateUpdate()
        {
            m_lastFramePosition = transform.position;
        }

        #endregion
        
        #region Class Implementation
        
        public void Initialize(Vector3 _minPosition, Vector3 _maxPosition, string _poolIdentifier)
        {
            m_stageMinPosition = _minPosition;
            m_stageMaxPosition = _maxPosition;
            m_poolIdentifier = _poolIdentifier;

            m_moveDirection = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1));
            
            m_hasInteracted = false;
            m_isInitialized = true;
        }

        protected void UpdateSpeed()
        {
            if (m_currentSpeed >= m_ballSpeed)
            {
                return;
            }

            m_currentSpeed = m_speedCurve.Evaluate(m_speedBuildUpTimeCurrent / m_speedBuildUpTimeMax);
            m_speedBuildUpTimeCurrent += Time.deltaTime;
        }

        protected void CheckForPlayers()
        {
            m_amountHit = Physics.OverlapSphereNonAlloc(transform.position, playerCheckRadius, 
                m_hitColliders, playerCheckLayer);

            if (m_amountHit == 0)
            {
                return;
            }

            for (int i = 0; i < m_amountHit; i++)
            {
                if (m_hasInteracted)
                {
                    continue;
                }
                
                m_hitColliders[i].TryGetComponent(out BaseCharacter _character);

                if (_character.IsNull())
                {
                    continue;
                }

                DoInteraction(_character);
                m_hasInteracted = true;
            }
            
        }

        protected virtual void DoInteraction(BaseCharacter _character)
        {
            
        }
        
        protected void CheckPosition()
        {
            if (!(transform.position.x > m_stageMaxPosition.x) &&
                !(transform.position.z > m_stageMaxPosition.z) &&
                !(transform.position.x < m_stageMinPosition.x) &&
                !(transform.position.z < m_stageMinPosition.z)) return;
            
            
            Debug.Log("Fixing ball position");
            transform.position = m_currentBallBouncePosition;
            Reflect();
        }
        
        private void Reflect()
        {
            m_moveDirection = Vector3.Reflect(m_moveDirection, m_currentWallBounceNormal);
            FindNextWallHitPoint(m_moveDirection);
        }

        private void FindNextWallHitPoint(Vector3 _direction)
        {
            if (!Physics.Raycast(transform.position, _direction, out RaycastHit _hit, Mathf.Infinity, wallLayers))
            {
                Debug.Log($"Something Wrong: {_direction}");
                return;
            }

            m_currentBallBouncePosition = _hit.point;
            m_currentWallBounceNormal = _hit.normal;
        }
        
        private bool HasReachedDistance()
        {
            return Vector3.SqrMagnitude(transform.position - m_currentBallBouncePosition) <= m_distThreshold;
        }

        private bool HasSetOffBackupCheck()
        {
            m_backupCheckDirection = transform.position - m_lastFramePosition;
            return Physics.Raycast(m_lastFramePosition, m_backupCheckDirection, m_backupCheckDirection.magnitude, wallLayers);
        }
        
        #endregion
        
        
        
        
    }
}