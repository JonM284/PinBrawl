using System.Threading;
using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay;
using UnityEngine;

namespace Runtime.Abilities
{
    public class ProjectileShootAbility: AbilityBase
    {

        #region Serialized Fields

        [SerializeField] private LineRenderer m_rangeIndicator;
        
        [SerializeField] private Transform m_shootPos;

        #endregion

        #region Private Fields

        private int m_amountOfShots;
        
        private float m_culculatedAngle;

        private Quaternion m_calculatedRotation;
        
        #endregion
        
        #region Class Implementation

        private ProjectileAbilityData projectileAbilityData => abilityData as ProjectileAbilityData;


        private void ChangeLineRendererColor()
        {
            if (m_rangeIndicator.IsNull())
            {
                return;
            }

            m_rangeIndicator.startColor = currentOwner.playerColor;
            m_rangeIndicator.endColor = currentOwner.playerColor;
        }
        
        
        private async UniTask T_ShootMultipleProjectiles(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (projectileAbilityData.isSpreadShotOnStart)
            {
                await T_ShootSpreadProjectiles(token);
            }
            else
            {
                await T_ShootMultipleStraightProjectiles(token);
            }
        }

        private async UniTask T_ShootSpreadProjectiles(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            PlayRandomSound();
         
            float _angleIncrease = projectileAbilityData.projectileSpread / (m_amountOfShots - 1f);
            float _startAngle = -projectileAbilityData.projectileSpread/2;
            
            for (int i = 0; i < m_amountOfShots; i++)
            {
                // Calculate the angle for this projectile
                m_culculatedAngle = _startAngle + i * _angleIncrease;

                // Create a rotation for the current angle on the XZ plane
                m_calculatedRotation = Quaternion.Euler(0, m_culculatedAngle, 0);

                // Apply rotation to the main direction to get the spread direction
                Vector3 spreadDirection = m_calculatedRotation * aimDirection;
                
                var _projectile = await ObjectPoolController.Instance.T_CreateObject(projectileAbilityData.name, 
                    projectileAbilityData.projeciltePrefab, currentOwner.spawnerLocation.position, token);
                
                _projectile.TryGetComponent(out ProjectileEntityBase _projectileEntity);

                if (_projectileEntity.IsNull())
                {
                    return;
                }
            
                _projectileEntity.Initialize(currentOwner, spreadDirection, projectileAbilityData, 
                    currentSpeed, currentDamage, currentKnockback, currentLifetime, currentScale);
            }
        }
        
        private async UniTask T_ShootMultipleStraightProjectiles(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            for (int i = 0; i < m_amountOfShots; i++)
            {
                await ShootSingleStraightProjectile(token);
                PlayRandomSound();
                await UniTask.WaitForSeconds(projectileAbilityData.timeBetweenShots, cancellationToken: token);
            }
        }

        private async UniTask ShootSingleStraightProjectile(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            PlayRandomSound();
            
            var _projectile = await ObjectPoolController.Instance.T_CreateObject(projectileAbilityData.name, 
                projectileAbilityData.projeciltePrefab, currentOwner.spawnerLocation.position, token);

            _projectile.transform.forward = aimDirection;
            
            _projectile.TryGetComponent(out ProjectileEntityBase _projectileEntity);

            var _calculatedLifetime = CommonUtils.GetTimeFromDistanceAndSpeed(currentRange , currentSpeed);

            if (_projectileEntity.IsNull())
            {
                return;
            }
            
            _projectileEntity.Initialize(currentOwner, aimDirection, projectileAbilityData, 
                currentSpeed, currentDamage, currentKnockback, _calculatedLifetime, currentScale);
        }
        
        #endregion
       
        #region AbilityBase Inherited Methods

        public override async UniTask InitializeAbilityAsync(BaseCharacter _owner, AbilityData _data, bool _canUseOnStart,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await base.InitializeAbilityAsync(_owner, _data, true, token);
            
            speedAmountMax = projectileAbilityData.projectileShootSpeed;
            lifeTimeMax = projectileAbilityData.projectileMaxLifetime;
            m_amountOfShots = projectileAbilityData.projectileAmount;
            
            m_rangeIndicator.transform.parent = _owner.GetIndicatorParent();
            
            ChangeLineRendererColor();
        }
        
        public override async UniTask PreLoadNecessaryObjectsAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            for (int i = 0; i < projectileAbilityData.projectileAmount; i++)
            {
                await ObjectPoolController.Instance.T_PreCreateObject(projectileAbilityData.name,
                    projectileAbilityData.projeciltePrefab, token);   
            }
        }

        public override async UniTask DoAbilityAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await base.DoAbilityAsync(token);
            
            canUseAbility = false;
            
            //Shoot Projectile
            if (m_amountOfShots > 1)
            {   
                await T_ShootMultipleProjectiles(token);
            }
            else
            {
                await ShootSingleStraightProjectile(token);
            }
        }
        
        public override void ShowAttackIndicator(bool _isActive)
        {
            base.ShowAttackIndicator(_isActive);
            
            m_rangeIndicator.SetPosition(0, currentOwner.spawnerLocation.position + new Vector3(0,-0.001f, 0));
            m_rangeIndicator.SetPosition(1, currentOwner.spawnerLocation.position + (aimDirection.normalized * currentRange));

            m_rangeIndicator.startWidth = currentScale;
            m_rangeIndicator.endWidth = currentScale;
            
            m_rangeIndicator.gameObject.SetActive(_isActive);
        }

        #endregion
        
    }
}