using System;
using System.Collections.Generic;
using Data;
using Project.Scripts.Utils;
using Rewired;
using Rewired.ControllerExtensions;
using Runtime.GameControllers;
using Runtime.UI.DataModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Items
{
    public class CharacterSelectUIItem: MonoBehaviour
    {

        #region Read-Only

        private readonly string confirmButton = "Select";
        private readonly string cancelButton = "Cancel";
        private readonly string startAction = "Start_Action";

        #endregion

        #region Nested Classes

        [Serializable]
        public class AbilityTypeVisuals
        {
            public GameObject tagBackground;
            public TMP_Text typeText;
        }

        #endregion

        #region Serialized Fields
        
        [SerializeField] private Image m_characterIcon;
        [SerializeField] private Image m_background;

        [SerializeField] private GameObject m_joinVisual;
        [SerializeField] private GameObject m_selectingVisual;
        [SerializeField] private GameObject m_characterVisuals;
        [SerializeField] private GameObject m_readyVisuals;

        [SerializeField] private TMP_Text m_characterName;

        [SerializeField] private TMP_Text m_shieldAmountText;

        [SerializeField] private TMP_Text m_firstAbilityDescription;
        [SerializeField] private Image m_firstAbilityIcon;
        [SerializeField] private List<AbilityTypeVisuals> m_firstAbilityTypeTags = new List<AbilityTypeVisuals>();
            
        [SerializeField] private TMP_Text m_secondAbilityDescription;
        [SerializeField] private Image m_secondAbilityIcon;
        [SerializeField] private List<AbilityTypeVisuals> m_secondAbilityTypeTags = new List<AbilityTypeVisuals>();
        
        #endregion

        #region Private Fields


        private bool m_isActive, m_canMoveSelection, m_isInitialized;

        private int m_characterIndexCurrent, m_characterMaxAmount;

        private float m_horizontalInput;

        private float m_threshold = 0.2f;

        private CharacterSelectDataModel m_manager;

        private Color m_playerColor;

        #endregion

        #region Accessors

        public CharacterData currentCharacter { get; private set; }

        public bool isReady { get; private set; }

        public Player assignedPlayer { get; private set; }

        #endregion

        #region Unity Events

        private void Update()
        {
            if (!m_isInitialized)
            {
                return;
            }

            ReadSelectionInputs();
            ReadJoinInputs();
            ReadOtherInputs();
        }

        #endregion
        
        #region Class Implementation

        public void Initialize(int _playerIndex, CharacterSelectDataModel _characterSelectDataModel)
        {
            assignedPlayer = ReInput.players.GetPlayer(_playerIndex);
            m_characterMaxAmount = MatchGameController.Instance.GetCharacterAmount();
            m_playerColor = SettingsController.Instance.GetColorByPlayerIndex(_playerIndex);
            m_background.color = m_playerColor;

            CheckController(assignedPlayer);
            
            m_manager = _characterSelectDataModel;
            
            DisconnectPlayer();
            m_isInitialized = true;
        }
        
        void CheckController(Player player)
        {
            foreach (Joystick joyStick in player.controllers.Joysticks)
            {
                var ds4 = joyStick.GetExtension<DualShock4Extension>();
                if (ds4.IsNull()){
                    //skip this if not DualShock4
                    continue;
                }

                ds4.SetLightColor(m_playerColor);
            }
        }

        public void DisconnectPlayer()
        {
            m_characterVisuals.SetActive(false);
            m_selectingVisual.SetActive(false);
            m_readyVisuals.SetActive(false);
            m_joinVisual.SetActive(true);

            isReady = false;
            m_isActive = false;
        }

        private void ReadJoinInputs()
        {
            if (m_isActive)
            {
                return;
            }
            
            if (assignedPlayer.GetButtonDown(confirmButton))
            {
                PlayerJoin();
            }
        }

        private void ReadOtherInputs()
        {
            if (assignedPlayer.GetButtonDown(cancelButton))
            {
                if (isReady)
                {
                    CancelSelection();
                }
                else
                {
                    DisconnectPlayer();
                }
            }

            if (assignedPlayer.GetButtonDown(startAction) && isReady)
            {
                m_manager.StartGame();
            }
        }
        
        private void ReadSelectionInputs()
        {
            if (!m_isActive)
            {
                return;
            }
            
            if (isReady)
            {
                return;
            }
            
            m_horizontalInput = assignedPlayer.GetAxisRaw("Move_Horizontal");

            if (m_horizontalInput >= m_threshold && m_canMoveSelection)
            {
                ChangeSelection(true);
            }else if (m_horizontalInput <= -m_threshold && m_canMoveSelection)
            {
                ChangeSelection(false);
            }else if (Mathf.Abs(m_horizontalInput) < m_threshold && !m_canMoveSelection)
            {
                m_canMoveSelection = true;
            }

            if (assignedPlayer.GetButtonDown(confirmButton))
            {
                SelectCharacter();
            }
        }

        private void CancelSelection()
        {
            isReady = false;
            UpdateReadyVisuals();
            m_manager.UpdateReadyPlayers();
        }

        private void SelectCharacter()
        {
            isReady = true;
            UpdateReadyVisuals();
            m_manager.UpdateReadyPlayers();
        }

        private void UpdateReadyVisuals()
        {
            m_readyVisuals.SetActive(isReady);
        }

        private void PlayerJoin()
        {
            m_characterVisuals.SetActive(true);
            m_selectingVisual.SetActive(true);
            m_readyVisuals.SetActive(false);
            m_joinVisual.SetActive(false);

            m_characterIndexCurrent = 0;
            currentCharacter = MatchGameController.Instance.GetCharacterDataAtIndex(m_characterIndexCurrent);
            UpdateCharacterInfo();
            
            m_isActive = true;
        }

        private void ChangeSelection(bool _isRight)
        {
            int _nextIndex = _isRight ? m_characterIndexCurrent + 1 : m_characterIndexCurrent - 1;

            if (_nextIndex > m_characterMaxAmount - 1)
            {
                _nextIndex = 0;
            }else if (_nextIndex < 0)
            {
                _nextIndex = m_characterMaxAmount - 1;
            }

            m_characterIndexCurrent = _nextIndex;

            currentCharacter = MatchGameController.Instance.GetCharacterDataAtIndex(m_characterIndexCurrent);

            UpdateCharacterInfo();
            
            m_canMoveSelection = false;
        }

        private void UpdateCharacterInfo()
        {
            if (currentCharacter.IsNull())
            {
                return;
            }
            
            m_firstAbilityTypeTags.ForEach(atv => atv.tagBackground.SetActive(false));
            m_secondAbilityTypeTags.ForEach(atv => atv.tagBackground.SetActive(false));
            
            m_characterIcon.sprite = currentCharacter.characterIconRef;
            m_characterName.text = currentCharacter.characterName;
            m_shieldAmountText.text = currentCharacter.characterArmorAmount.ToString();
            
            m_firstAbilityDescription.text = currentCharacter.allCharacterAbilities[0].abilityDescription;
            m_firstAbilityIcon.sprite = currentCharacter.allCharacterAbilities[0].abilityIconRef;
            for (int i = 0; i < m_firstAbilityTypeTags.Count; i++)
            {
                m_firstAbilityTypeTags[i].tagBackground.SetActive(i < currentCharacter.allCharacterAbilities[0].abilityCategories.Count);
                
                if (i >= currentCharacter.allCharacterAbilities[0].abilityCategories.Count)
                {
                    continue;
                }
                
                m_firstAbilityTypeTags[i].typeText.text = currentCharacter.allCharacterAbilities[0].abilityCategories[i].abilityCategoryName;
            }
            
            m_secondAbilityDescription.text = currentCharacter.allCharacterAbilities[1].abilityDescription;
            m_secondAbilityIcon.sprite = currentCharacter.allCharacterAbilities[1].abilityIconRef;
            
            for (int i = 0; i < m_secondAbilityTypeTags.Count; i++)
            {
                m_secondAbilityTypeTags[i].tagBackground.SetActive(i < currentCharacter.allCharacterAbilities[1].abilityCategories.Count);
                
                if (i >= currentCharacter.allCharacterAbilities[1].abilityCategories.Count)
                {
                    continue;
                }
                
                m_secondAbilityTypeTags[i].typeText.text = currentCharacter.allCharacterAbilities[1].abilityCategories[i].abilityCategoryName;
            }
        }

        #endregion
        
        
    }
}