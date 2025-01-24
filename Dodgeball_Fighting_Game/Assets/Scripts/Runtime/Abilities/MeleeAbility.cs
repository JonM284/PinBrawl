using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.GameplayInterfaces;
using Runtime.VFX;
using UnityEngine;

namespace Runtime.Abilities
{
    public class MeleeAbility: AbilityBase
    {

        #region Serialized Fields
        
        [SerializeField] private VFXPlayer m_indicator;

        [SerializeField] private ParticleSystem m_particleSystem;

        [SerializeField] private GameObject m_radialMeleeIndicator;
        [SerializeField] private LineRenderer m_lineMeleeIndicator;

        #endregion

        #region Private Fields

        private Collider[] m_hitColliders = new Collider[6];
        private int m_hitAmount;

        #endregion
        
        #region Accessors

        private MeleeAbilityData m_meleeAbilityData => abilityData as MeleeAbilityData;

        #endregion

        #region Class Implementation
        
        private void ChangeLineRendererColor()
        {
            if (m_lineMeleeIndicator.IsNull())
            {
                return;
            }


            m_lineMeleeIndicator.startColor = currentOwner.playerColor;
            m_lineMeleeIndicator.endColor = currentOwner.playerColor;
        }

        private void PointMelee()
        {
            if (!m_indicator.IsNull())
            {
                m_indicator.Play();
            }
            
            var _meleePoint = currentOwner.transform.position +
                            (aimDirection.normalized * currentRange);

            m_hitAmount = Physics.OverlapSphereNonAlloc(_meleePoint, currentScale, m_hitColliders,
                m_meleeAbilityData.collisionDetectionLayers);

            if (m_hitAmount == 0)
            {
                return;
            }

            for (int i = 0; i < m_hitAmount; i++)
            {
                if (m_previouslyHitColliders.Contains(m_hitColliders[i]))
                {
                    continue;
                }
                
                m_hitColliders[i].TryGetComponent(out BaseCharacter _character);
                
                if (_character == currentOwner)
                {
                    continue;
                }
                
                //first check if ball -> hit ball
                HitCollider(m_hitColliders[i]);
            }
            
        }

        private void BeamAttack()
        {
            if (!m_particleSystem.IsNull())
            {
                var _main = m_particleSystem.main;
                _main.startSizeY = currentRange;
            }
         
            var _endPoint = transform.position +
                            (aimDirection.normalized * currentRange);

            var midPoint = transform.position +
                           (aimDirection.normalized * currentRange) / 2;

            if (!m_particleSystem.IsNull())
            {
                m_particleSystem.transform.position = midPoint;
            }
            
            JuiceGameController.Instance.CreateRangeIndicator(currentOwner.transform.position, _endPoint, currentScale);
            
            m_hitAmount = Physics.OverlapCapsuleNonAlloc(transform.position, _endPoint, 
                currentScale, m_hitColliders ,m_meleeAbilityData.collisionDetectionLayers);

            if (m_hitAmount == 0)
            {
                return;
            }

            for (int i = 0; i < m_hitAmount; i++)
            {
                if (m_previouslyHitColliders.Contains(m_hitColliders[i]))
                {
                    continue;
                }
                
                m_hitColliders[i].TryGetComponent(out BaseCharacter _character);
                
                if (_character == currentOwner)
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
                _ball.HitBall(aimDirection * m_meleeAbilityData.knockbackDirectionMod, 
                    m_meleeAbilityData.ballHitStrength, currentOwner);
            }

            _collider.TryGetComponent(out IDamagable _damagable);
            _collider.TryGetComponent(out IKnockbackable _knockbackable);
            
            _knockbackable?.ApplyKnockback(currentOwner.transform, currentOwner , currentKnockback, 
                aimDirection * m_meleeAbilityData.knockbackDirectionMod);
            _damagable?.OnDealDamage(currentOwner.transform, currentDamage, currentOwner);
            
            m_previouslyHitColliders.Add(_collider);
        }

        #endregion

        #region IAbility Inherited Methods

        public override async UniTask DoAbility()
        {
            base.DoAbility();

            PlayRandomSound();
            
            switch (m_meleeAbilityData.meleeType)
            {
                case MeleeType.BEAM:
                    BeamAttack();
                    break;
                case MeleeType.CONNECTED_POINT:
                    PointMelee();
                    BeamAttack();
                    break;
                default:
                    PointMelee();
                    break;
            }
            
            //Start Cooldown Timer
            canUseAbility = false;
        }
        
        public override void ShowAttackIndicator(bool _isActive)
        {
            base.ShowAttackIndicator(_isActive);

            switch (m_meleeAbilityData.meleeType)
            {
                case MeleeType.BEAM:
                    DisplayBeamIndicator(_isActive);
                    break;
                case MeleeType.CONNECTED_POINT:
                    DisplayCircleIndicator(_isActive);
                    DisplayBeamIndicator(_isActive);
                    break;
                default:
                    DisplayCircleIndicator(_isActive);
                    break;
            }
           
        }

        private void DisplayCircleIndicator(bool _isActive)
        {
            m_radialMeleeIndicator.transform.localScale = Vector3.one * (currentRange * 2);
            
            m_radialMeleeIndicator.SetActive(_isActive); 
        }

        private void DisplayBeamIndicator(bool _isActive)
        {
            m_lineMeleeIndicator.SetPosition(0, currentOwner.spawnerLocation.position + new Vector3(0,-0.001f, 0));
            m_lineMeleeIndicator.SetPosition(1, currentOwner.spawnerLocation.position +
                                                (aimDirection.normalized * currentRange));

            m_lineMeleeIndicator.startWidth = currentScale;
            m_lineMeleeIndicator.endWidth = currentScale;
            
            m_lineMeleeIndicator.gameObject.SetActive(_isActive);
        }

        public override void InitializeAbility(BaseCharacter _owner, AbilityData _data)
        {
            base.InitializeAbility(_owner, _data);

            ChangeLineRendererColor();
        }

        #endregion
        
        
    }
}