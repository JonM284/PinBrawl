using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using DG.Tweening;
using GameControllers;
using Project.Scripts.Utils;
using Rewired;
using Runtime.GameControllers;
using UnityEngine;

namespace Runtime.Character
{
    
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class PracticeDummyCharacter: BaseCharacter
    {

        [SerializeField] private float m_dummyDamageAmount;
        [SerializeField] private float m_dummyKillAmount;
        
        #region Private Fields

        private Vector3 m_startLocation;

        #endregion
        
        #region Unity Events

        private void Update()
        {
            if (!m_isAlive || !m_isInitialized)
            {
                return;
            }

            CheckStatusCooldown();
            
            if (!m_isKnockedBack)
            {
                return;
            }
            
            DoKnockback();
        }

        #endregion
        
        #region Class Implementation
        
        public override async UniTask InitializeCharacter(CharacterData _characterData, int _index, Player _player,
            LayerMask _groundMask, LayerMask _wallMask)
        {
            if (_characterData.IsNull())
            {
                return;
            }

            Debug.Log("Initialize Player");
            m_playerIndex = _index;

            m_groundMask = _groundMask;
            m_wallLayerMask = _wallMask;
            
            characterData = _characterData;

            m_currentMaxArmor = characterData.characterArmorAmount;
            m_currentArmor = characterData.characterArmorAmount;

            m_originalSpeed = characterData.characterWalkSpeed;
            m_currentSpeed = m_originalSpeed;
            
            m_currentEnergy = m_energyMax / 4f;

            m_playerKillEnergyAddAmount = m_dummyKillAmount;
            m_pvpDamageEnergyAddAmount = m_dummyDamageAmount;

            m_startLocation = transform.position;
            
            m_isInitialized = true;
        }

        #region Inherited Unused Methods

        protected override void PokeAbility() { }

        #endregion

        #endregion

        #region IDamagable Inherited Methods

        public override void OnRevive()
        {
            m_currentArmor = m_currentMaxArmor;
            m_isAlive = true;
        }

        public override void OnHeal(float _healAmount)
        {
            m_currentArmor = Mathf.Clamp(m_currentArmor + Mathf.RoundToInt(_healAmount), 0, m_currentMaxArmor);
        }

        public void OnDealDamage(Transform _attacker, float _damageAmount, BaseCharacter _attackingCharacter = null)
        {
            
            if (_damageAmount <= 0)
            {
                return;
            }

            _damageAmount = Mathf.CeilToInt(_damageAmount);

            var _damageIntakeAmount = Mathf.CeilToInt(_damageAmount * m_damageIntakeMod);
            m_currentArmor = Mathf.Clamp(m_currentArmor - _damageIntakeAmount, 0, m_currentMaxArmor);

            if (!_attackingCharacter.IsNull())
            {
                _attackingCharacter.AddEnergy(m_pvpDamageEnergyAddAmount);
            }
            
            JuiceGameController.Instance.CreateDamageText(_damageIntakeAmount, transform.position);
        }

        public override void OnKillPlayer(Vector3 _deathPosition, Vector3 _deathDirection)
        {
            m_characterMoveVector = Vector3.zero;
            m_isKnockedBack = false;
            m_knockbackForce = 0;
            m_knockbackTime = 0;

            VFXController.Instance.ForceDeathVFX(_deathPosition);

            MatchGameController.Instance.TempKillDummyCharacter(this, _deathPosition, _deathDirection, m_startLocation);
        }

        #endregion


        #region IKnockbackable Inherited Methods

       private async UniTask T_OnKnockback()
        {
            Debug.Log($"Current Hit Stun Settings: Time= {m_currentHitStunTime} Frequency= {m_hitStunV3FrequencyCurrent}, Vibrato= {m_currentVirbrato}");
            
            m_characterModelHolder.DOShakePosition( m_currentHitStunTime, m_hitStunV3FrequencyCurrent , m_currentVirbrato,
                0);

            await UniTask.WaitForSeconds(m_currentHitStunTime);

            m_characterModelHolder.localPosition = Vector3.zero;
            
            StartKnockBack();
        }

        public void ApplyKnockback(Transform _attackerTransform, BaseCharacter _lastAttacker, 
            float _baseKnockbackAmount, Vector3 _forcedDirection, bool _isBallHit = false)
        {
            if (_baseKnockbackAmount <= 0)
            {
                return;
            }

            PlayDamageSFX();
            
            m_knockbackForce = _baseKnockbackAmount * (m_currentArmor > 0 ? 1 : _isBallHit ? 10 : 2.5f) *
                               (1 - 0.2f);
            
            m_knockbackMoveVector = _forcedDirection == Vector3.zero ? transform.position - _attackerTransform.position : _forcedDirection.FlattenVector3Y();
            
            //ToDo: can use end point calculation to do a SmashBros last hit FX
            //var m_knockbackEndPos = transform.position + (m_knockbackMoveVector.normalized * _baseKnockbackAmount);
            
            float _percentage = m_knockbackForce / m_hitStunKnockbackThreshold;

            m_currentHitStunTime = m_hitStunMaxTime * _percentage;
            m_currentHitStunFrequency = m_hitStunMaxFrequency * _percentage;
            m_currentVirbrato = Mathf.CeilToInt(m_hitStunMaxVirbrato * _percentage);
            m_hitStunV3FrequencyCurrent = m_knockbackMoveVector.normalized * m_currentHitStunFrequency;

            VFXController.Instance.PlayDamageVFX(transform.position, Quaternion.identity);
            
            //ToDo: Controller Vibration
            //m_player.SetVibration(0, 0.1f, 0.1f);
            T_OnKnockback();
        }

        #endregion
        
        
    }
}