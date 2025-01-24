using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.Gameplay;
using Runtime.GameplayInterfaces;
using UnityEngine;

namespace Runtime.Abilities
{
    public class HookAbility: AbilityBase
    {
        
        #region Serialized Fields

        [SerializeField] private LineRenderer m_hookIndicator;

        [SerializeField] private GameObject m_hookPoint;

        [SerializeField] private LineRenderer m_ropeVisuals;

        [SerializeField] private Transform m_hookReturnPoint;
        
        #endregion

        #region Private Fields
        
        private bool m_hasConnected;

        private Vector3 m_contactPosition;

        private GameObject m_savedEndLocation;

        //These should be changeable with AUGMENTS
        private float m_hookedPlayerTravelDistance;
        
        private float m_currentHookUseTime, m_currentPlayerMoveTime, m_maxPlayerTravelTime;

        private float m_hookPercentage, m_movementPercentage;

        private float m_hookDetectionRange;

        private float m_visualsWaitTime = 1f;

        private float m_percentThreshold = 0.95f;

        private Vector3 m_movementEndPosition, m_movementStartPosition;

        private BaseCharacter m_targetMoveCharacter;

        private BaseCharacter m_hitCharacter, m_stationaryCharacter;

        private Collider[] m_hitColliders = new Collider[6];
        private int m_amountHit;

        #endregion
        
        #region Accessors

        private HookAbilityData m_hookAbilityData => abilityData as HookAbilityData;

        private float m_maxHookDetectionRange => m_hookDetectionRange * currentScale;
        
        #endregion

        #region Class Implementation
        
        

        protected async UniTask T_DoHookAction()
        {
            canUseAbility = false;
            
            GetEndPosition();

            await T_HookMovement();

            if(m_hasConnected)
            {
                await T_DoMovement();
                Debug.Log("Connected");
            }
        }

        protected async UniTask T_HookMovement()
        {
            while (m_hookPercentage < m_percentThreshold && !m_hasConnected)
            {
                if (m_hasConnected)
                {
                    break;
                }
                
                m_currentHookUseTime += Time.deltaTime;
                
                HookHitDetection();
                
                m_hookPercentage = m_currentHookUseTime / currentLifetime;

                m_hookPoint.transform.position = Vector3.Lerp(currentOwner.transform.position, m_endPosition, m_hookPercentage);
                m_ropeVisuals.SetPosition(1, m_hookPoint.transform.position);

                await UniTask.Yield();
            }
            
        }
        
        protected async UniTask T_DoMovement()
        {
            m_targetMoveCharacter = m_hookAbilityData.bringHookedPlayerBack ? m_hitCharacter : currentOwner;
            
            m_hitCharacter.Pause_UnPause_Character(true);
            
            m_movementEndPosition = m_hookAbilityData.bringHookedPlayerBack ? 
                (m_hitCharacter.transform.position - currentOwner.transform.position).normalized 
                + new Vector3(currentOwner.transform.position.x, 
                    m_hitCharacter.transform.position.y, currentOwner.transform.position.z) : 
                (currentOwner.transform.position - m_hitCharacter.transform.position).normalized 
                + new Vector3(m_hitCharacter.transform.position.x, 
                    currentOwner.transform.position.y, m_hitCharacter.transform.position.z);

            m_movementStartPosition = m_hookAbilityData.bringHookedPlayerBack
                ? m_contactPosition
                : currentOwner.transform.position;
            
            m_targetMoveCharacter.EnableCharacterController(false);
            
            await T_PathMovement();
            
            m_targetMoveCharacter.EnableCharacterController(true);
            
            currentOwner.Pause_UnPause_Character(false);
            m_hitCharacter.Pause_UnPause_Character(false);
        }

        protected async UniTask T_PathMovement()
        {
            while (m_movementPercentage < m_percentThreshold)
            {
                m_currentPlayerMoveTime += Time.deltaTime;
                
                m_movementPercentage = m_currentPlayerMoveTime / m_maxPlayerTravelTime;

                m_targetMoveCharacter.transform.position = 
                    Vector3.Lerp(m_movementStartPosition, m_movementEndPosition, m_movementPercentage);

                m_hookPoint.transform.position = m_targetMoveCharacter.transform.position;
                m_ropeVisuals.SetPosition(1, m_hookPoint.transform.position);
                
                if (!m_hookAbilityData.bringHookedPlayerBack)
                {
                    m_ropeVisuals.SetPosition(0, m_targetMoveCharacter.transform.position);
                }
                
                await UniTask.Yield();
            }
        }
        
        private void HookHitDetection()
        {
            if (m_hasConnected)
            {
                return;
            }
            
            //Moving Hook
            m_amountHit = Physics.OverlapSphereNonAlloc(m_hookPoint.transform.position, m_maxHookDetectionRange,
                m_hitColliders, m_hookAbilityData.collisionDetectionLayers);

            if (m_amountHit == 0)
            {
                return;
            }

            for (int i = 0; i < m_amountHit; i++)
            {
                if (m_hasConnected)
                {
                    return;
                }
                
                m_hitColliders[i].TryGetComponent(out BaseCharacter _character);
                
                if (_character == currentOwner)
                {
                    continue;
                }
                
                //first check if ball -> hit ball
                HitCollider(m_hitColliders[i]);

                if (!_character.IsNull())
                {
                    m_hitCharacter = _character;
                }
            }
        }
        
        private void HitCollider(Collider _collider)
        {
            _collider.TryGetComponent(out BallBehavior _ball);
            
            if (!_ball.IsNull())
            {
                _ball.HitBall(aimDirection * -1, 
                    m_hookAbilityData.ballHitStrength, currentOwner);
                return;
            }
            
            _collider.TryGetComponent(out IDamagable _damagable);
            _damagable?.OnDealDamage(currentOwner.transform, currentDamage, currentOwner);

            m_hasConnected = true;
            m_contactPosition = _collider.transform.position;
        }

        #endregion
        
        #region IAbility Inherited Methods

        public override void InitializeAbility(BaseCharacter _owner, AbilityData _data)
        {
            base.InitializeAbility(_owner, _data);
            
            lifeTimeMax = m_hookAbilityData.hookTravelTime;
            
            m_maxPlayerTravelTime = m_hookAbilityData.hookedPlayerReturnTime;
            m_hookDetectionRange = m_hookAbilityData.abilityScale;

            m_hookPercentage = 0;
            m_currentHookUseTime = 0;
            m_movementPercentage = 0;
            m_currentPlayerMoveTime = 0;
            
            m_hookPoint.SetActive(false);
            m_ropeVisuals.gameObject.SetActive(false);
            
            m_hookIndicator.transform.parent = _owner.GetIndicatorParent();
            
            ChangeLineRendererColor();
        }

        public override void ResetAbilityUse()
        {
            base.ResetAbilityUse();
            m_hasConnected = false;
            m_hookPercentage = 0;
            m_currentHookUseTime = 0;
            m_movementPercentage = 0;
            m_currentPlayerMoveTime = 0;
            m_hookPoint.transform.localPosition = Vector3.zero;
        }

        public override async UniTask DoAbility()
        {
            base.DoAbility();
            canUseAbility = false;
            
            m_hookPoint.SetActive(true);
            m_ropeVisuals.SetPosition(0, currentOwner.transform.position);
            m_ropeVisuals.SetPosition(1, m_hookPoint.transform.position);
            m_ropeVisuals.gameObject.SetActive(true);
            
            currentOwner.Pause_UnPause_Character(true);

            PlayRandomSound();
            
            await T_DoHookAction();
            
            currentOwner.Pause_UnPause_Character(false);
            
            m_hookPoint.SetActive(false);
            m_ropeVisuals.gameObject.SetActive(false);
        }
        
        public override void ShowAttackIndicator(bool _isActive)
        {
            base.ShowAttackIndicator(_isActive);

            GetEndPosition();
            
            m_hookIndicator.SetPosition(0, currentOwner.spawnerLocation.position + new Vector3(0,-0.001f, 0));
            m_hookIndicator.SetPosition(1, m_endPosition);

            m_hookIndicator.startWidth = currentScale;
            m_hookIndicator.endWidth = currentScale;
            
            m_hookIndicator.gameObject.SetActive(_isActive);
        }
        
        private void ChangeLineRendererColor()
        {
            if (m_hookIndicator.IsNull())
            {
                return;
            }


            m_hookIndicator.startColor = currentOwner.playerColor;
            m_hookIndicator.endColor = currentOwner.playerColor;
        }

        #endregion
        
        
        
    }
}