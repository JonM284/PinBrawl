using System;
using System.Collections.Generic;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay.Sensors;
using Runtime.GameplayInterfaces;
using Runtime.VFX;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Runtime.Gameplay
{
    
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class BallBehavior: MonoBehaviour
    {

        #region Enum

        public enum BallState
        {
            NORMAL,
            BUNTED,
            NULLIFIED,
        }

        #endregion
        
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

        private static readonly int colorName = Shader.PropertyToID("_Color");
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

        [SerializeField] private DamageableDetectionSensor damageableDetectionSensor;
        
        [Header("Ball Variables")]
        [Header("Speed")]
        [SerializeField] private float m_ballMinSpeed = 6f;

        [SerializeField] private float m_ballMaxSpeed = 100f;

        [SerializeField] private float m_ballLightHit = 75f, m_ballMediumHit = 110f, m_ballHeavyHit = 150f;

        [SerializeField] private float m_chargedHitSpeedMod = 1.5f;

        [Header("Size")] 
        [SerializeField] private float m_ballScaleMaxSize = 20f;
        
        [SerializeField] private float m_ballScaleModRate = 1.5f;
        
        [SerializeField] private LayerMask wallLayers;

        [SerializeField] private float m_charConOriginalSize = 0.27f;

        [Space(15)] [Header("Player Interaction")]
        [SerializeField] private float m_ballDamageAmount = 100;

        [SerializeField] private float m_ballMaxKnockback = 40f;
        
        [SerializeField] private float playerCheckRadius;
        
        [SerializeField] private LayerMask playerCheckLayer;

        [Header("Visuals")] 
        [SerializeField] private Transform m_ballVisualsParent;
        
        [SerializeField] private MeshRenderer m_ballVisuals;

        [SerializeField] private TrailRenderer m_trail;

        [SerializeField] private Color m_neutralColor;
        [SerializeField] private Gradient m_neutralGradient;

        [SerializeField] private Color m_buntedColor;
        [SerializeField] private Gradient m_buntGradient;
        
        [SerializeField] private VFXPlayer m_heavyWackVFX;

        [Header("Wack Charge visuals")]
        [SerializeField] private GameObject m_aimRotator;
        [SerializeField] private Image m_ballAimImg,m_ballChargeImg;
        [SerializeField] private VFXPlayer ballChargeVFX;
        
        #endregion
        
        #region Private Fields

        private Vector3 m_ballMoveDirection = new Vector3(1,0,1);

        private BaseCharacter m_lastWackCharacter;

        private BaseCharacter m_buntingCharacter, m_chargedWackPlayer;

        private BallTrajectory m_currentMotionPath;

        private BallTrajectory m_nextMotionPath;
        
        private float m_strengthMaxAmount = 0.1f, m_maxRandomness = 10f;

        private float m_buntTimerMax = 3f, m_currentBuntTimer;

        private float m_currentScale = 1f;

        private Vector3 m_currentBallBouncePosition;

        private Vector3 m_currentWallBounceNormal;

        private Vector3 m_lastFramePosition;

        private List<BaseCharacter> m_recentlyHitCharacters = new List<BaseCharacter>();

        private float m_currentSpeed, m_trackedSpeed;

        private float m_ballHitEnergyAddAmount = 5f;

        private bool m_isBuildingUp;

        private Vector3 m_stageMinPosition, m_stageMaxPosition;

        private Vector3 m_dirFromLastPosition;

        private Collider[] m_hitColliders = new Collider[6];
        private int m_amountHit;

        private BallState m_currentBallState, m_previousState;
        
        #endregion

        #region Accessors

        private float m_distThreshold => 0.1f + (m_currentSpeed / m_ballMaxSpeed);

        private float m_speedThreshold => m_ballMaxSpeed - 20f;

        public float speedPercentage => m_trackedSpeed / m_ballMaxSpeed;

        public float currentBallSpeed => m_currentSpeed;

        public float minSpeed => m_ballMinSpeed;

        public bool isFastBall => m_trackedSpeed >= m_ballHeavyHit;

        public bool isSemiFastBall => m_trackedSpeed >= m_ballMediumHit;

        public BallState CurrentBallState => m_currentBallState;

        public float CurrentBallDamage => Mathf.RoundToInt(m_ballDamageAmount * (m_currentSpeed / m_ballMaxSpeed));
        
        #endregion

        #region Unity Events

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, playerCheckRadius * m_currentScale);
        }

        private void Awake()
        {
            damageableDetectionSensor.SetColliderRadius(playerCheckRadius * m_currentScale);
        }

        private void OnEnable()
        {
            damageableDetectionSensor.OnDamageableEnter += OnHitDamageable;
        }

        private void OnDisable()
        {
            damageableDetectionSensor.OnDamageableEnter -= OnHitDamageable;
        }

        private void Update()
        {
            if (m_currentSpeed <= 0 || m_isBuildingUp)
            {
                return;
            }
            
            if (HasReachedDistance() || HasSetOffBackupCheck())
            {
                ReflectBall();
            }
            
            m_ballMoveDirection = m_ballMoveDirection.normalized * (m_currentSpeed * Time.deltaTime);
            cc.Move(m_ballMoveDirection);

            CheckBallPosition();
            CheckBuntCooldown();
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

        public void ChangeState(BallState newState, BaseCharacter changingCharacter)
        {
            m_previousState = m_currentBallState;
            m_currentBallState = newState;
            
            //State Change has not occured
            if (m_previousState == m_currentBallState)
            {
               return; 
            }
            
            //Upon Entering new state
            switch (m_currentBallState)
            {
                case BallState.NULLIFIED:
                    InstantSlowBall();
                    ChangeColor(m_neutralColor);
                    ChangeGradient(m_neutralGradient);
                    break;
                case BallState.BUNTED:
                    InstantSlowBall();
                    BuntBall(changingCharacter);
                    ChangeGradient(m_buntGradient);
                    break;
                case BallState.NORMAL:
                    if (m_currentSpeed != m_trackedSpeed)
                    {
                        m_currentSpeed = m_trackedSpeed;
                    }
                    break;
            }
        }
        
        private void CheckBuntCooldown()
        {
            if (m_currentBallState != BallState.BUNTED)
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
            m_currentBallState = BallState.NORMAL;
            m_previousState = BallState.NORMAL;
            StopBall();
            ChangeColor(m_neutralColor);
            m_lastWackCharacter = null;
            m_currentScale = 1;
            m_trackedSpeed = m_ballMinSpeed;
            m_ballVisualsParent.transform.localScale = Vector3.one * m_currentScale;
            m_trail.startWidth = m_currentScale/2;
            cc.radius = m_charConOriginalSize;
        }

        private void ShowChargeDirection(bool _display)
        {
            m_aimRotator.gameObject.SetActive(_display);
        }
        
        public void UpdateBallCharge(float m_percentage, Vector3 _aimDirection)
        {
            m_aimRotator.transform.rotation =
                Quaternion.Euler(new Vector3(90,0, Mathf.Atan2(-_aimDirection.x, _aimDirection.z) * Mathf.Rad2Deg)); 
            m_ballChargeImg.fillAmount = m_percentage;
        }
        
        private void SlowDownBall()
        {
            if (m_currentSpeed <= m_ballMinSpeed)
            {
                return;
            }

            m_currentSpeed -= Time.deltaTime * m_ballSpeedReduceRate;
        }

        private void InstantReduceSpeedByPercentage(float percent)
        {
            if (m_currentSpeed <= m_ballMinSpeed)
            {
                return;
            }

            percent = Mathf.Clamp01(percent);
            var amountProxy = Mathf.Max(m_currentSpeed * percent, m_ballMinSpeed);
            m_currentSpeed = amountProxy;
        }
        
        public void InstantSlowBall()
        {
            m_currentSpeed = m_ballMinSpeed;
        }

        private void ReflectBall()
        {
            PlayWallHitSFX();
            
            ChangeBallDirection(Vector3.Reflect(m_ballMoveDirection, m_currentWallBounceNormal));

            if (m_currentBallState == BallState.BUNTED)
            {
                ChangeState(BallState.NORMAL, m_buntingCharacter);
            }
        }

        public void SetBuildUp(bool _isBuildingUp, BaseCharacter _baseCharacter)
        {
            m_isBuildingUp = _isBuildingUp;

            m_chargedWackPlayer = m_isBuildingUp ? _baseCharacter : null;

            m_ballChargeImg.fillAmount = 0;
            
            ballChargeVFX.ChangeAllStartColor(_baseCharacter.playerColor);
            ballChargeVFX.gameObject.SetActive(_isBuildingUp);
            
            if (_isBuildingUp && !m_heavyWackVFX.IsNull() && isSemiFastBall)
            {
                m_heavyWackVFX.ChangeAllStartColor(_baseCharacter.playerColor);
                m_heavyWackVFX.Play();
            }
            
            ShowChargeDirection(_isBuildingUp);
        }
        
        public void ChargedWackResizeBall()
        {
            if (m_currentScale >= m_ballScaleMaxSize)
            {
                return;
            }
            Debug.Log("Changing Size");
            m_currentScale = Mathf.Clamp(m_currentScale + m_ballScaleModRate, 1, m_ballScaleMaxSize);
            m_ballVisualsParent.transform.localScale = Vector3.one * m_currentScale;
            damageableDetectionSensor.SetColliderRadius(playerCheckRadius * m_currentScale);
            cc.radius = m_charConOriginalSize * m_currentScale;
            m_trail.startWidth = m_currentScale/2;
        }

        public void HitBall(Vector3 direction, HitStrengthType _hitLevel, BaseCharacter _currentHittingCharacter)
        {
            if (m_isBuildingUp && _currentHittingCharacter != m_chargedWackPlayer)
            {
                return;
            }

            if (_hitLevel == HitStrengthType.NULLIFY)
            {
                ChangeBallDirection(direction);
                ChangeState(BallState.NULLIFIED, _currentHittingCharacter);
                return;
            }

            if (m_currentBallState == BallState.BUNTED && _currentHittingCharacter != m_buntingCharacter)
            {
                return;
            }
            
            switch (_hitLevel)
            {
                case HitStrengthType.LIGHT:
                    m_trackedSpeed += 0.05f;
                    break;
                default:
                    m_trackedSpeed *= m_chargedHitSpeedMod;
                    break;
            }

            m_trackedSpeed = Mathf.Clamp(m_trackedSpeed, m_ballMinSpeed, m_ballMaxSpeed);

            m_currentSpeed = m_trackedSpeed;
            
            PlayHitBallSFX();

            ChangeBallDirection(direction);
            ChangeState(BallState.NORMAL, _currentHittingCharacter);
            
            if (m_lastWackCharacter == _currentHittingCharacter && m_previousState == BallState.NORMAL)
            {
                return;
            }
            
            m_lastWackCharacter = _currentHittingCharacter;
            OnBallSwap?.Invoke(m_lastWackCharacter);
            ChangeColor(m_lastWackCharacter);
        }

        private void ChangeBallDirection(Vector3 direction)
        {
            m_ballMoveDirection = direction.FlattenVector3Y();
            FindNextWallHitPoint(m_ballMoveDirection);
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
        
        
        private void BuntBall(BaseCharacter _currentBuntingCharacter)
        {
            m_currentBuntTimer = m_buntTimerMax;
            m_buntingCharacter = _currentBuntingCharacter;
            ChangeBallDirection(transform.position - _currentBuntingCharacter.transform.position);
            ChangeColor(m_buntedColor);
            m_lastWackCharacter = null;
        }

        private void EndBunt()
        {
            ChangeState(BallState.NORMAL, m_buntingCharacter);
            ChangeColor(m_neutralColor);
        }

        private void ChangeColor(BaseCharacter character)
        {
            m_ballVisuals.materials[0].SetColor(colorName, SettingsController.Instance.GetColorByPlayerIndex(character.GetPlayerIndex()));
            m_ballVisuals.materials[1].SetColor(outlineColorName, SettingsController.Instance.GetColorByPlayerIndex(character.GetPlayerIndex()));
            ChangeGradient(SettingsController.Instance.GetGradientByPlayerIndex(character.GetPlayerIndex()));
        }

        private void ChangeColor(Color _newColor)
        {
            m_ballVisuals.materials[0].SetColor(colorName, _newColor);
            m_ballVisuals.materials[1].SetColor(outlineColorName, _newColor);
            m_ballAimImg.color = _newColor;
            m_ballChargeImg.color = _newColor;
        }

        private void ChangeGradient(Gradient newGradient)
        {
            m_trail.colorGradient =  newGradient;
        }

        public void ForceChangeColorTo(Color color)
        {
            ChangeColor(color);
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

        private void OnHitDamageable(IDamagable hitDamageable)
        {
            if (hitDamageable.IsNull()) return;
            if (m_lastWackCharacter.IsNull()) return;
            if (m_currentSpeed <= 0 || m_isBuildingUp) return;
            if (m_currentBallState == BallState.NULLIFIED) return; //ball is harmless when null
            if (hitDamageable is BaseCharacter hitCharacter)
            {
                OnHitPlayer(hitCharacter);
                return;
            }

            hitDamageable.OnDealDamage(this.transform, CurrentBallDamage);
        }

        private void OnHitPlayer(BaseCharacter hitCharacter)
        {
            if (hitCharacter.IsNull()) return;
            if (hitCharacter == m_lastWackCharacter || m_recentlyHitCharacters.Contains(hitCharacter)) return;
            if (hitCharacter.isParrying)
            {
                ChangeState(BallState.BUNTED, hitCharacter);
                return;
            }
            
            hitCharacter.OnDealDamage(this.transform, CurrentBallDamage);
            hitCharacter.ApplyKnockback(this.transform , m_lastWackCharacter ,m_currentSpeed > m_ballLightHit ? 30 : 15, m_ballMoveDirection, true);
            m_recentlyHitCharacters.Add(hitCharacter);
            TickGameController.Instance.CreateNewTimer("Hit_Player", 0.7f, false, RemoveCharacterFromRecentlyHit);
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
            return Vector3.SqrMagnitude(transform.position - m_currentBallBouncePosition) <= m_distThreshold * m_currentScale;
        }

        private bool HasSetOffBackupCheck()
        {
            m_dirFromLastPosition = transform.position - m_lastFramePosition;
            return Physics.Raycast(m_lastFramePosition, m_dirFromLastPosition, m_dirFromLastPosition.magnitude, wallLayers);
        }

        #endregion
        
        
        
        
    }
}