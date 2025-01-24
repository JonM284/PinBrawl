using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.Gameplay;
using Runtime.GameplayInterfaces;
using Runtime.VFX;
using UnityEngine;

namespace Runtime.Abilities
{
    public class BuntAbility: AbilityBase
    {
        
        #region Serialized Fields

        [SerializeField] private float m_buntTimeMax;
        
        [SerializeField] private VFXPlayer m_indicator;
        
        #endregion

        #region Private Fields

        private float m_buntTimeCurrent;

        private Collider[] m_hitColliders = new Collider[6];
        private int m_amountHit;
        
        #endregion
        
        #region Accessors

        private MeleeAbilityData m_meleeAbilityData => abilityData as MeleeAbilityData;

        private float meleeSphereRadius => m_meleeAbilityData.abilityScale * currentScale;
        
        #endregion
        
        #region Class Implementation

        private async UniTask T_DoBunt()
        {
            
            m_indicator.Play();
            
            while (m_buntTimeCurrent > 0f)
            {
                m_buntTimeCurrent -= Time.deltaTime;
                
                CheckPoint();
                
                await UniTask.Yield();
            }
            
        }

        private void CheckPoint()
        {
            m_amountHit = Physics.OverlapSphereNonAlloc(currentOwner.transform.position, meleeSphereRadius,
                m_hitColliders, m_meleeAbilityData.collisionDetectionLayers);

            if (m_amountHit == 0)
            {
                return;
            }

            for (int i = 0; i < m_amountHit; i++)
            {
                if (m_hitColliders[i].IsNull())
                {
                    continue;
                }

                if (m_previouslyHitColliders.Contains(m_hitColliders[i]))
                {
                    continue;
                }
                
                //first check if ball -> hit ball
                HitCollider(m_hitColliders[i]);
            }

        }

        private void HitCollider(Collider _collider)
        {
            if (m_previouslyHitColliders.Count > 0 && m_previouslyHitColliders.Contains(_collider))
            {
                return;
            }
            
            _collider.TryGetComponent(out BallBehavior _ball);

            if (!_ball.IsNull())
            {
                _ball.BuntBall(currentOwner);
                return;
            }

            _collider.TryGetComponent(out IDamagable _damagable);
            _collider.TryGetComponent(out IKnockbackable _knockbackable);
            
            _knockbackable?.ApplyKnockback(currentOwner.transform, currentOwner , currentKnockback, Vector3.zero);
            _damagable?.OnDealDamage(currentOwner.transform, currentDamage, currentOwner);
            
            m_previouslyHitColliders.Add(_collider);
        }

        #endregion

        #region IAbility Inherited Methods

        public override async UniTask DoAbility()
        {
            base.DoAbility();
            
            currentOwner.HaltCharacterMovement();

            m_buntTimeCurrent = m_buntTimeMax;
            
            m_previouslyHitColliders.Clear();

            PlayRandomSound();
            
            await T_DoBunt();

            currentOwner.ResetCharacterMovementSpeed();
        }

        #endregion
        
        
        
    }
}