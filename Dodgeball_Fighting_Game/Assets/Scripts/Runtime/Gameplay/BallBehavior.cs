using System;
using System.Collections.Generic;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.Gameplay
{
    
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class BallBehavior: MonoBehaviour
    {

        #region Nested-Classes

        [Serializable]
        public class BallTrajectory
        {
            public Vector3 startPos;
            public Vector3 endPos;
            public Vector3 direction;
            public float maxTime;

            public BallTrajectory(Vector3 _startPos, Vector3 _endPos)
            {
                startPos = _startPos;
                endPos = _endPos;
                direction = endPos - startPos;
            }
        }

        #endregion

        #region Read-Only

        private static readonly int outlineColorName = Shader.PropertyToID("_OutlineColor");

        #endregion

        #region Actions

        public static event Action<BaseCharacter> OnBallSwap;

        #endregion
        
        #region Serialized Fields

        [Header("Fields")] 

        [SerializeField] private CharacterController cc;

        [SerializeField] private AudioSource aSource, bSource;

        [SerializeField] private List<AudioClip> m_hitBallSFX = new List<AudioClip>();
        [SerializeField] private List<AudioClip> m_wallHitSFX = new List<AudioClip>();

        [SerializeField] private float m_ballSpeedReduceRate = 1f;
        
        [Header("Ball Variables")]
        [SerializeField]
        private float m_wallRayLegnth = 0.3f;

        [SerializeField] private float m_ballMinSpeed = 6f;

        [SerializeField] private float m_ballMaxSpeed = 100f;

        [SerializeField] private float m_ballLightHit = 75f;

        [SerializeField] private float m_ballMediumHit = 110f;

        [SerializeField] private float m_ballHeavyHit = 150f;
        
        [SerializeField] private LayerMask wallLayers;

        [Space(15)] [Header("Player Interaction")]
        [SerializeField] private float m_ballDamageAmount = 100;

        [SerializeField] private float m_ballMaxKnockback = 40f;
        
        [SerializeField] private float playerCheckRadius;
        
        [SerializeField] private LayerMask playerCheckLayer;

        [Header("Visuals")] 
        [SerializeField] private MeshRenderer m_ballVisuals;

        [SerializeField] private TrailRenderer m_trail;

        [SerializeField] private Color m_neutralColor;

        [SerializeField] private Color m_buntedColor;
        
        #endregion
        
        #region Private Fields

        private Vector3 m_ballMoveDirection = new Vector3(1,0,1);

        private BaseCharacter m_lastWackCharacter;

        private BaseCharacter m_buntingCharacter;

        private BallTrajectory m_currentMotionPath;

        private BallTrajectory m_nextMotionPath;
        
        private float m_strengthMaxAmount = 0.1f, m_maxRandomness = 10f;

        private float m_buntTimerMax = 3f, m_currentBuntTimer;

        private Vector3 m_currentBallBouncePosition;

        private Vector3 m_currentWallBounceNormal;

        private Vector3 m_lastFramePosition;

        private List<BaseCharacter> m_recentlyHitCharacters = new List<BaseCharacter>();

        private float m_currentSpeed;

        private float m_ballHitEnergyAddAmount = 5f;

        private bool m_isBunted;

        private Vector3 m_stageMinPosition, m_stageMaxPosition;

        private Collider[] m_hitColliders = new Collider[6];
        private int m_amountHit;
        
        #endregion

        #region Accessors

        private float m_distThreshold => 0.1f + (m_currentSpeed / m_ballMaxSpeed);

        private float m_speedThreshold => m_ballMaxSpeed - 20f;

        public float currentBallSpeed => m_currentSpeed;

        public float minSpeed => m_ballMinSpeed;

        private bool m_isFastBall => m_currentSpeed >= m_ballMediumHit;

        #endregion

        #region Unity Events
        
        private void Update()
        {
            if (m_currentSpeed <= 0 && !m_isBunted)
            {
                return;
            }

            if (m_isBunted)
            {
                CheckBuntCooldown();
                return;
            }
            
            if (HasReachedDistance() || HasSetOffBackupCheck())
            {
                ReflectBall();
            }

            CheckForPlayers();
            
            SlowDownBall();

            m_ballMoveDirection = m_ballMoveDirection.normalized * (m_currentSpeed * Time.deltaTime);
            cc.Move(m_ballMoveDirection);

            CheckBallPosition();
        }

        private void LateUpdate()
        {
            m_lastFramePosition = transform.position;
        }

        #endregion

        #region Class Implementation

        public void Initialize(Vector3 _minPosition, Vector3 _maxPosition)
        {
            m_stageMinPosition = _minPosition;
            m_stageMaxPosition = _maxPosition;
        }

        private void CheckBallPosition()
        {
            if (!(transform.position.x > m_stageMaxPosition.x) &&
                !(transform.position.z > m_stageMaxPosition.z) &&
                !(transform.position.x < m_stageMinPosition.x) &&
                !(transform.position.z < m_stageMinPosition.z)) return;
            
            
            Debug.Log("Fixing ball position");
            transform.position = m_currentBallBouncePosition;
            ReflectBall();
        }
        
        private void CheckBuntCooldown()
        {
            if (!m_isBunted)
            {
                return;
            }

            m_currentBuntTimer -= Time.deltaTime;

            if (m_currentBuntTimer > 0)
            {
                return;
            }

            EndBunt();
        }

        public void StopBall()
        {
            m_currentSpeed = 0;
        }

        public void ResetBall()
        {
            m_isBunted = false;
            StopBall();
            m_ballVisuals.materials[0].SetColor(outlineColorName, m_neutralColor);
            m_lastWackCharacter = null;
        }

        private void SlowDownBall()
        {
            if (m_currentSpeed <= m_ballMinSpeed)
            {
                return;
            }

            m_currentSpeed -= Time.deltaTime * m_ballSpeedReduceRate;
        }

        public void InstantSlowBall()
        {
            m_currentSpeed = m_ballMinSpeed;
        }

        private void ReflectBall()
        {
            PlayWallHitSFX();
            
            m_ballMoveDirection = Vector3.Reflect(m_ballMoveDirection, m_currentWallBounceNormal);
            FindNextWallHitPoint(m_ballMoveDirection);

            if (!(m_currentSpeed >= m_ballLightHit))
            {
                return;
            }
            
            JuiceGameController.Instance.DoCameraShake(m_isFastBall ? 0.1f : 0.05f,
                m_isFastBall ? m_strengthMaxAmount : m_strengthMaxAmount/2, 10,
                m_maxRandomness);
        }

        public void HitBall(Vector3 direction, HitStrength _hitLevel, BaseCharacter _currentHittingCharacter)
        {
            if (m_isBunted && _currentHittingCharacter != m_buntingCharacter)
            {
                return;
            }

            if (m_isBunted)
            {
                _hitLevel = HitStrength.HEAVY;
                m_isBunted = false;
            }
            
            m_currentSpeed = _hitLevel switch
            {
                HitStrength.HEAVY => m_ballHeavyHit,
                HitStrength.MEDIUM => m_ballMediumHit,
                _ => m_ballLightHit
            };

            PlayHitBallSFX();
            
            m_ballMoveDirection = direction.FlattenVector3Y();
            FindNextWallHitPoint(m_ballMoveDirection);
            
            _currentHittingCharacter.AddEnergy(m_ballHitEnergyAddAmount);

            if (m_lastWackCharacter == _currentHittingCharacter)
            {
                return;
            }
            
            m_lastWackCharacter = _currentHittingCharacter;
            OnBallSwap?.Invoke(m_lastWackCharacter);
            ChangeColors();
        }

        private void PlayHitBallSFX()
        {
            if (m_hitBallSFX.Count == 0)
            {
                return;
            }
            
            aSource.pitch = Random.Range(0.9f, 1.1f);
            aSource.PlayOneShot(m_hitBallSFX[Random.Range(0, m_hitBallSFX.Count)]);
        }

        private void PlayWallHitSFX()
        {
            if (m_wallHitSFX.Count == 0)
            {
                return;
            }
            
            bSource.pitch = Random.Range(0.8f, 1.2f);
            bSource.PlayOneShot(m_wallHitSFX[Random.Range(0, m_wallHitSFX.Count)]);
        }
        
        
        public void BuntBall(BaseCharacter _currentBuntingCharacter)
        {
            if (m_isBunted)
            {
                return;
            }

            m_currentBuntTimer = m_buntTimerMax;
            m_buntingCharacter = _currentBuntingCharacter;
            StopBall();
            m_ballVisuals.materials[0].SetColor(outlineColorName, m_buntedColor);
            m_lastWackCharacter = null;
            m_isBunted = true;
        }

        private void EndBunt()
        {
            m_isBunted = false;
            m_ballVisuals.materials[0].SetColor(outlineColorName, m_neutralColor);
        }

        private void ChangeColors()
        {
            m_ballVisuals.materials[0].SetColor(outlineColorName, SettingsController.Instance.GetColorByPlayerIndex(m_lastWackCharacter.GetPlayerIndex()));
            m_trail.colorGradient =  SettingsController.Instance.GetGradientByPlayerIndex(m_lastWackCharacter.GetPlayerIndex());
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

        private void CheckForPlayers()
        {
            m_amountHit = Physics.OverlapSphereNonAlloc(transform.position, playerCheckRadius, 
                m_hitColliders, playerCheckLayer);

            if (m_amountHit == 0)
            {
                return;
            }

            for (int i = 0; i < m_amountHit; i++)
            {
                m_hitColliders[i].TryGetComponent(out BaseCharacter _character);

                if (_character.IsNull() || _character == m_lastWackCharacter || m_recentlyHitCharacters.Contains(_character))
                {
                    continue;
                }
                
                _character.ApplyKnockback(this.transform , m_lastWackCharacter ,m_currentSpeed > m_ballLightHit ? 30 : 15, m_ballMoveDirection, true);
                _character.OnDealDamage(this.transform, Mathf.RoundToInt(m_ballDamageAmount * (m_currentSpeed / m_ballMaxSpeed)));
                m_recentlyHitCharacters.Add(_character);
                TickGameController.Instance.CreateNewTimer("Hit_Player", 0.7f, false, RemoveCharacterFromRecentlyHit);
            }
            
        }

        private void RemoveCharacterFromRecentlyHit()
        {
            if (m_recentlyHitCharacters.Count == 0)
            {
                return;
            }
            
            m_recentlyHitCharacters.RemoveAt(0);
        }

        //Use this is wanting to do lethal league style ball bounce
        private void CalculateTwoStepsFromDirection(Vector3 _direction)
        {
            if (!Physics.Raycast(transform.position, _direction, out RaycastHit _hit, Mathf.Infinity, wallLayers))
            {
                Debug.Log($"Something Wrong {_direction} /// {_hit.collider.name}");
                return;
            }
            
            m_currentMotionPath = new BallTrajectory(transform.position, _hit.point);

            var _reflectDir = Vector3.Reflect(m_currentMotionPath.direction, _hit.normal);

            CalculateNextBounce(_hit.point, _reflectDir);
        }

        private void CalculateNextBounce(Vector3 _bouncePos, Vector3 _direction)
        {
            if (Physics.Raycast(_bouncePos, _direction, out RaycastHit _bounceHit, Mathf.Infinity, wallLayers))
            {
                m_nextMotionPath = new BallTrajectory(_bouncePos, _bounceHit.point);
            }
        }
        
        private bool HasReachedDistance()
        {
            return Vector3.SqrMagnitude(transform.position - m_currentBallBouncePosition) <= m_distThreshold;
        }

        private bool HasSetOffBackupCheck()
        {
            
            var _dir = transform.position - m_lastFramePosition;
            return Physics.Raycast(m_lastFramePosition, _dir, _dir.magnitude, wallLayers);
        }

        #endregion
        
        
        
        
    }
}