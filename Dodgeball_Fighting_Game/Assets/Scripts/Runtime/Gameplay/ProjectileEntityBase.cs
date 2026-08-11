using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Abilities;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay.Sensors;
using Runtime.GameplayInterfaces;
using Runtime.ScriptedAnimations;
using Runtime.VFX;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Gameplay
{
    public class ProjectileEntityBase: MonoBehaviour
    {

        #region Actions

        public static event Action<BaseCharacter> OnProjectileEnded;

        #endregion

        #region Serialized Fields

        [SerializeField] private UnityEvent onEnd;

        [SerializeField] private Transform m_visuals;

        [SerializeField] private PlayerDetectionSensor playerDetectionSensor;
        [SerializeField] private BallDetectionSensor ballDetectionSensor;
        [SerializeField] private WallDetectionSensor wallDetectionSensor;
        
        [SerializeField] private AnimationsBase m_movingAnimation;

        [SerializeField] private VFXPlayer m_explosionVisuals;

        [SerializeField] private GameObject m_creatableOnEnd;

        #endregion
        
        #region Private Fields

        private Vector3 m_moveDir;

        private BaseCharacter m_owner;

        private float m_lifeTimeTimer ,m_maxLifetime;
        private float m_moveSpeed;
        private float m_damageAmount;
        private float m_knockbackAmount;
        private float m_explosionRange;

        private bool m_isInitialized;
        private bool m_isPassThroughObjects;
        private bool m_hasEnded;

        private int m_amountOfStages;

        private float m_playerDamagedEnergyAddAmount;

        private string objectPoolNameRef;
        
        private ProjectileEndType m_projectileEndType;
        
        private LayerMask m_detectableLayers;

        private HitStrengthType _mBallHitStrengthType;

        private ProjectileAbilityData m_projectileAbilityData;
        
        protected List<BaseCharacter> m_previouslyHitCharacter = new List<BaseCharacter>();

        protected Collider[] m_hitColliders = new Collider[6];
        protected int m_amountHit;
        
        protected Collider[] m_explosionHitColliders = new Collider[6];
        protected int m_explosionHitAmount;
        protected CancellationTokenSource cts = new CancellationTokenSource();
        
        #endregion

        #region Unity Events

        private void OnEnable()
        {
            playerDetectionSensor.OnCharacterEnter += OnCharacterEnter;
            ballDetectionSensor.OnBallEnter += OnBallEnter;
            wallDetectionSensor.OnWallHit += OnWallHit;
        }

        private void OnDisable()
        {
            playerDetectionSensor.OnCharacterEnter -= OnCharacterEnter;
            ballDetectionSensor.OnBallEnter -= OnBallEnter;
            wallDetectionSensor.OnWallHit -= OnWallHit;
        }

        private void Update()
        {
            if (m_hasEnded || !m_isInitialized)
            {
                return;
            }

            MoveProjectile();
            
            SlowDown();
            
            m_lifeTimeTimer += Time.deltaTime;

            if (!(m_lifeTimeTimer >= m_maxLifetime))
            {
                return;
            }
            
            OnProjectileEnd();
        }

        #endregion
        
        #region Class Implementation

        public void Initialize(BaseCharacter _shooter, Vector3 _direction, ProjectileAbilityData _abilityData, 
            float _speed, float _damageAmount, float _knockbackAmount, float _maxLifetime, float _scale = 1f)
        {
            m_owner = _shooter;
            
            m_moveDir = _direction;
            
            m_moveSpeed = _speed;

            m_hasEnded = false;

            m_damageAmount = _damageAmount;
            
            m_knockbackAmount = _knockbackAmount;

            m_projectileAbilityData = _abilityData;
            
            playerDetectionSensor.SetColliderRadius(_scale / 2f);

            m_explosionRange = _scale;

            _mBallHitStrengthType = _abilityData.ballHitStrengthType;

            m_isPassThroughObjects = _abilityData.isPassThroughObjects;

            m_projectileEndType = _abilityData.projectileEndType;

            m_amountOfStages = _abilityData.amountOfStages;

            m_playerDamagedEnergyAddAmount = SettingsController.Instance.GetPvpDamageEnergyAmount();
            
            m_visuals.localScale = Vector3.one * (_scale);

            m_detectableLayers = _abilityData.collisionDetectionLayers;

            m_maxLifetime = _maxLifetime;
            
            m_lifeTimeTimer = 0;

            m_previouslyHitCharacter.Clear();

            objectPoolNameRef = _abilityData.isGenericCharacterPrefab
                ? string.Format(MatchGameController.Instance.defaultProjectilePoolNameFormat,
                    m_owner.characterData.characterName, m_owner.GetPlayerIndex(),
                    _abilityData.abilityName)
                : _abilityData.abilityName;
            
            if (!m_movingAnimation.IsNull())
            {
                m_movingAnimation.Play();
            }

            m_isInitialized = true;
        }

        private void MoveProjectile()
        {
            transform.position += m_moveDir * (m_moveSpeed * Time.deltaTime);
        }
        
        private void DeleteObject()
        {
            onEnd?.Invoke();
            ObjectPoolController.Instance.ReturnToPool(objectPoolNameRef, gameObject);
        }

        private void SlowDown()
        {
            if (m_projectileAbilityData.IsNull() || !m_projectileAbilityData.isSlowDownOverTime)
            {
                return;
            }

            if (m_projectileAbilityData.slowDownType == SlowDownType.EXPONENTIAL)
            {
                m_moveSpeed *= Mathf.Exp(-m_projectileAbilityData.slowDownModifier * Time.deltaTime);
            }
            else
            {
                m_moveSpeed -= Time.deltaTime * m_projectileAbilityData.slowDownModifier;
            }
        }
        
        private void OnWallHit()
        {
            if (!m_isPassThroughObjects)
            {
                OnProjectileEnd().Forget();
            }
        }
        
        private void OnBallEnter(BallBehavior ball)
        {
            Debug.Log("Ball Enter Range");
            
            ball.HitBall(m_moveDir, _mBallHitStrengthType, m_owner);
            
            if (!m_isPassThroughObjects)
            {
                OnProjectileEnd().Forget();
            }
        }

        private void OnCharacterEnter(BaseCharacter character)
        {
            if (character == m_owner)
            {
                return;
            }

            if (!m_previouslyHitCharacter.Contains(character))
            {
                ApplyStatus(character);
            
                if (m_knockbackAmount > 0)
                {
                    character.ApplyKnockback(transform, m_owner, m_knockbackAmount,
                        m_projectileEndType == ProjectileEndType.EXPLODE ? Vector3.zero : m_moveDir);
                }
            
                if (m_damageAmount > 0)
                {
                    character.OnDealDamage(transform, m_damageAmount, m_owner);
                }
                
                m_previouslyHitCharacter.Add(character);
            }
            
            if (!m_isPassThroughObjects)
            {
                OnProjectileEnd().Forget();
            }
        }

        private async UniTask OnProjectileEnd()
        {
            m_hasEnded = true;
            
            //ToDo: Might not be necessary
            switch (m_projectileEndType)
            {
                case ProjectileEndType.EXPLODE:
                    await T_ExplodeOnEnd();
                    break;
                case ProjectileEndType.PROJECTILE_RETURN:
                    //await T_ShootReturnProjectile();
                    break;
                case ProjectileEndType.PROJECTILE_SPREAD:
                    //await T_ShootSpreadShot();
                    break;
            }
            
            OnProjectileEnded?.Invoke(m_owner);
            DeleteObject();
        }

        private async UniTask T_ExplodeOnEnd()
        {
            if (m_projectileEndType != ProjectileEndType.EXPLODE)
            {
                return;
            }
            
            if (!m_explosionVisuals.IsNull())
            {
                m_explosionVisuals.Play();
            }
            
            CheckExplosion();

            if (!m_explosionVisuals.IsNull())
            {
                await UniTask.WaitUntil(() => !m_explosionVisuals.is_playing);
            }
            
        }
        
        protected void CheckExplosion()
        {
            if (m_projectileEndType != ProjectileEndType.EXPLODE)
            {
                return;
            }
            
            m_explosionHitAmount = Physics.OverlapSphereNonAlloc(transform.position, m_explosionRange,
                m_explosionHitColliders, m_detectableLayers);

            if (m_explosionHitAmount == 0)
            {
                return;
            }

            for (int i = 0; i < m_explosionHitAmount; i++)
            {
                m_explosionHitColliders[i].TryGetComponent(out BaseCharacter _character);
                
                if (_character == m_owner)
                {
                    continue;
                }
                
                ApplyStatus(_character);
                
                //DoInteraction(m_explosionHitColliders[i]);
            }
        }

        #region Interaction Related

        private void ApplyStatus(BaseCharacter _character)
        {
            if (m_projectileAbilityData.applicableStatusesOnHit.Count <= 0)
            {
                return;
            }

            if (cts.IsNull())
            {
                cts = new CancellationTokenSource();
            }
            
            foreach (var _statusData in m_projectileAbilityData.applicableStatusesOnHit)
            {
                if(_character.IsNull() || _character.ContainsStatus(_statusData)){
                    continue;   
                }

                _character.ApplyStatus(_statusData, cts.Token).Forget();
            }
        }

        #endregion
        
        
        
        #endregion




    }
}