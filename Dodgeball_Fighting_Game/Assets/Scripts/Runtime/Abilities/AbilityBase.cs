using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using UnityEngine;

namespace Runtime.Abilities
{
    
    [RequireComponent(typeof(AudioSource))]
    public abstract class AbilityBase: MonoBehaviour, IAbility
    {

        #region Serialized Fields

        [SerializeField] protected GameObject m_abilityCastRangeIndicator;
        
        [SerializeField] protected LayerMask m_wallLayer;

        [SerializeField] protected List<AudioClip> m_abilityUseSFX = new List<AudioClip>();

        [SerializeField] protected List<MeshRenderer> m_changableMR = new List<MeshRenderer>();

        #endregion
        
        #region Protected Fields

        protected List<Collider> m_previouslyHitColliders = new List<Collider>();

        protected List<string> m_categoryGUIDs = new List<string>();
        
        protected float speedAmountMax;
        protected float rangeAmountMax;
        protected float lifeTimeMax;

        protected Vector3 m_endPosition;

        protected bool m_lastActiveState;

        protected AudioSource m_audioSource;
        
        protected RaycastHit[] m_hitWalls = new RaycastHit[2];
        protected int m_hitWallsAmount;
        
        #endregion

        #region Private Fields

        protected float cooldownModifier = 1f;
        //hit = damage and Knockback modifier
        protected float hitModifier = 1f;
        protected float lifeTimeModifier = 1f;
        protected float speedModifier = 1f;
        protected float rangeModifier = 1f;
        protected float knockbackAmountMax;
        protected float damageAmountMax;

        protected float chargeTimeCurrent, chargeTimeMax;

        protected float chargePercentage;

        protected CancellationTokenSource cts = new CancellationTokenSource();
        
        #endregion
        
        #region IAbility Inherited Methods

        public bool canUseAbility { get; set; }
        
        public float abilityCooldownCurrent { get; set; }
        public float abilityCooldownMax { get; set; }

        public Vector3 aimDirection { get; set; }
        public BaseCharacter currentOwner { get; set; }
        
        #region Accessors

        public AbilityData abilityData { get; private set; }
        
        //Cooldown, Knockback, Damage, Scale, lifeTime, speed, range
        public float currentKnockback => knockbackAmountMax * hitModifier;
        public float currentDamage => damageAmountMax * hitModifier;
        public float currentLifetime => lifeTimeMax * lifeTimeModifier;
        public float currentSpeed => speedAmountMax * speedModifier;
        public float currentRange => rangeAmountMax * rangeModifier;

        public float cooldownReductionModifier => cooldownModifier;

        public float knockbackDir => abilityData.IsNull() ? 1f : abilityData.isForwardKnockBack ? 1f : -1f;

        public float currentScale { get; set; }

        public bool isCharging { get; protected set;  }

        public AudioSource aSource => CommonUtils.GetRequiredComponent(ref m_audioSource,  GetComponent<AudioSource>);

        #endregion
        
        /// <summary>
        /// Initialize Ability WITH INJECTION -> reduces amount of necessary prefabs
        /// </summary>
        /// <param name="_owner">Owner Player</param>
        /// <param name="_data">Actual Data</param>
        public virtual async UniTask InitializeAbilityAsync(BaseCharacter _owner, AbilityData _data,
            bool _canUseOnStart, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_owner.IsNull())
            {
                return;
            }
            
            abilityData = _data;
            currentOwner = _owner;
            abilityCooldownMax = abilityData.abilityCooldownTimeMax;
            abilityCooldownCurrent = abilityCooldownMax;
            damageAmountMax = abilityData.abilityDamageAmount;
            knockbackAmountMax = abilityData.abilityKnockbackAmount;
            rangeAmountMax = abilityData.abilityRange;
            currentScale = abilityData.abilityScale;
            chargeTimeMax = abilityData.abilityActivationWaitTimeMax;
            canUseAbility = _canUseOnStart;
            
            SetCategoryGUIDs();
            await PreLoadNecessaryObjectsAsync(token);
            ChangeMrColor();
        }

        /// <summary>
        /// Percentage will change how effective this ability is, ie: more damage bigger range.
        /// ONLY if the ability is able to be charged. Otherwise the ability will always be max effective.
        /// </summary>
        protected void SetChargePercentage()
        {
            switch (abilityData.activationType)
            {
                case ActivationType.OnHold:
                case ActivationType.OnAutoCharge:
                case ActivationType.CountdownAfterPress:
                    chargePercentage = 0f;
                    break;
                default:
                    chargePercentage = 1f;
                    break;
            }
        }
        
        /// <summary>
        /// Change colors to the owner player.
        /// </summary>
        private void ChangeMrColor()
        {
            if (m_changableMR.Count == 0)
            {
                return;
            }

            foreach (var _mr in m_changableMR.Where(_mr => !_mr.IsNull()))
            {
                _mr.materials[0].SetColor("_Tint", currentOwner.playerColor);
                _mr.materials[0].SetColor("_Color", currentOwner.playerColor);
            }
        }

        /// <summary>
        /// Category GUIDs used for descriptions.
        /// </summary>
        public void SetCategoryGUIDs()
        {
            if (abilityData.abilityCategories.Count == 0)
            {
                return;
            }

            foreach (var _category in abilityData.abilityCategories)
            {
                m_categoryGUIDs.Add(_category.abilityCategoryGUID);
            }
        }
        
        /// <summary>
        /// Preload vfx and projectiles
        /// </summary>
        /// <param name="token"></param>
        public virtual async UniTask PreLoadNecessaryObjectsAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// When the ability button is pressed down
        /// </summary>
        public void OnAbilityButtonPressed()
        {
            switch (abilityData.activationType)
            {
                case ActivationType.OnRelease:
                    return;
                case ActivationType.OnAutoCharge when isCharging:
                    ReleaseAbilityCharge();
                    return;
                case ActivationType.OnAutoCharge when !isCharging:
                    ChargeAbilityAutoAsync(destroyCancellationToken).Forget();
                    break;
                case ActivationType.OnPress:
                    DoAbilityAsync(destroyCancellationToken).Forget();
                    break;
                case ActivationType.OnHold:
                    isCharging = true;
                    break;
            }
        }

        public void OnAbilityButtonHeld()
        {
            switch (abilityData.activationType)
            {
                case ActivationType.OnAutoCharge:
                case ActivationType.OnPress:
                    return;
                case ActivationType.OnRelease:
                    ShowAttackIndicator(true);
                    return;
                case ActivationType.OnHold:
                    HoldAbilityCharge();
                    break;
            }
        }

        public void OnAbilityButtonReleased()
        {
            switch (abilityData.activationType)
            {
                case ActivationType.OnAutoCharge:
                case ActivationType.OnPress:
                    return;
                case ActivationType.OnRelease:
                    DoAbilityAsync(destroyCancellationToken).Forget();
                    return;
                case ActivationType.OnHold:
                    ReleaseAbilityCharge();
                    DoAbilityAsync(destroyCancellationToken).Forget();
                    break;
            }
        }

        /// <summary>
        /// When the ability use button is pressed. (Charge activation type only)
        /// </summary>
        protected void HoldAbilityCharge()
        {
            if (!isCharging || abilityData.IsNull() || abilityData.activationType != ActivationType.OnHold)
            {
                return;
            }
            
            chargeTimeCurrent += Time.deltaTime;
            chargePercentage = Mathf.Clamp01(chargeTimeCurrent / chargeTimeMax);
            ShowAttackIndicator(isCharging);
        }
        
        /// <summary>
        /// When the ability use button is released. (Charge activation type only)
        /// </summary>
        protected void ReleaseAbilityCharge()
        {
            isCharging = false;
            ShowAttackIndicator(false);
        }

        /// <summary>
        /// Activate Ability charge.
        /// When fully charged or re-activated, the ability will perform it's action. (re-activate and countdown ability types)
        /// </summary>
        /// <param name="token"></param>
        protected async UniTask ChargeAbilityAutoAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            isCharging = true;
            chargeTimeCurrent = 0f;
            
            while (isCharging && chargeTimeCurrent < chargeTimeMax)
            {
                chargeTimeCurrent += Time.deltaTime;
                chargePercentage = Mathf.Clamp01(chargeTimeCurrent / chargeTimeMax);
                ShowAttackIndicator(isCharging);
                await UniTask.Yield(PlayerLoopTiming.LastUpdate, token);
                if (isCharging && !(chargeTimeCurrent >= chargeTimeMax)) continue;
                
                isCharging = false;
                chargeTimeCurrent = chargeTimeMax;
                break;
            }
            
            DoAbilityAsync(token).Forget();
        }
        
        /// <summary>
        /// Display Attack Indicator (usually used when attack is on release)
        /// </summary>
        protected virtual void ShowAttackIndicator(bool _isActive)
        {
            aimDirection = currentOwner.m_playerAimVector.normalized;
        }
        
        /// <summary>
        /// Perform actual ability
        /// </summary>
        /// <param name="token"></param>
        protected virtual async UniTask DoAbilityAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            aimDirection = currentOwner.m_playerAimVector.normalized;

            if (abilityData.isUltimateAbility)
            {
                //ToDo: add "Cinematic" Intro
                
            }
            
        }

        //Example: object[] arg = {Cooldown, Hit [damage and Knockback], Scale};
        //Projectile -> MaxLifetime, Speed
        //Dash -> Offset
        /// <summary>
        /// Change Ability Parameters during runtime
        /// </summary>
        public virtual void UpdateAbility(params object[] _arguments)
        {
            cooldownModifier += (float)_arguments[0];
            hitModifier += (float)_arguments[1];
            currentScale += (float)_arguments[2];
            lifeTimeModifier += (float)_arguments[3];
            speedModifier += (float)_arguments[4];
            rangeModifier += (float)_arguments[5];
        }
        

        /// <summary>
        /// Set ability to be ready to use again
        /// </summary>
        public virtual void ResetAbilityUse()
        {
            //Reset Variables
            canUseAbility = true;
            abilityCooldownCurrent = abilityCooldownMax;
            m_previouslyHitColliders.Clear();
        }

        /// <summary>
        /// Returns whether or not the category is for this ability.
        /// </summary>
        public bool ContainsCategory(AbilityCategories _checkCategory)
        {
            return m_categoryGUIDs.Count != 0 && m_categoryGUIDs.Contains(_checkCategory.abilityCategoryGUID);
        }

        /// <summary>
        /// SFX for this ability.
        /// </summary>
        protected void PlayRandomSound()
        {
            if (m_abilityUseSFX.Count == 0)
            {
                return;
            }

            aSource.pitch = Random.Range(0.9f, 1.1f);
            aSource.PlayOneShot(m_abilityUseSFX[Random.Range(0, m_abilityUseSFX.Count)]);
        }
        
        /// <summary>
        /// Sets the end position of this ability.
        /// Used for dashes and teleports so the player doesn't go out of bounds.
        /// </summary>
        protected virtual void GetEndPosition()
        {
            m_hitWallsAmount = Physics.RaycastNonAlloc(currentOwner.transform.position, aimDirection, 
                m_hitWalls, currentRange, m_wallLayer);
            
            m_endPosition = m_hitWallsAmount > 0 ? m_hitWalls[0].point : 
                currentOwner.transform.position + (aimDirection.normalized * currentRange);
        }

        #endregion
       
    }
}