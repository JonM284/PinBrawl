using System;
using System.Collections.Generic;
using System.Linq;
using Data.StatusDatas;
using Project.Scripts.Utils;
using Runtime.Abilities;
using Runtime.Character;
using Runtime.GameControllers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utils;

namespace Runtime.UI.Items
{
    public class PlayerHealthDataModel: MonoBehaviour
    {

        #region Nested Classes

        [Serializable]
        public class AbilityVisuals
        {
            public Image background;
            public Image cooldownBar;
            public Image abilityIcon;
            public AbilityBase abilityRef;
            public bool isOnCooldown;
        }

        #endregion
        
        #region Serialized Fields
        
        [Header("General")]
        [SerializeField] private Color m_energyColorNormal, m_energyColorCharged;
        [SerializeField] private float m_burnSpeedMod = 1f;
        
        [Header("Player Follow Armor Bar")]
        [SerializeField] private RectTransform m_followBarVisuals;
        [SerializeField] private GameObject m_barVisuals, m_followSkullVisuals;
        [SerializeField] private Image m_currentArmorFollowImage, m_burnDamageFollowImage, m_energyFollowImage;
        [SerializeField] private TMP_Text m_armorAmountFollowText;
        [SerializeField] private List<GameObject> m_followHealthTicks = new List<GameObject>();
        [SerializeField] private List<Image> m_followStatusIcons = new List<Image>();

        [Header("Static Player Info Area")]
        [SerializeField] private Image m_playerInfoBackground, m_characterPortrait;
        [SerializeField] private RectTransform m_playerInfoAreaVisuals;
        [SerializeField] private GameObject m_staticBarVisuals, m_staticSkullVisuals;
        [SerializeField] private Image m_currentArmorStaticImage, m_burnDamageStaticImage, m_energyStaticImage;
        [SerializeField] private TMP_Text m_armorAmountStaticText;
        [SerializeField] private List<GameObject> m_staticHealthTicks = new List<GameObject>();
        [SerializeField] private AbilityVisuals m_wackAbility;
        [SerializeField] private List<AbilityVisuals> m_abilityHolders = new List<AbilityVisuals>();
        [SerializeField] private List<Image> m_staticStatusIcons = new List<Image>();

        
        #endregion
        
        #region Private Fields

        private BaseCharacter m_assignedCharacter;

        private float m_maxShield;

        private Camera m_cameraRef;

        private Transform m_healthBarFollow;
        
        private float m_HealthIncrements = 100f;

        private float m_maxEnergy, m_currentArmorPercentage, m_currentEnergyPercentage;

        private bool m_hasTakenDamage;

        private float m_previousShield;

        private int m_amountofTicks;

        private List<StatusData> m_appliedStatuses = new List<StatusData>();

        private List<AbilityVisuals> m_abilitiesOnCooldown = new List<AbilityVisuals>();
        private List<AbilityVisuals> m_removableAbilityCooldowns = new List<AbilityVisuals>();
        
        #endregion

        #region Accessors

        private Camera cameraRef => CommonUtils.GetRequiredComponent(ref m_cameraRef, CameraUtils.GetMainCamera);
        
        private Transform characterFollowTransform => CommonUtils.GetRequiredComponent(ref m_healthBarFollow, m_assignedCharacter.GetHealthBarTransform);
        
        #endregion

        #region Unity Events

        private void OnEnable()
        {
            BaseCharacter.OnArmorAmountChanged += UpdateHealthValue;
            BaseCharacter.OnMaxArmorChanged += UpdateMaxHealth;
            BaseCharacter.OnEnergyAmountChanged += UpdateShieldValue;
            BaseCharacter.OnPlayerDeath += OnPlayerDeath;
            BaseCharacter.OnPlayerRevived += OnPlayerRevived;
            BaseCharacter.OnStatusApplied += OnStatusApplied;
            BaseCharacter.OnStatusRemoved += OnStatusRemoved;
            BaseCharacter.OnAbilityUsed += OnAbilityUsed;
            BaseCharacter.OnWackUsed += OnWackUsed;
            BaseCharacter.OnAbilitiesAssigned += SetupAbilities;
        }

        private void OnDisable()
        {
            BaseCharacter.OnArmorAmountChanged -= UpdateHealthValue;
            BaseCharacter.OnMaxArmorChanged -= UpdateMaxHealth;
            BaseCharacter.OnEnergyAmountChanged -= UpdateShieldValue;
            BaseCharacter.OnPlayerDeath -= OnPlayerDeath;
            BaseCharacter.OnPlayerRevived -= OnPlayerRevived;
            BaseCharacter.OnStatusApplied -= OnStatusApplied;
            BaseCharacter.OnStatusRemoved -= OnStatusRemoved;
            BaseCharacter.OnAbilityUsed -= OnAbilityUsed;
            BaseCharacter.OnWackUsed -= OnWackUsed;
            BaseCharacter.OnAbilitiesAssigned -= SetupAbilities;
        }


        private void LateUpdate()
        {
            if (m_assignedCharacter.IsNull())
            {
                return;
            }

            if (characterFollowTransform.IsNull())
            {
                return;
            }

            Vector3 screenPos =
                cameraRef.WorldToScreenPoint(new Vector3(characterFollowTransform.position.x, characterFollowTransform.position.y, characterFollowTransform.position.z));

            m_followBarVisuals.position = screenPos;

            CheckWackCooldown();
            CheckAbilityCooldown();
            
            if (!m_hasTakenDamage)
            {
                return;
            }

            DoBurnHealthAnim();
        }

        #endregion
        
        #region Class Implementation

        public void Initialize(BaseCharacter _baseCharacter, float _maxShield, float _maxEnergy, Transform _staticHealthParent)
        {
            if (_baseCharacter.IsNull())
            {
                return;
            }

            m_assignedCharacter = _baseCharacter;
            
            //Follow Bar
            m_currentArmorFollowImage.fillAmount = 1f;
            m_burnDamageFollowImage.fillAmount = 1f;
            m_energyFollowImage.fillAmount = 0.25f;
            m_armorAmountFollowText.text = $"{_maxShield}";
            Vector3 screenPos =
                cameraRef.WorldToScreenPoint(new Vector3(characterFollowTransform.position.x, characterFollowTransform.position.y, characterFollowTransform.position.z));
            m_followBarVisuals.position = screenPos;
            
            //Static Area
            m_characterPortrait.sprite = m_assignedCharacter.characterData.characterIconRef;
            m_playerInfoAreaVisuals.parent = _staticHealthParent;
            m_currentArmorStaticImage.fillAmount = 1f;
            m_burnDamageStaticImage.fillAmount = 1f;
            m_energyStaticImage.fillAmount = 0.25f;
            m_armorAmountStaticText.text = $"{_maxShield}";
            
            //General
            m_previousShield = _maxShield;
            UpdateMaxHealth(m_assignedCharacter,_maxShield);
            m_maxEnergy = _maxEnergy;
            SetColor();
        }

        private void SetupAbilities()
        {
            if (m_assignedCharacter.IsNull())
            {
                return;
            }

            m_wackAbility.background.color = m_assignedCharacter.playerColor;
            m_wackAbility.cooldownBar.gameObject.SetActive(false);
            m_wackAbility.abilityIcon.sprite = SettingsController.Instance.GetWackIcon();

            for (int i = 0; i < m_assignedCharacter.AbilitiesAmount(); i++)
            {
                var _currentAbility = m_assignedCharacter.GetAbility(i);

                m_abilityHolders[i].abilityRef = _currentAbility;

                m_abilityHolders[i].background.color = m_assignedCharacter.playerColor;
                m_abilityHolders[i].cooldownBar.gameObject.SetActive(false);
                m_abilityHolders[i].abilityIcon.sprite = _currentAbility.abilityData.abilityIconRef;
            }
            
        }

        private void SetColor()
        {
            if (m_assignedCharacter.IsNull())
            {
                return;
            }

            m_currentArmorFollowImage.color = m_assignedCharacter.playerColor;
            m_currentArmorStaticImage.color = m_assignedCharacter.playerColor;

            m_playerInfoBackground.color = new Color(m_assignedCharacter.playerMidColor.r,
                m_assignedCharacter.playerMidColor.g, m_assignedCharacter.playerMidColor.b, m_playerInfoBackground.color.a);
        }
        
        private void ChangeTicksAmount()
        {
            m_amountofTicks = (int)Mathf.Floor(m_maxShield / m_HealthIncrements);

            for (int i = 0; i < m_followHealthTicks.Count; i++)
            {
                m_followHealthTicks[i].SetActive(i < m_amountofTicks);
                m_staticHealthTicks[i].SetActive(i < m_amountofTicks);
            }
        }

        private void OnWackUsed(BaseCharacter _baseCharacter)
        {
            if (_baseCharacter != m_assignedCharacter)
            {
                return;
            }

            m_wackAbility.isOnCooldown = true;
            m_wackAbility.cooldownBar.gameObject.SetActive(true);
            m_wackAbility.cooldownBar.fillAmount = 0;
        }
        
        private void OnAbilityUsed(BaseCharacter _baseCharacter, AbilityBase _abilityBase)
        {
            if (_baseCharacter != m_assignedCharacter)
            {
                return;
            }

            var _foundAbilityHolder = m_abilityHolders.FirstOrDefault(av => av.abilityRef == _abilityBase);

            if (_foundAbilityHolder.IsNull())
            {
                Debug.Log("Couldn't find ability");
                return;
            }

            _foundAbilityHolder.isOnCooldown = true;
            _foundAbilityHolder.cooldownBar.gameObject.SetActive(true);
            _foundAbilityHolder.cooldownBar.fillAmount = 0;
            m_abilitiesOnCooldown.Add(_foundAbilityHolder);
        }

        private void CheckWackCooldown()
        {
            if (!m_wackAbility.isOnCooldown)
            {
                return;
            }

            m_wackAbility.cooldownBar.fillAmount =
                m_assignedCharacter.wackTimer.currentTime / m_assignedCharacter.wackTimer.maxTime;

            if (m_wackAbility.cooldownBar.fillAmount <= 0.02f || m_assignedCharacter.canUseWack)
            {
                m_wackAbility.isOnCooldown = false;
                m_wackAbility.cooldownBar.gameObject.SetActive(false);
            }

        }

        private void CheckAbilityCooldown()
        {
            if (m_abilitiesOnCooldown.Count == 0)
            {
                return;
            }

            foreach (var _visualCooldown in m_abilitiesOnCooldown)
            {
                _visualCooldown.cooldownBar.fillAmount = _visualCooldown.abilityRef.abilityCooldownCurrent /
                                                         _visualCooldown.abilityRef.abilityCooldownMax;

                if (_visualCooldown.cooldownBar.fillAmount <= 0.02f || _visualCooldown.abilityRef.canUseAbility)
                {
                    m_removableAbilityCooldowns.Add(_visualCooldown);
                }
            }

            if (m_removableAbilityCooldowns.Count == 0)
            {
                return;
            }

            foreach (var _removableAbility in m_removableAbilityCooldowns)
            {
                _removableAbility.isOnCooldown = false;
                _removableAbility.cooldownBar.gameObject.SetActive(false);
                m_abilitiesOnCooldown.Remove(_removableAbility);
            }
            
            m_removableAbilityCooldowns.Clear();
        }

        private void OnStatusApplied(BaseCharacter _baseCharacter, StatusData _status)
        {
            if (_baseCharacter != m_assignedCharacter)
            {
                return;
            }
            
            m_appliedStatuses.Add(_status);
            UpdateStatusList();
        }

        private void OnStatusRemoved(BaseCharacter _baseCharacter, StatusData _status)
        {
            if (_baseCharacter != m_assignedCharacter)
            {
                return;
            }

            if (!m_appliedStatuses.Contains(_status))
            {
                return;
            }
            
            m_appliedStatuses.Remove(_status);
            UpdateStatusList();
        }

        private void UpdateStatusList()
        {
            for (int i = 0; i < m_followStatusIcons.Count; i++)
            {
                m_followStatusIcons[i].gameObject.SetActive(i < m_appliedStatuses.Count);

                if (i >= m_appliedStatuses.Count)
                {
                    continue;
                }

                m_followStatusIcons[i].sprite = m_appliedStatuses[i].statusIconRef;
            }
        }

        private void ClearAllStatuses()
        {
            m_appliedStatuses.Clear();
        }


        private void UpdateHealthValue(BaseCharacter _baseCharacter, float _currentArmor, BaseCharacter _attackingCharacter)
        {
            if (_baseCharacter != m_assignedCharacter)
            {
                return;
            }
            
            _currentArmor = Mathf.Max(Mathf.CeilToInt(_currentArmor), 0);

            m_currentArmorPercentage = _currentArmor / m_maxShield;
            
            m_currentArmorFollowImage.fillAmount = m_currentArmorPercentage;
            m_armorAmountFollowText.text = $"{_currentArmor}";

            m_currentArmorStaticImage.fillAmount = m_currentArmorPercentage;
            m_armorAmountStaticText.text = $"{_currentArmor}";

            if (m_previousShield > _currentArmor)
            {
                m_hasTakenDamage = true;   
            }
            else
            {
                m_burnDamageFollowImage.fillAmount = _currentArmor / m_maxShield;
                m_burnDamageStaticImage.fillAmount = _currentArmor / m_maxShield;
            }
            
            m_barVisuals.SetActive(m_currentArmorPercentage > 0);
            m_followSkullVisuals.SetActive(m_currentArmorPercentage <= 0);
            
            m_staticBarVisuals.SetActive(m_currentArmorPercentage > 0);
            m_staticSkullVisuals.SetActive(m_currentArmorPercentage <= 0);
            
            m_previousShield = _currentArmor;
        }

        private void UpdateShieldValue(BaseCharacter _baseCharacter, float _currentEnergy)
        {
            if (_baseCharacter != m_assignedCharacter)
            {
                return;
            }

            m_currentEnergyPercentage = _currentEnergy / m_maxEnergy;
            
            m_energyFollowImage.fillAmount = m_currentEnergyPercentage;
            m_energyStaticImage.fillAmount = m_currentEnergyPercentage;
            
            m_energyFollowImage.color =
                m_energyFollowImage.fillAmount == 1 ? m_energyColorCharged : m_energyColorNormal;
            m_energyStaticImage.color =
                m_energyStaticImage.fillAmount == 1 ? m_energyColorCharged : m_energyColorNormal;
            
        }

        private void DoBurnHealthAnim()
        {
            if (!m_hasTakenDamage)
            {
                return;
            }

            m_burnDamageFollowImage.fillAmount -= Time.deltaTime * m_burnSpeedMod;
            m_burnDamageStaticImage.fillAmount -= Time.deltaTime * m_burnSpeedMod;

            if (m_burnDamageFollowImage.fillAmount <= m_currentArmorFollowImage.fillAmount)
            {
                m_hasTakenDamage = false;
            }

        }
        
        private void OnPlayerDeath(BaseCharacter _killedCharacter, BaseCharacter _attacker, Vector3 _deathPosition)
        {
            if (_killedCharacter != m_assignedCharacter)
            {
                return;
            }
            
            m_followBarVisuals.gameObject.SetActive(false);
        }
        
        private void OnPlayerRevived(BaseCharacter _revivedPlayer)
        {
            if (_revivedPlayer != m_assignedCharacter)
            {
                return;
            }
            
            m_followBarVisuals.gameObject.SetActive(true);
        }

        public void UpdateMaxHealth(BaseCharacter _baseCharacter, float _newMaxHealth)
        {
            if (_baseCharacter.IsNull() || _baseCharacter != m_assignedCharacter)
            {
                return;
            }
            
            m_maxShield = _newMaxHealth;
            
            m_armorAmountStaticText.text = $"{m_maxShield}";
            m_armorAmountFollowText.text = $"{m_maxShield}";

            ChangeTicksAmount();
        }
        
        

        #endregion
        
        
        
        
        
    }
}