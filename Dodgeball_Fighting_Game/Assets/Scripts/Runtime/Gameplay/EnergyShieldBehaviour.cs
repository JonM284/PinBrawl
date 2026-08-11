using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.Gameplay.Sensors;
using Runtime.GameplayInterfaces;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class EnergyShieldBehaviour: MonoBehaviour
    {
        #region Actions

        public static event Action<BaseCharacter, float> OnEnergyAmountChanged;
        
        #endregion
        
        #region Serialized Fields
        
        [SerializeField] private Transform m_energyShieldParent;
        [SerializeField] private GameObject m_energyShieldIndicator;

        #endregion

        #region Private Fields

        //ToDo: add to settings
        private float m_energyMax = 50f, m_energyDepleteAmount = 5f, m_energyRechargeAmount = 1f, m_energyBubbleMaxScale = 0.6f;

        private BaseCharacter ownerCharacter;

        private SemaphoreSlim rechargeSemaphore = new SemaphoreSlim(1, 1);

        private CancellationTokenSource destroyCancellation;

        #endregion
        
        #region Accessor

        public bool isParrying { get; private set; }

        public bool isShielding { get; private set; }

        public float CurrentEnergy { get; private set; }

        #endregion

        #region Class Implementation

        public void Initialize(BaseCharacter _ownerCharacter)
        {
            ownerCharacter = _ownerCharacter;
            CurrentEnergy = m_energyMax;
            destroyCancellation = new CancellationTokenSource();
            EnableShield(false);
        }

        public void EnableShield(bool _isActive)
        {
            if (m_energyShieldIndicator.IsNull())
            {
                return;
            }
            
            if (_isActive)
            {
                m_energyShieldParent.LookAt(ownerCharacter.mainCamera.transform);
            }
            
            m_energyShieldIndicator.SetActive(_isActive);
        }
        
        private void UpdateShieldScale()
        {
            m_energyShieldIndicator.transform.localScale = Vector3.one * (((CurrentEnergy / m_energyMax) * m_energyBubbleMaxScale) + 0.2f);
        }
        
        public void AddEnergy(float _amountToAdd)
        {
            CurrentEnergy = Mathf.Clamp(CurrentEnergy + _amountToAdd, 0, m_energyMax);
            OnEnergyAmountChanged?.Invoke(ownerCharacter, CurrentEnergy);
            
            UpdateShieldScale();
        }

        public void RemoveEnergy(float _amountToRemove)
        {
            CurrentEnergy = Mathf.Clamp(CurrentEnergy - _amountToRemove, -1, m_energyMax);
            OnEnergyAmountChanged?.Invoke(ownerCharacter, CurrentEnergy);
            
            UpdateShieldScale();
        }
        
        private async UniTask RechargeShieldAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            await rechargeSemaphore.WaitAsync(cancellationToken: token);

            try
            {
                while (CurrentEnergy < m_energyMax)
                {
                    if (isShielding)
                    {
                        break;
                    }

                    AddEnergy(m_energyRechargeAmount * Time.deltaTime);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            finally
            {
                Debug.Log("Exit Recharge");
                rechargeSemaphore.Release();
            }
        }
        
        public void SetParrying(bool _isParrying)
        {
            isParrying = _isParrying;
        }
        
        public void UseEnergyShield()
        {
            isShielding = true;
            
            if (!m_energyShieldIndicator.activeSelf)
            {
                EnableShield(true);
            }
            
            if (CurrentEnergy <= 0)
            {
                PopEnergyShield();
                return;
            }

            RemoveEnergy(m_energyDepleteAmount * Time.deltaTime);
        }

        public void EndEnergyShield()
        {
            isShielding = false;
            if (CurrentEnergy < m_energyMax)
            {
                RechargeShieldAsync(destroyCancellation.Token).Forget();
            }
        }

        protected void PopEnergyShield()
        { 
            ownerCharacter.OnPopEnergyShield();
        }

        public void HalfRegenShieldEnergy()
        {
            CurrentEnergy = m_energyMax / 2;
        }

        
        #endregion
        
    }
}