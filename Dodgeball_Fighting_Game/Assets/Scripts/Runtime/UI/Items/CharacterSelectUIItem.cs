using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using Data.AbilityDatas;
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

        #region Enum

        public enum CharacterSelectionState
        {
            None,
            Disconnected,
            SelectingCharacter,
            SelectingAbility,
            Ready,
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

        [SerializeField] private GameObject m_secondAbilityHolder;
        [SerializeField] private TMP_Text m_secondAbilityDescription;
        [SerializeField] private Image m_secondAbilityIcon;
        
        [SerializeField] private PlayerUIInputReader inputReader;
        
        #endregion

        #region Private Fields


        private bool m_canMoveSelection, m_isInitialized;

        private int m_characterIndexCurrent, m_characterMaxAmount;

        private float m_horizontalInput;

        private float m_threshold = 0.2f;

        private CharacterSelectDataModel manager;

        private Color m_playerColor;

        #endregion

        #region Accessors

        public CharacterData currentCharacter { get; private set; }

        public AbilityData currentAbilityData { get; private set; }

        public CharacterSelectionState currentSelectionState { get; private set; }

        public Player assignedPlayer { get; private set; }

        public Color assignedColor => m_playerColor;

        public int playerIndex { get; private set; }

        public PlayerUIInputReader InputReader => inputReader;

        public bool isReady => currentSelectionState == CharacterSelectionState.Ready;

        #endregion
        
        #region Class Implementation

        public void Initialize(int _playerIndex, CharacterSelectDataModel _characterSelectDataModel)
        {
            playerIndex = _playerIndex;
            assignedPlayer = ReInput.players.GetPlayer(playerIndex);
            m_characterMaxAmount = MatchGameController.Instance.GetCharacterAmount();
            m_playerColor = SettingsController.Instance.GetColorByPlayerIndex(playerIndex);
            m_background.color = m_playerColor;
            currentSelectionState = CharacterSelectionState.Disconnected;
            
            inputReader.InitializeItem(assignedPlayer, null, OnSelectCallback, OnCancelCallback, 
                OnHorizontalChangeCallback, OnVerticalChangeCallback);

            CheckController(assignedPlayer);
            
            manager = _characterSelectDataModel;
            
            DisconnectPlayer();
            m_isInitialized = true;
        }

        private void OnSelectCallback(Player controller)
        {
            manager.OnSelectPressed(controller);
        }

        private void OnCancelCallback(Player controller)
        {
            manager.OnCancelPressed(controller);
        }

        private void OnHorizontalChangeCallback(Player controller, bool isRight)
        {
            manager.OnHorizontalUpdated(controller, isRight);
        }

        private void OnVerticalChangeCallback(Player controller, bool isUp)
        {
            manager.OnVerticalUpdated(controller, isUp);
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
        
        private void UpdateReadyVisuals()
        {
            m_readyVisuals.SetActive(currentSelectionState == CharacterSelectionState.Ready);
        }

        public void DisconnectPlayer()
        {
            m_characterVisuals.SetActive(false);
            m_selectingVisual.SetActive(false);
            m_readyVisuals.SetActive(false);
            m_joinVisual.SetActive(true);
            currentSelectionState = CharacterSelectionState.Disconnected;
        }

        /// <summary>
        /// Disconnected -> Character Selection -> Ability Selection -> Ready
        /// </summary>

        public void OnSelect_Disconnected(CharacterData characterData)
        {
            m_characterVisuals.SetActive(true);
            m_selectingVisual.SetActive(true);
            m_readyVisuals.SetActive(false);
            m_joinVisual.SetActive(false);
            currentCharacter = characterData;
            currentAbilityData = null;
            m_characterIndexCurrent = 0;
            UpdateCharacterInfo();
            currentSelectionState = CharacterSelectionState.SelectingCharacter;
        }
        
        public void OnSelect_Character()
        {
            currentSelectionState = CharacterSelectionState.SelectingAbility;
        }
        
        public void OnSelect_Ability()
        {
            currentSelectionState = CharacterSelectionState.Ready;
            UpdateReadyVisuals();
            manager.UpdateReadyPlayers();
        }
        
        public void OnCancel_CharacterSelect()
        {
            currentSelectionState = CharacterSelectionState.Disconnected;
            DisconnectPlayer();
        }
        
        public void OnCancel_AbilitySelect()
        {
            currentSelectionState = CharacterSelectionState.SelectingCharacter;
            currentAbilityData = null;
            UpdateAbilitySelectVisuals(false);
        }

        public void OnCancel_Ready()
        {
            currentSelectionState = CharacterSelectionState.SelectingAbility;
        }

        public void ChangeSelectedCharacter(CharacterData characterData)
        {
            if (characterData.IsNull())
            {
                return;
            }

            currentCharacter = characterData;
            
            UpdateCharacterInfo();
            
            m_canMoveSelection = false;
        }

        private void UpdateCharacterInfo()
        {
            if (currentCharacter.IsNull())
            {
                return;
            }
            
            m_characterIcon.sprite = currentCharacter.characterIconRef;
            m_characterName.text = currentCharacter.characterName;
            m_shieldAmountText.text = currentCharacter.characterArmorAmount.ToString();
            
            //First ability is ultimate
            m_firstAbilityDescription.text = currentCharacter.allCharacterAbilities[0].abilityDescription;
            m_firstAbilityIcon.sprite = currentCharacter.allCharacterAbilities[0].abilityIconRef;
        }

        public void ChangeSelectedAbility(AbilityData abilityData)
        {
            if (abilityData.IsNull())
            {
                return;
            }

            currentAbilityData = abilityData;
            UpdateAbilitySelectVisuals(true);
        }

        private void UpdateAbilitySelectVisuals(bool isShow)
        {
            m_secondAbilityHolder.SetActive(isShow);
            
            if (currentAbilityData.IsNull())
            {
                m_secondAbilityDescription.text = string.Empty;
                m_secondAbilityIcon.sprite = null;
                return;
            }
            
            m_secondAbilityDescription.text = currentAbilityData.abilityDescription;
            m_secondAbilityIcon.sprite = currentAbilityData.abilityIconRef;
        }
        
        
        #endregion
        
        
    }
}