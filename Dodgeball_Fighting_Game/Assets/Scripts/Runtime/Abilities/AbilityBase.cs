using System.Collections.Generic;
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

        private float cooldownModifier = 1f;
        //hit = damage and Knockback modifier
        private float hitModifier = 1f;
        private float lifeTimeModifier = 1f;
        private float speedModifier = 1f;
        private float rangeModifier = 1f;
        private float knockbackAmountMax;
        private float damageAmountMax;

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

        public float currentScale { get; set; }

        public AudioSource aSource => CommonUtils.GetRequiredComponent(ref m_audioSource,  GetComponent<AudioSource>);

        #endregion
        
        /// <summary>
        /// Initialize Ability WITH INJECTION -> reduces amount of necessary prefabs
        /// </summary>
        /// <param name="_owner">Owner Player</param>
        /// <param name="_data">Actual Data</param>
        public virtual void InitializeAbility(BaseCharacter _owner, AbilityData _data)
        {
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
            canUseAbility = true;
            SetCategoryGUIDs();
            PreLoadNecessaryObjects();
            ChangeMRColor();
        }

        private void ChangeMRColor()
        {
            if (m_changableMR.Count == 0)
            {
                return;
            }

            foreach (var _mr in m_changableMR)
            {
                if (_mr.IsNull())
                {
                    continue;
                }
                
                _mr.materials[0].SetColor("_Tint", currentOwner.playerColor);
                _mr.materials[0].SetColor("_Color", currentOwner.playerColor);
            }
        }

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
        
        public virtual async UniTask PreLoadNecessaryObjects()
        {
            
        }

        public virtual void ShowAttackIndicator(bool _isActive)
        {
            aimDirection = currentOwner.m_playerAimVector.normalized;
        }

        public virtual async UniTask DoAbility()
        {
            aimDirection = currentOwner.m_playerAimVector.normalized;
        }

        //Example: object[] arg = {Cooldown, Hit [damage and Knockback], Scale};
        //Projectile -> MaxLifetime, Speed
        //Dash -> Offset
        public virtual void UpdateAbility(params object[] _arguments)
        {
            cooldownModifier += (float)_arguments[0];
            hitModifier += (float)_arguments[1];
            currentScale += (float)_arguments[2];
            lifeTimeModifier += (float)_arguments[3];
            speedModifier += (float)_arguments[4];
            rangeModifier += (float)_arguments[5];
        }
        

        public virtual void ResetAbilityUse()
        {
            //Reset Variables
            canUseAbility = true;
            abilityCooldownCurrent = abilityCooldownMax;
            m_previouslyHitColliders.Clear();
        }

        public bool ContainsCategory(AbilityCategories _checkCategory)
        {
            return m_categoryGUIDs.Count != 0 && m_categoryGUIDs.Contains(_checkCategory.abilityCategoryGUID);
        }

        protected void PlayRandomSound()
        {
            if (m_abilityUseSFX.Count == 0)
            {
                return;
            }

            aSource.pitch = Random.Range(0.9f, 1.1f);
            aSource.PlayOneShot(m_abilityUseSFX[Random.Range(0, m_abilityUseSFX.Count)]);
        }
        
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