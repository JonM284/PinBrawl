using System;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class DashReturnPointEntity: MonoBehaviour
    {

        #region Serialized Field

        [SerializeField] private LineRenderer m_lineVisuals;

        [SerializeField] private Transform m_connectionPoint;

        #endregion
        
        #region Private Fields

        private BaseCharacter m_owner;

        private float m_timer;

        private bool m_isInitialized;

        private string m_poolName;

        private Action m_endAction;

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            BaseCharacter.OnPlayerDeath += OnPlayerDeath;
        }

        private void OnDisable()
        {
            BaseCharacter.OnPlayerDeath -= OnPlayerDeath;
        }

        private void Update()
        {
            if (!m_isInitialized)
            {
                return;
            }

            m_timer -= Time.deltaTime;

            m_lineVisuals.SetPosition(1, m_owner.transform.position);

            if (m_timer > 0)
            {
                return;
            }
            
            m_endAction?.Invoke();
        }

        #endregion
        
        #region Class Implementation

        public void Initialize(BaseCharacter _baseCharacter, string _poolName ,float _duration, Action onEndDuration)
        {
            if (_baseCharacter.IsNull())
            {
                DestroyObject();
                return;
            }

            m_owner = _baseCharacter;
            m_timer = _duration;
            m_endAction = onEndDuration;

            m_poolName = _poolName;
            
            m_lineVisuals.SetPosition(0, m_connectionPoint.position);
            
            m_isInitialized = true;
        }
        
        private void OnPlayerDeath(BaseCharacter _deadCharacter, BaseCharacter _attacker, Vector3 _deathPosition)
        {
            if (m_owner.IsNull())
            {
                DestroyObject();
            }
            
            if (_deadCharacter != m_owner)
            {
                return;
            }

            DestroyObject();
        }

        public void DestroyObject()
        {
            m_lineVisuals.SetPosition(0,Vector3.zero);
            m_lineVisuals.SetPosition(1,Vector3.zero);
            ObjectPoolController.Instance.ReturnToPool(m_poolName, gameObject);
        }

        #endregion
        
        
        
    }
}