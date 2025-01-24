using System;
using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay;
using UnityEngine;

namespace Runtime.Abilities
{
    public class CreationAbility: AbilityBase
    {

        #region Serialized Fields

        [SerializeField] private GameObject m_cursorIndicator;

        #endregion

        #region Protected Fields

        protected Vector3 m_inputAmount, m_testLocation, m_wallCheckDirection;

        protected float m_aimSpeedMod = 18f, m_wallCheckDistance, m_inverseDir = -1, m_axisThreshold = 0.25f;

        protected bool m_aimingRightStick;

        #endregion
        
        #region Accessors

        private CreationAbilityData m_creationAbilityData => abilityData as CreationAbilityData;
        
        #endregion

        #region IAbility Inherited Methods

        protected bool IsFacingWall()
        {
            m_hitWallsAmount = Physics.RaycastNonAlloc(currentOwner.transform.position, m_wallCheckDirection, 
                m_hitWalls, m_wallCheckDistance, m_wallLayer);

            return m_hitWallsAmount > 0;
        }
        
        public override async UniTask PreLoadNecessaryObjects()
        {
            await ObjectPoolController.Instance.T_PreCreateObject(m_creationAbilityData.name,
                m_creationAbilityData.creationPrefab);
        }

        public override void InitializeAbility(BaseCharacter _owner, AbilityData _data)
        {
            base.InitializeAbility(_owner, _data);

            lifeTimeMax = m_creationAbilityData.maxTimeAlive;
            
            m_cursorIndicator.transform.parent = _owner.GetIndicatorParent();
            m_abilityCastRangeIndicator.transform.parent = _owner.GetIndicatorParent();
        }

        public override async UniTask DoAbility()
        {
            base.DoAbility();
            
            m_previouslyHitColliders.Clear();
            
            canUseAbility = false;
            
            var _instantiatedObj =  await ObjectPoolController.Instance.T_CreateObject(m_creationAbilityData.name, 
                m_creationAbilityData.creationPrefab, m_endPosition);

            _instantiatedObj.transform.forward = currentOwner.m_playerAimVector.normalized;

            _instantiatedObj.TryGetComponent(out CreationEntityBase _creationEntity);

            PlayRandomSound();

            if (_creationEntity.IsNull())
            {
                ObjectPoolController.Instance.ReturnToPool(m_creationAbilityData.name, gameObject);
                return;
            }
            
            await _creationEntity.Initialize(currentOwner, m_creationAbilityData, currentScale,
                currentDamage, currentKnockback, currentLifetime);
            
            m_aimingRightStick = false;
            
        }
        
        public override void ShowAttackIndicator(bool _isActive)
        {
            
            if (Mathf.Abs(currentOwner.playerRStickInput.magnitude) > m_axisThreshold && m_aimingRightStick == false)
            {
                m_aimingRightStick = true;
            }

            aimDirection = m_aimingRightStick ? currentOwner.playerRStickInput.normalized : currentOwner.m_playerAimVector.normalized;
            
            if (m_lastActiveState != _isActive)
            {
                //Runs 1 time
                //turn on indicator

                SetupCursorLocation(_isActive);
                m_lastActiveState = _isActive;
            }

            UpdateCursorLocation();
        }

        private void SetupCursorLocation(bool _isActive)
        {
            m_wallCheckDirection = aimDirection;
            m_wallCheckDistance = currentRange;
                
            if (IsFacingWall())
            {
                m_testLocation = new Vector3(m_hitWalls[0].point.x, currentOwner.GetGroundY(),
                    m_hitWalls[0].point.z);                }
            else
            {
                m_testLocation = currentOwner.transform.position + (aimDirection.normalized * (currentRange - 0.01f));
            }
                
            m_cursorIndicator.SetActive(_isActive);
            m_cursorIndicator.transform.localScale = Vector3.one * (currentScale * 2);
            m_abilityCastRangeIndicator.SetActive(_isActive);
            m_abilityCastRangeIndicator.transform.localScale = Vector3.one * (currentRange * 2);
            m_endPosition = new Vector3(m_testLocation.x, currentOwner.GetGroundY(),
                m_testLocation.z);
            m_cursorIndicator.transform.position = m_endPosition;
        }

        private void UpdateCursorLocation()
        {

            if (!m_aimingRightStick)
            {
                //aiming will default to farthest point in range when aiming with left stick
                m_testLocation = (aimDirection * currentRange);
            }
            else
            {
                m_inputAmount += aimDirection * (m_aimSpeedMod * Time.deltaTime);
                m_inputAmount = Vector3.ClampMagnitude(m_inputAmount, currentRange);
                m_testLocation = currentOwner.transform.position + m_inputAmount;
                m_testLocation = Vector3.ClampMagnitude(m_testLocation - currentOwner.transform.position, currentRange);
            }

            m_wallCheckDirection = m_testLocation;
            m_wallCheckDistance = m_wallCheckDirection.magnitude;
            
            if (IsFacingWall())
            {
                m_endPosition = new Vector3(m_hitWalls[0].point.x, currentOwner.GetGroundY(),
                    m_hitWalls[0].point.z);
            }
            else
            {
                m_endPosition = new Vector3(currentOwner.transform.position.x + m_testLocation.x,
                    currentOwner.GetGroundY(), currentOwner.transform.position.z + m_testLocation.z);
            }
            
            m_cursorIndicator.transform.position = m_endPosition;
        }

        #endregion
        
        
    }
}