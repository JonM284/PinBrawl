using System;
using System.Collections.Generic;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay.Sensors;
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
        
        [SerializeField] private PlayerDetectionSensor playerDetectionSensor;
        
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

        [SerializeField] private Color m_buntedColor;
        
        [SerializeField] private VFXPlayer m_heavyWackVFX;

        [Header("Wack Charge visuals")]
        [SerializeField] private GameObject m_aimRotator;
        [SerializeField] private Image m_ballAimImg,m_ballChargeImg;
        
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

        private bool m_isBunted, m_isBuildingUp;

        private Vector3 m_stageMinPosition, m_stageMaxPosition;

        private Vector3 m_dirFromLastPosition;

        private Collider[] m_hitColliders = new Collider[6];
        private int m_amountHit;
        
        #endregion

        #region Accessors

        private float m_distThreshold => 0.1f + (m_currentSpeed / m_ballMaxSpeed);

        private float m_speedThreshold => m_ballMaxSpeed - 20f;

        public float speedPercentage => m_trackedSpeed / m_ballMaxSpeed;

        public float currentBallSpeed => m_currentSpeed;

        public float minSpeed => m_ballMinSpeed;

        public bool isFastBall => m_trackedSpeed >= m_ballHeavyHit;

        public bool isSemiFastBall => m_trackedSpeed >= m_ballMediumHit;

        #endregion

        #region Unity Events

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, playerCheckRadius * m_currentScale);
        }

        private void Awake()
        {
            playerDetectionSensor.SetColliderRadius(playerCheckRadius);
        }

        private void OnEnable()
        {
            playerDetectionSensor.OnCharacterEnter += OnHitPlayer;
        }

        private void OnDisable()
        {
            playerDetectionSensor.OnCharacterEnter -= OnHitPlayer;
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

        public void InstantSlowBall()
        {
            m_currentSpeed = m_ballMinSpeed;
        }

        private void ReflectBall()
        {
            PlayWallHitSFX();
            
            m_ballMoveDirection = Vector3.Reflect(m_ballMoveDirection, m_currentWallBounceNormal);
            FindNextWallHitPoint(m_ballMoveDirection);
        }

        public void SetBuildUp(bool _isBuildingUp, BaseCharacter _baseCharacter)
        {
            m_isBuildingUp = _isBuildingUp;

            m_chargedWackPlayer = m_isBuildingUp ? _baseCharacter : null;

            m_ballChargeImg.fillAmount = 0;
            
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
            
            m_currentScale = Mathf.Clamp(m_currentScale + m_ballScaleModRate, 1, m_ballScaleMaxSize);
            m_ballVisualsParent.transform.localScale = Vector3.one * m_currentScale;
            cc.radius = m_charConOriginalSize * m_currentScale;
            m_trail.startWidth = m_currentScale/2;
        }

        public void HitBall(Vector3 direction, HitStrengthType _hitLevel, BaseCharacter _currentHittingCharacter)
        {
            //ToDo: speed (x2 or so) if charged hit
            //ToDo: on charged hit, pause wacking player and ball for a second or so, [build up to new speed] then release. 
            //During that time, player can change direction
            //ToDo: Ball Increases size on charged hit
            //ToDO: basic hit -> just redirect ball 
            //ALWAYS: change ball color
            
            if (m_isBuildingUp && _currentHittingCharacter != m_chargedWackPlayer)
            {
                return;
            }
            
            if (_hitLevel == HitStrengthType.LIGHT)
            {
                m_trackedSpeed += 1f;
            }
            else
            {
                m_trackedSpeed *= m_chargedHitSpeedMod;
            }
            
            m_trackedSpeed = Mathf.Clamp(m_trackedSpeed, m_ballMinSpeed, m_ballMaxSpeed);

            m_currentSpeed = m_trackedSpeed;
            
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

        public void ForceChangeColorTo(Color _newColor)
        {
            m_ballVisuals.materials[0].SetColor(outlineColorName, _newColor);
            m_ballAimImg.color = _newColor;
            m_ballChargeImg.color = _newColor;
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

        private void OnHitPlayer(BaseCharacter hitCharacter)
        {
            if (m_lastWackCharacter.IsNull()) return;
            if (m_currentSpeed <= 0 || m_isBuildingUp) return;
            if (hitCharacter.IsNull() || hitCharacter == m_lastWackCharacter || m_recentlyHitCharacters.Contains(hitCharacter))
            {
                return;
            }
                
            hitCharacter.OnDealDamage(this.transform, Mathf.RoundToInt(m_ballDamageAmount * (m_currentSpeed / m_ballMaxSpeed)));
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