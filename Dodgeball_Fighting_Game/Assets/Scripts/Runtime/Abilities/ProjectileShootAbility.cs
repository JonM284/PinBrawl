using System.Threading;
using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Abilities
{
    public class ProjectileShootAbility: AbilityBase
    {

        #region Serialized Fields

        /// <summary>
        /// visuals feedback for player input aiming
        /// </summary>
        [SerializeField] private GameObject m_directionIndicator;

        /// <summary>
        /// 2D Image used for aiming
        /// </summary>
        [SerializeField] private Image m_directionIndicatorVisuals;
        
        /// <summary>
        /// projectile spawn position
        /// </summary>
        [SerializeField] private Transform m_shootPos;

        #endregion

        #region Private Fields

        /// <summary>
        /// amount of shots for this ability
        /// </summary>
        private int m_amountOfShots;
        
        /// <summary>
        /// calculated angle for spread shot
        /// </summary>
        private float m_culculatedAngle;

        /// <summary>
        /// calculated rotation for spread shot
        /// </summary>
        private Quaternion m_calculatedRotation;

        /// <summary>
        /// Name of projectile for object pool spawner
        /// </summary>
        private string projectileNameRef;

        /// <summary>
        /// Projectile prefab reference for object pool spawner
        /// </summary>
        private GameObject projectilePrefabRef;
        
        #endregion
        
        #region Class Implementation

        /// <summary>
        /// Projectile variables reference
        /// </summary>
        private ProjectileAbilityData projectileAbilityData => abilityData as ProjectileAbilityData;

        /// <summary>
        /// Set colors to player color
        /// </summary>
        private void ChangeLineRendererColor()
        {
            if (m_directionIndicator.IsNull())
            {
                return;
            }

            m_directionIndicatorVisuals.color = currentOwner.playerColor;
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
                
                var _projectile =  await GetProjectilePrefabAsync(token);
                
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
            
            var _projectile = await GetProjectilePrefabAsync(token);

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

        /// <summary>
        /// Initialize Ability variables
        /// </summary>
        public override async UniTask InitializeAbilityAsync(BaseCharacter _owner, AbilityData _data, bool _canUseOnStart,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await base.InitializeAbilityAsync(_owner, _data, true, token);
            
            speedAmountMax = projectileAbilityData.projectileShootSpeed;
            lifeTimeMax = projectileAbilityData.projectileMaxLifetime;
            m_amountOfShots = projectileAbilityData.projectileAmount;
            
            m_directionIndicator.transform.parent = _owner.GetIndicatorParent();
            
            ChangeLineRendererColor();
        }

        /// <summary>
        /// Return projectile from ObjectPoolSpawner
        /// </summary>
        private async UniTask<GameObject> GetProjectilePrefabAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            return await ObjectPoolController.Instance.T_CreateObject(projectileNameRef, 
                projectilePrefabRef, currentOwner.spawnerLocation.position, token);
        }
        
        /// <summary>
        /// Load necessary GameObjects for this ability before match starts.
        /// </summary>
        /// <param name="token"></param>
        public override async UniTask PreLoadNecessaryObjectsAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            for (int i = 0; i < projectileAbilityData.projectileAmount; i++)
            {
                projectileNameRef = projectileAbilityData.isGenericCharacterPrefab
                    ? string.Format(MatchGameController.Instance.defaultProjectilePoolNameFormat,
                        currentOwner.characterData.characterName, currentOwner.GetPlayerIndex(),
                        projectileAbilityData.abilityName)
                    : projectileAbilityData.name;
                
                projectilePrefabRef = projectileAbilityData.isGenericCharacterPrefab
                    ? SettingsController.Instance.GetGenericProjectileByPlayerIndex(currentOwner.GetPlayerIndex())
                    : projectileAbilityData.projeciltePrefab;
                
                await ObjectPoolController.Instance.T_PreCreateObject(projectileNameRef,
                    projectilePrefabRef, token);   
            }
        }

        /// <summary>
        /// Shoot projectile with current settings 
        /// </summary>
        protected override async UniTask DoAbilityAsync(CancellationToken token)
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
        
        /// <summary>
        /// Display or Hide attack indicator for player aiming
        /// </summary>
        protected override void ShowAttackIndicator(bool _isActive)
        {
            base.ShowAttackIndicator(_isActive);
            
            m_directionIndicator.gameObject.SetActive(_isActive);
        }

        #endregion
        
    }
}