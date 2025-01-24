using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.GameplayInterfaces;
using Runtime.ScriptedAnimations;
using Runtime.VFX;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class CreationEntityBase: MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField] private Transform m_detectionVisuals;

        [SerializeField] private GameObject m_creationVisuals;
        
        [SerializeField] private VFXPlayer m_endVFX;

        [SerializeField] private AnimationsBase m_introAnimation;

        [SerializeField] private AnimationsBase m_outroAnimation;

        [SerializeField] private List<MeshRenderer> m_changableMR = new List<MeshRenderer>();

        [SerializeField] private float m_creationScaleMultiplier = 1f;

        #endregion
        
        #region Protected Fields

        protected float m_damageAmount;

        protected float m_playerDamagedEnergyAddAmount;

        protected float m_knockbackAmount;

        protected float m_detectionRange;

        protected float m_lifeTimer;

        protected float m_creationScale;

        protected CreationAbilityData m_creationAbilityData;

        protected BaseCharacter m_owner;

        protected LayerMask m_detectableLayers;

        protected bool m_isInitialized, m_isRanged;

        protected Vector3 m_savedAimDirection;

        protected bool m_isExplosion, m_isEnding;

        protected float m_WaitTime = 1.5f;

        protected Collider[] m_hitColliders = new Collider[6];
        protected int m_hitAmount;

        #endregion
        
        #region Unity Events

        private void Update()
        {
            UpdateCreation();
        }

        #endregion
        
        #region Class Implementation

        public async UniTask Initialize(BaseCharacter _ownerPlayer, CreationAbilityData _creationAbilityData, 
            float _scale, float _damage, float _knockbackAmount , float _maxLifetime)
        {
            if (_ownerPlayer.IsNull())
            {
                return;
            }

            m_isInitialized = false;
            
            m_creationScale = _scale;
            
            if (!m_detectionVisuals.IsNull())
            {
                m_detectionVisuals.gameObject.SetActive(false);
            }

            if (!_creationAbilityData.isWall)
            {
                m_creationVisuals.transform.localScale = Vector3.one * (m_creationScale * m_creationScaleMultiplier);
            }
            else
            {
                m_creationVisuals.transform.localScale = _creationAbilityData.wallHalfExtents * 2;
            }
            
            m_creationVisuals.SetActive(true);
            m_isEnding = false;
            m_owner = _ownerPlayer;
            m_damageAmount = _damage;
            m_knockbackAmount = _knockbackAmount;
            m_detectionRange = _scale;
            m_creationAbilityData = _creationAbilityData;
            m_lifeTimer = _maxLifetime;
            m_detectableLayers = _creationAbilityData.collisionDetectionLayers;

            m_playerDamagedEnergyAddAmount = SettingsController.Instance.GetPvpDamageEnergyAmount();

            if (!m_detectionVisuals.IsNull())
            {
                m_detectionVisuals.localScale = Vector3.one * (m_detectionRange * 2);
            }
            
            m_isExplosion = _creationAbilityData.isExplosion;
            
            if (_creationAbilityData.isSavePointedDirection)
            {
                m_savedAimDirection = m_owner.m_playerAimVector;
            }

            if (!m_introAnimation.IsNull())
            {
                m_introAnimation.Play();
                await UniTask.WaitUntil(() => !m_introAnimation.isPlaying);
            }

            ChangeColors();
            
            if (!m_detectionVisuals.IsNull())
            {
                m_detectionVisuals.gameObject.SetActive(true);
            }

            await UniTask.WaitForSeconds(0.5f);
            
            m_isInitialized = true;

        }

        protected virtual void ChangeColors()
        {
            if (m_changableMR.Count == 0)
            {
                return;
            }

            foreach (var _mr in m_changableMR)
            {
                if (_mr.IsNull())
                {
                    continue;
                }
                
                _mr.materials[0].SetColor("_Tint", m_owner.playerColor);
                _mr.materials[0].SetColor("_Color", m_owner.playerColor);
            }
        }

        protected virtual void UpdateCreation()
        {
            if (m_isEnding)
            {
                return;
            }
            
            CheckLifeTime();

            if (m_isExplosion)
            {
                return;
            }
            
            CheckDetectionRange();
        }

        protected void CheckLifeTime()
        {
            if (!m_isInitialized)
            {
                return;
            }

            m_lifeTimer -= Time.deltaTime;

            if (m_lifeTimer > 0)
            {
                return;
            }
            Debug.Log("Time ran out");

            EndCreation();
        }

        protected virtual void CheckDetectionRange()
        {
            if (!m_isInitialized)
            {
                return;
            }

            m_hitAmount = 
                Physics.OverlapSphereNonAlloc(transform.position, m_detectionRange, m_hitColliders ,m_detectableLayers);

            if (m_hitAmount == 0)
            {
                return;
            }

            bool _hasHit = false;

            for (int i = 0; i < m_hitAmount; i++)
            {
                m_hitColliders[i].TryGetComponent(out BaseCharacter _character);
                
                if (_character == m_owner)
                {
                    continue;
                }
                
                ApplyStatus(_character);
                
                //first check if ball -> hit ball
                HitCollider(m_hitColliders[i]);

                _hasHit = true;
            }

            if (!_hasHit || m_isExplosion)
            {
                return;
            }
            
            Debug.Log("Detected");
            
            DeleteObject();
        }
        
        protected void HitCollider(Collider _collider)
        {
            _collider.TryGetComponent(out BallBehavior _ball);

            if (!_ball.IsNull())
            {
                if (m_creationAbilityData.isHitBall)
                {
                    var _hitDirection = (_ball.transform.position - transform.position) *
                                        m_creationAbilityData.knockbackDirectionMod;

                    if (m_creationAbilityData.isSavePointedDirection)
                    {
                        _hitDirection = m_savedAimDirection * m_creationAbilityData.knockbackDirectionMod;
                    }
                
                    _ball.HitBall(_hitDirection, m_creationAbilityData.ballHitStrength, m_owner);
                }
                else if(m_creationAbilityData.isWall)
                {
                    if (_ball.currentBallSpeed > _ball.minSpeed)
                    {
                        _ball.InstantSlowBall();    
                    }
                }
            }

            if (m_knockbackAmount > 0)
            {
                _collider.TryGetComponent(out IKnockbackable _knockbackable);
                _knockbackable?.ApplyKnockback(transform, m_owner , m_knockbackAmount, 
                    m_creationAbilityData.isSavePointedDirection ? 
                        m_savedAimDirection * m_creationAbilityData.knockbackDirectionMod : Vector3.zero);    
            }
            
            if (m_damageAmount > 0)
            {
                _collider.TryGetComponent(out IDamagable _damagable);
                _damagable?.OnDealDamage(transform, m_damageAmount, m_owner);
            }
            
        }

        private async UniTask EndCreation()
        {

            m_isEnding = true;
            
            if (m_isExplosion)
            {
                m_endVFX.Play();
                
                m_creationVisuals.SetActive(false);
            
                CheckDetectionRange();
                
                await UniTask.WaitUntil(() => !m_endVFX.is_playing);
            }

            if (!m_outroAnimation.IsNull())
            {
                m_outroAnimation.Play();
                await UniTask.WaitUntil(() => !m_outroAnimation.isPlaying);
            }
            
            DeleteObject();
        }

        //ToDo: RETURN TO POOL
        public void DeleteObject()
        {
            Debug.Log("Deleted Creation");
            ObjectPoolController.Instance.ReturnToPool(m_creationAbilityData.name, gameObject);
        }
        
        private void ApplyStatus(BaseCharacter _character)
        {
            if (_character.IsNull())
            {
                return;
            }
            
            if (m_creationAbilityData.applicableStatusesOnHit.Count <= 0)
            {
                return;
            }
            
            foreach (var _statusData in m_creationAbilityData.applicableStatusesOnHit)
            {
                if(_character.IsNull() || _character.ContainsStatus(_statusData)){
                    continue;   
                }

                _character.ApplyStatus(_statusData);
            }
        }
        
        #endregion
        
        
        
        
        
        
    }
}