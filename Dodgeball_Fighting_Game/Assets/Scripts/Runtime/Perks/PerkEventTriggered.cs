using Data.PerkDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Perks
{
    [RequireComponent(typeof(PerkEvents))]
    public class PerkEventTriggered: PerkEntityBase
    {
        
        #region Private Fields

        private PerkEvents m_perkEventRef;

        #endregion
        
        #region Events

        public UnityEvent onEventCall;
        public UnityEvent onReset;

        #endregion

        #region Accessors

        private PerkEvents m_perkEvent => CommonUtils.GetRequiredComponent(ref m_perkEventRef, GetComponent<PerkEvents>);
        
        private EventTriggeredPerkData m_eventTriggeredPerkData => currentPerkData as EventTriggeredPerkData;

        #endregion
        
        
        #region Unity Events

        private void OnEnable()
        {
            BaseCharacter.OnPlayerDeath += OnPlayerDeath;
            BaseCharacter.OnPlayerPreDeath += OnPlayerPreDeath;
            BaseCharacter.OnArmorAmountChanged += OnCharacterDamaged;
            BallBehavior.OnBallSwap += OnBallSwap;
            ProjectileEntityBase.OnProjectileEnded += OnProjectileEnded;
            MatchGameController.OnRoundStart += ResetOnRound;
            MatchGameController.OnNewSetStart += ResetOnNewSet;
        }

        private void OnDisable()
        {
            BaseCharacter.OnPlayerDeath -= OnPlayerDeath;
            BaseCharacter.OnPlayerPreDeath -= OnPlayerPreDeath;
            BaseCharacter.OnArmorAmountChanged -= OnCharacterDamaged;
            BallBehavior.OnBallSwap -= OnBallSwap;
            ProjectileEntityBase.OnProjectileEnded -= OnProjectileEnded;
            MatchGameController.OnRoundStart -= ResetOnRound;
            MatchGameController.OnNewSetStart -= ResetOnNewSet;
        }

        #endregion

        #region PerkEntityBase Inherited Methods

        public override void OnApply(BaseCharacter _baseCharacter, PerkDataBase _perkData)
        {
            base.OnApply(_baseCharacter, _perkData);
            m_perkEvent.Initialize(m_eventTriggeredPerkData);
        }

        #endregion


        #region Class Implementation

        //Owner kills another player
        private void OnPlayerDeath(BaseCharacter _dyingPlayer, BaseCharacter _attackingPlayer, Vector3 _deathPosition)
        {
            if (m_eventTriggeredPerkData.listenState != EventListenState.OWNER_KILL_OTHER)
            {
                return;
            }
            
            if (_attackingPlayer != currentOwner)
            {
                return;
            }
            
            onEventCall?.Invoke();
        }
        
        //Right before owner Dies
        private void OnPlayerPreDeath(BaseCharacter _dyingCharacter)
        {
            if (m_eventTriggeredPerkData.listenState != EventListenState.OWNER_PRE_DIED)
            {
                return;
            }

            if (_dyingCharacter != currentOwner)
            {
                return;
            }
            
            onEventCall?.Invoke();
        }
        
        
        private void OnCharacterDamaged(BaseCharacter _damagedCharacter, float _damageAmount, BaseCharacter _attackingCharacter)
        {
            if (m_eventTriggeredPerkData.listenState != EventListenState.OWNER_DAMAGED &&
                m_eventTriggeredPerkData.listenState != EventListenState.OWNER_DAMAGE_OTHER)
            {
                return;
            }   
            
            if(_damagedCharacter != currentOwner 
               && (!_attackingCharacter.IsNull() && _attackingCharacter != currentOwner))
            {
                return;
            }

            if (_damagedCharacter == currentOwner)
            {
                CheckOnDamaged();
            }else if (_attackingCharacter == currentOwner)
            {
                CheckOnAttack();
            }
            
        }

        //Owner was Damaged
        private void CheckOnDamaged()
        {
            if (m_eventTriggeredPerkData.listenState != EventListenState.OWNER_DAMAGED)
            {
                return;
            }
            
            onEventCall?.Invoke();
        }
        
        //Owner Damaged another player
        private void CheckOnAttack()
        {
            if (m_eventTriggeredPerkData.listenState != EventListenState.OWNER_DAMAGE_OTHER)
            {
                return;
            }
            
            onEventCall?.Invoke();
        }

        //Owner swaps ball
        private void OnBallSwap(BaseCharacter _lastWackCharacter)
        {
            if (m_eventTriggeredPerkData.listenState != EventListenState.OWNER_SWAP_BALL)
            {
                return;
            }
            
            if (_lastWackCharacter != currentOwner)
            {
                return;
            }
            
            onEventCall?.Invoke();
        }
        
        //Owner's projectile ended
        private void OnProjectileEnded(BaseCharacter _shooter)
        {
            if (m_eventTriggeredPerkData.listenState != EventListenState.OWNER_PROJECTILE_END)
            {
                return;
            }
            
            if (_shooter != currentOwner)
            {
                return;
            }
            
            onEventCall?.Invoke();
        }

        private void ResetOnRound()
        {
            OnReset();
        }

        private void ResetOnNewSet()
        {
            OnReset();
        }

        public override void OnReset()
        {
            base.OnReset();
            onReset?.Invoke();
        }

        #endregion
        
        
        
        
    }
}