using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Abilities;
using Runtime.Character;
using Runtime.GameControllers;
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
        private float m_detectionRange;
        private float m_explosionRange;

        private bool m_isInitialized;
        private bool m_isPassThroughObjects;
        private bool m_hasEnded;

        private int m_amountOfStages;

        private float m_playerDamagedEnergyAddAmount;
        
        private ProjectileEndType m_projectileEndType;
        
        private LayerMask m_detectableLayers;

        private HitStrength m_ballHitStrength;

        private ProjectileAbilityData m_projectileAbilityData;
        
        protected List<Collider> m_previouslyHitColliders = new List<Collider>();

        protected Collider[] m_hitColliders = new Collider[6];
        protected int m_amountHit;
        
        protected Collider[] m_explosionHitColliders = new Collider[6];
        protected int m_explosionHitAmount;
        
        #endregion

        #region Unity Events

        private void Update()
        {
            if (m_hasEnded)
            {
                return;
            }

            MoveProjectile();
            
            SlowDown();
            
            CheckDetectionRange();

            if (!m_isInitialized)
            {
                return;
            }
            
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

            m_detectionRange = _scale;

            m_explosionRange = _scale;

            m_ballHitStrength = _abilityData.ballHitStrength;

            m_isPassThroughObjects = _abilityData.isPassThroughObjects;

            m_projectileEndType = _abilityData.projectileEndType;

            m_amountOfStages = _abilityData.amountOfStages;

            m_playerDamagedEnergyAddAmount = SettingsController.Instance.GetPvpDamageEnergyAmount();
            
            m_visuals.localScale = Vector3.one * (_scale * 2);

            m_detectableLayers = _abilityData.collisionDetectionLayers;

            m_maxLifetime = _maxLifetime;
            
            m_lifeTimeTimer = 0;

            m_previouslyHitColliders.Clear();
            
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
            ObjectPoolController.Instance.ReturnToPool(m_projectileAbilityData.name, gameObject);
        }

        private void SlowDown()
        {
            if (!m_projectileAbilityData.isSlowDownOverTime)
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

        private void CheckDetectionRange()
        {
            m_amountHit = Physics.OverlapSphereNonAlloc(transform.position, m_detectionRange, m_hitColliders
                ,m_detectableLayers);

            if (m_amountHit == 0)
            {
                return;
            }

            for (int i = 0; i < m_amountHit; i++)
            {
                if (m_previouslyHitColliders.Contains(m_hitColliders[i]))
                {
                    continue;
                }
                
                m_hitColliders[i].TryGetComponent(out BaseCharacter _character);
                
                if (_character == m_owner)
                {
                    continue;
                }

                ApplyStatus(_character);
                
                DoInteraction(m_hitColliders[i]);
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
                
                DoInteraction(m_explosionHitColliders[i]);
            }
        }

        #region Interaction Related

        protected virtual void DoInteraction(Collider _collider)
        {
            if (m_previouslyHitColliders.Count > 0 && m_previouslyHitColliders.Contains(_collider))
            {
                return;
            }
            
            //first check if ball -> hit ball
            _collider.TryGetComponent(out BallBehavior _ball);

            if (!_ball.IsNull())
            {
                if (!m_isPassThroughObjects)
                {
                    _ball.HitBall(m_projectileEndType == ProjectileEndType.EXPLODE ? 
                        _collider.transform.position - transform.position :
                        m_moveDir, m_ballHitStrength, m_owner);
                    
                    OnProjectileEnd();
                    return;
                }
            }

            HitCollided(_collider);
                
            if (!m_isPassThroughObjects)
            {
                OnProjectileEnd();
            }
        }
        
        
        private void HitCollided(Collider _collider)
        {
            if (m_knockbackAmount > 0)
            {
                _collider.TryGetComponent(out IKnockbackable _knockbackable);
                _knockbackable?.ApplyKnockback(transform, m_owner, m_knockbackAmount,
                    m_projectileEndType == ProjectileEndType.EXPLODE ? Vector3.zero : m_moveDir);
            }
            
            if (m_damageAmount > 0)
            {
                _collider.TryGetComponent(out IDamagable _damagable);
                _damagable?.OnDealDamage(transform, m_damageAmount, m_owner);
            }
            
            m_previouslyHitColliders.Add(_collider);
        }

        private void ApplyStatus(BaseCharacter _character)
        {
            if (m_projectileAbilityData.applicableStatusesOnHit.Count <= 0)
            {
                return;
            }
            
            foreach (var _statusData in m_projectileAbilityData.applicableStatusesOnHit)
            {
                if(_character.IsNull() || _character.ContainsStatus(_statusData)){
                    continue;   
                }

                _character.ApplyStatus(_statusData);
            }
        }

        #endregion
        
        
        
        #endregion




    }
}