using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.GameplayInterfaces;
using UnityEngine;

namespace Runtime.Abilities
{
    public class DashAbilityBase: AbilityBase
    {

        #region Serialized Fields

        [SerializeField] private LineRenderer m_lineAttackIndicator;

        [SerializeField] private GameObject m_pointTeleportIndicator;
        
        #endregion

        #region Private Fields

        private bool m_hasUsedAbility;

        private Vector3 m_startPosition, m_knockbackDir;

        private GameObject m_savedEndLocation;

        private float m_currentTravelTime, m_currentPercentage;
        
        private float m_visualsWaitTime = 1f;
        
        private Collider[] m_hitColliders = new Collider[6];
        private int m_hitCollidersAmount;
        
        protected Vector3 m_inputAmount, m_testLocation, m_wallCheckDirection, m_movementProxyPosition;

        protected float m_aimSpeedMod = 18f, m_wallCheckDistance, m_inverseDir = -1, m_axisThreshold = 0.25f;

        protected bool m_aimingRightStick;

        #endregion
        
        #region Accessors

        private DashAbilityData m_dashAbilityData => abilityData as DashAbilityData;
        
        #endregion

        #region Class Implementation

        protected void DoTeleport()
        {
            currentOwner.TeleportPlayer(m_endPosition);
        }

        protected void CheckDashMelee()
        {
            switch (m_dashAbilityData.movementType)
            {
                case MovementType.REACTIVATE:
                    BeamAttack();
                    break;
                default:
                    PointMelee();
                    break;
            }
        }


        private void CurrentPointMelee()
        {
            m_hitCollidersAmount = Physics.OverlapSphereNonAlloc(transform.position, currentScale, m_hitColliders,
                m_dashAbilityData.collisionDetectionLayers);

            if (m_hitCollidersAmount == 0)
            {
                return;
            }

            for (int i = 0; i < m_hitCollidersAmount; i++)
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
                
                ApplyStatus(_character);
                
                //first check if ball -> hit ball
                HitCollider(m_hitColliders[i]);
            }
        }
        
        private void PointMelee()
        {
            m_hitCollidersAmount = Physics.OverlapSphereNonAlloc(m_endPosition, currentScale, m_hitColliders,
                m_dashAbilityData.collisionDetectionLayers);

            if (m_hitCollidersAmount == 0)
            {
                return;
            }

            for (int i = 0; i < m_hitCollidersAmount; i++)
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
                
                ApplyStatus(_character);
                
                //first check if ball -> hit ball
                HitCollider(m_hitColliders[i]);
            }
        }

        private void BeamAttack()
        {
            m_hitCollidersAmount = Physics.OverlapCapsuleNonAlloc(currentOwner.transform.position, m_endPosition, 
                currentScale, m_hitColliders,m_dashAbilityData.collisionDetectionLayers);

            if (m_hitCollidersAmount == 0)
            {
                return;
            }

            for (int i = 0; i < m_hitCollidersAmount; i++)
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

                ApplyStatus(_character);
                
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

            m_knockbackDir = !m_savedEndLocation.IsNull()
                ? m_savedEndLocation.transform.position - currentOwner.transform.position
                : aimDirection;
            
            if (!_ball.IsNull())
            {
                _ball.HitBall(m_knockbackDir * m_dashAbilityData.knockbackDirectionMod, 
                    m_dashAbilityData.ballHitStrength, currentOwner);
                return;
            }
            
            if (m_dashAbilityData.movementType == MovementType.JUMP || m_dashAbilityData.movementType == MovementType.TELEPORT)
            {
                m_knockbackDir = Vector3.zero;
            }

            if (currentKnockback > 0)
            {
                _collider.TryGetComponent(out IKnockbackable _knockbackable);
                
                _knockbackable?.ApplyKnockback(currentOwner.transform, currentOwner , currentKnockback, 
                    m_knockbackDir * m_dashAbilityData.knockbackDirectionMod);
            }

            if (currentDamage > 0)
            {
                _collider.TryGetComponent(out IDamagable _damagable);
                
                _damagable?.OnDealDamage(currentOwner.transform, currentDamage, currentOwner);
            }

            
            m_previouslyHitColliders.Add(_collider);
        }
        
        private void ApplyStatus(BaseCharacter _character)
        {
            if (_character.IsNull())
            {
                return;
            }
            
            if (m_dashAbilityData.applicableStatusesOnHit.Count <= 0)
            {
                return;
            }
            
            foreach (var _statusData in m_dashAbilityData.applicableStatusesOnHit)
            {
                if(_character.IsNull() || _character.ContainsStatus(_statusData)){
                    continue;   
                }

                _character.ApplyStatus(_statusData);
            }
        }

        protected void DoInstantDash()
        {
            //ToDo: Start up time
            
            JuiceGameController.Instance.CreateRangeIndicator(currentOwner.transform.position, m_endPosition, currentScale);

            PlayRandomSound();
            
            //Teleport
            DoTeleport();
            
            //Check Melee
            CheckDashMelee();
                
            //DoVFX
            //ToDo: VFX
            
                
            //Start Cooldown Timer
            canUseAbility = false;
        }

        protected void DoReactivateTeleport()
        {
            //Do Ability
            m_endPosition = m_savedEndLocation.transform.position;
                    
            JuiceGameController.Instance.CreateRangeIndicator(currentOwner.transform.position, m_endPosition, currentScale);

            PlayRandomSound();
            
            //Check Melee
            CheckDashMelee();
            //Teleport
            DoTeleport();
                
            //DoVFX
            //ToDo: VFX
                
            //Start Cooldown Timer
            canUseAbility = false;
            ObjectPoolController.Instance.ReturnToPool(m_dashAbilityData.name , m_savedEndLocation);
        }

        private void OnAutoReactivate()
        {
            DoReactivateTeleport();
            currentOwner.AddAbilityCooldown(this);
        }

        protected async UniTask SetupReactivateTeleport()
        {
            Debug.Log("Setup Location");

            var _dashPointObject = await ObjectPoolController.Instance.T_CreateObject(m_dashAbilityData.name, 
                m_dashAbilityData.previousLeftPoint.gameObject, currentOwner.transform.position);

            _dashPointObject.TryGetComponent(out DashReturnPointEntity _dashPointEntity);
            
            _dashPointEntity.Initialize(currentOwner, m_dashAbilityData.name ,m_dashAbilityData.reactivationTime, OnAutoReactivate);
            
            m_savedEndLocation = _dashPointObject.gameObject;
            
            m_hasUsedAbility = true;
        }

        protected async UniTask DoPathMovement()
        {
            canUseAbility = false;

            PlayRandomSound();

            if (m_dashAbilityData.movementType == MovementType.ALONG_PATH)
            {
                if (IsFacingWall())
                {
                    m_testLocation = new Vector3(m_hitWalls[0].point.x, currentOwner.GetGroundY(),
                        m_hitWalls[0].point.z);                }
                else
                {
                    m_testLocation = currentOwner.transform.position + (aimDirection.normalized * (currentRange - 0.01f));
                }
                
            }
            
            currentOwner.EnableCharacterController(false);
            
            if (m_dashAbilityData.movementType == MovementType.ALONG_PATH)
            {
                JuiceGameController.Instance.CreateRangeIndicator(currentOwner.transform.position, m_endPosition, currentScale);
            }
            
            await T_PathMovement();
        }

        protected async UniTask T_PathMovement()
        {
            m_currentPercentage = 0;
            m_currentTravelTime = 0;
            m_startPosition = currentOwner.transform.position;
            
            float _calculatedTime = CommonUtils.GetTimeFromDistanceAndSpeed(
                (Vector3.Distance(m_endPosition, currentOwner.transform.position)), currentSpeed);
            
            while (m_currentPercentage < 0.9f)
            {
                if (m_currentPercentage >= 0.9f)
                {
                    break;
                }
                
                m_currentTravelTime += Time.deltaTime;

                if (m_dashAbilityData.movementType == MovementType.ALONG_PATH)
                {
                    CurrentPointMelee();
                }
                
                m_currentPercentage = m_currentTravelTime / _calculatedTime;

                m_movementProxyPosition = Vector3.Lerp(m_startPosition,
                    m_endPosition, m_currentPercentage);

                if (m_dashAbilityData.movementType == MovementType.JUMP)
                {
                    m_movementProxyPosition.y = m_startPosition.y + m_dashAbilityData.jumpCurve.Evaluate(m_currentPercentage);
                }
                
                currentOwner.transform.position = m_movementProxyPosition;
                
                await UniTask.Yield();
            }

            currentOwner.transform.position = new Vector3(m_endPosition.x, m_startPosition.y, m_endPosition.z);

            if (m_dashAbilityData.movementType == MovementType.JUMP)
            {
                CheckDashMelee();
            }
            
            currentOwner.EnableCharacterController(true);
        }
        
        private void ChangeLineRendererColor()
        {
            if (m_lineAttackIndicator.IsNull())
            {
                return;
            }


            m_lineAttackIndicator.startColor = currentOwner.playerColor;
            m_lineAttackIndicator.endColor = currentOwner.playerColor;
        }
        
        protected bool IsFacingWall()
        {
            m_hitWallsAmount = Physics.RaycastNonAlloc(currentOwner.transform.position, m_wallCheckDirection, 
                m_hitWalls, m_wallCheckDistance, m_wallLayer);

            return m_hitWallsAmount > 0;
        }
        
        #endregion
        
        #region IAbility Inherited Methods
        
        public override void InitializeAbility(BaseCharacter _owner, AbilityData _data)
        {
            base.InitializeAbility(_owner, _data);
            
            speedAmountMax = m_dashAbilityData.dashSpeed;

            m_lineAttackIndicator.transform.parent = _owner.GetIndicatorParent();

            ChangeLineRendererColor();
        }


        public override void ShowAttackIndicator(bool _isActive)
        {
            if (m_dashAbilityData.movementType == MovementType.REACTIVATE)
            {
                //rewind ability
                return;
            }

            switch (m_dashAbilityData.movementType)
            {
                case MovementType.JUMP: case MovementType.TELEPORT:
                    
                    if (Mathf.Abs(currentOwner.playerRStickInput.magnitude) >= m_axisThreshold && m_aimingRightStick == false)
                    {
                        m_aimingRightStick = true;
                    }

                    aimDirection = m_aimingRightStick ? 
                        currentOwner.playerRStickInput.normalized : 
                        currentOwner.m_playerAimVector.normalized;
                    
                    if (m_lastActiveState != _isActive)
                    {
                        //Runs 1 time
                        //turn on indicator
                        SetupCursorLocation(_isActive);
                        m_lastActiveState = _isActive;
                    }

                    UpdateCursorLocation();
                    
                    break;
                case MovementType.ALONG_PATH:
                    aimDirection = currentOwner.m_playerAimVector.normalized;
                    
                    UpdatePathIndicator(_isActive);
                    break;
            }
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
                
            m_pointTeleportIndicator.SetActive(_isActive);
            m_pointTeleportIndicator.transform.localScale = Vector3.one * (currentScale * 2);
            m_abilityCastRangeIndicator.SetActive(_isActive);
            m_abilityCastRangeIndicator.transform.localScale = Vector3.one * (currentRange * 2);
            m_endPosition = new Vector3(m_testLocation.x, currentOwner.GetGroundY(),
                m_testLocation.z);
            m_pointTeleportIndicator.transform.position = m_endPosition;
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
            
            m_pointTeleportIndicator.transform.position = m_endPosition;
        }

        private void UpdatePathIndicator(bool _isActive)
        {
            if (IsFacingWall())
            {
                m_endPosition = new Vector3(m_hitWalls[0].point.x, currentOwner.GetGroundY(),
                    m_hitWalls[0].point.z);                }
            else
            {
                m_endPosition = currentOwner.transform.position + (aimDirection.normalized * (currentRange - 0.01f));
            }
            
            Debug.Log("Updating Path Indicator");
            
            m_lineAttackIndicator.SetPosition(0, currentOwner.spawnerLocation.position + new Vector3(0,-0.001f, 0));
            m_lineAttackIndicator.SetPosition(1, m_endPosition);

            m_lineAttackIndicator.startWidth = currentScale;
            m_lineAttackIndicator.endWidth = currentScale;
            
            if (m_lastActiveState != _isActive)
            {
                //Runs 1 time
                //turn on/off indicator
                m_lineAttackIndicator.gameObject.SetActive(_isActive);
                m_lastActiveState = _isActive;
            }
        }
        
        public override async UniTask PreLoadNecessaryObjects()
        {
            if (m_dashAbilityData.previousLeftPoint.IsNull())
            {
                return;
            }
            
            await ObjectPoolController.Instance.T_PreCreateObject(m_dashAbilityData.name,
                m_dashAbilityData.previousLeftPoint.gameObject);
        }

        public override void ResetAbilityUse()
        {
            base.ResetAbilityUse();
            m_previouslyHitColliders.Clear();
            m_hasUsedAbility = false;
        }

        public override async UniTask DoAbility()
        {
            base.DoAbility();
            

            switch (m_dashAbilityData.movementType)
            {
                case MovementType.TELEPORT:
                    DoInstantDash();
                    break;
                case MovementType.ALONG_PATH: case MovementType.JUMP:
                    await DoPathMovement();
                    break;
                case MovementType.REACTIVATE:
                    if (m_hasUsedAbility)
                    {
                        DoReactivateTeleport();
                    }
                    else
                    {
                        await SetupReactivateTeleport();
                    }
                    break;
            }
            
            m_aimingRightStick = false;
        }

        #endregion
    }
}