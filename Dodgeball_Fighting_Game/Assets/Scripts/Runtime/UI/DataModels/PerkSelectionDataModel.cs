using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Data.PerkDatas;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Character;
using Runtime.GameControllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Items
{
    public class PerkSelectionDataModel: MonoBehaviour
    {

        #region Read-ONLY

        private readonly string confirmActionName = "Select";
        private readonly string cancelActionName = "Cancel";

        #endregion
        
        #region Nested Classes

        [Serializable]
        public class AbilityHolder
        {
            public Image m_abilityImage;
            public List<AbilityTypeHolder> m_abilityTypes = new List<AbilityTypeHolder>();
        }
        
        [Serializable]
        public class AbilityTypeHolder
        {
            public GameObject tagBackground;
            public TMP_Text typeText;
        }

        #endregion
        
        #region Serialized Fields

        [SerializeField] private Image m_currentSelectingCharacterImage;

        [SerializeField] private Image m_currentSelectingPlayerBackgroundImage;

        [SerializeField] private TMP_Text m_selectingCharacterName;

        [SerializeField] private List<AbilityHolder> m_abilityHolders = new List<AbilityHolder>();

        [SerializeField] private List<PerkChipsUIItem> m_perkUISlots = new List<PerkChipsUIItem>();

        [SerializeField] private float m_axisThreshold;

        [SerializeField] private int m_amountOfPerks;

        [SerializeField] private float m_controllerVibrationAmount = 0.5f;
        [SerializeField] private float m_controllerVibrationDuration = 0.15f;
        
        #endregion

        #region Private Fields
        
        private PerkChipsUIItem m_currentHightlightPerk, m_previouslyHighlightedPerk;

        private bool m_canSwitch = true;

        private float m_inputMoveDir;

        private int m_currentHoveredPerkIndex, m_nextIndex, m_previousIndex;

        private List<PerkDataBase> m_randomPerks = new List<PerkDataBase>();
        
        #endregion
        
        #region Accessors

        public bool isSelecting { get; private set; }

        public Player currentPlayerController { get; private set;}

        public BaseCharacter currentPlayer { get; private set; }

        #endregion

        #region Unity Events

        private void Update()
        {
            if (!isSelecting || currentPlayerController.IsNull())
            {
                return;
            }

            ReadPlayerInputs();
        }

        #endregion

        #region Class Implementation

        public void SetActiveState(bool _isActive)
        {
            isSelecting = _isActive;
        }

        public async UniTask SetupSelectionScreenPlayer(BaseCharacter _baseCharacter)
        {
            if (_baseCharacter.IsNull())
            {
                Debug.Log("Base Character Null");
                return;
            }
            
            m_canSwitch = true;
            
            currentPlayer = _baseCharacter;
            
            m_randomPerks = MatchGameController.Instance.GetRandomPerkSet(m_amountOfPerks).ToList();
            
            Debug.Log($"Random Perks Amount = {m_randomPerks.Count}");

            m_currentSelectingCharacterImage.sprite = currentPlayer.characterData.characterIconRef;
            m_currentSelectingPlayerBackgroundImage.color = currentPlayer.playerColor;
            m_selectingCharacterName.text = currentPlayer.characterData.characterName;

            for (int x = 0; x < currentPlayer.characterData.allCharacterAbilities.Count; x++)
            {
                if (x >= 2)
                {
                    continue;
                }

                m_abilityHolders[x].m_abilityImage.sprite =
                    currentPlayer.characterData.allCharacterAbilities[x].abilityIconRef;

                for (int y = 0; y < m_abilityHolders[x].m_abilityTypes.Count; y++)
                {
                    m_abilityHolders[x].m_abilityTypes[y].tagBackground.SetActive(y < currentPlayer.characterData.allCharacterAbilities[x].abilityCategories.Count);
                
                    if (y >= currentPlayer.characterData.allCharacterAbilities[x].abilityCategories.Count)
                    {
                        continue;
                    }
                
                    m_abilityHolders[x].m_abilityTypes[y].typeText.text = 
                        currentPlayer.characterData.allCharacterAbilities[x].abilityCategories[y].abilityCategoryName;
                }
                
            }
            
            m_currentHoveredPerkIndex = 0;

            currentPlayerController = currentPlayer.GetPlayerController();
            currentPlayerController.SetVibration(0,m_controllerVibrationAmount, m_controllerVibrationDuration);

            for (int i = 0; i < m_perkUISlots.Count; i++)
            {
                m_perkUISlots[i].gameObject.SetActive(i < m_amountOfPerks);
            }

            for (int i = 0; i < m_randomPerks.Count; i++)
            {
                m_perkUISlots[i].gameObject.SetActive(!m_randomPerks[i].IsNull());

                if (m_randomPerks[i].IsNull())
                {
                    continue;
                }
                
                await m_perkUISlots[i].SetupPerk(m_randomPerks[i]);
                m_perkUISlots[i].SetActiveHighlight(i == m_currentHoveredPerkIndex);
            }


            m_currentHightlightPerk = m_perkUISlots[m_currentHoveredPerkIndex];
        }


        private void ReadPlayerInputs()
        {
            m_inputMoveDir = currentPlayerController.GetAxisRaw("Move_Horizontal");
            
            if (m_canSwitch)
            {
                if (m_inputMoveDir > m_axisThreshold)
                {
                    ChangeCurrentHovered(true);
                }else if (m_inputMoveDir < -m_axisThreshold)
                {
                    ChangeCurrentHovered(false);
                }
            }
            else
            {
                if (Mathf.Abs(m_inputMoveDir) <= m_axisThreshold)
                {
                    m_canSwitch = true;
                }
            }
            
            if (currentPlayerController.GetButtonDown(confirmActionName))
            {
                SelectPerk();
            }
        }

        private void ChangeCurrentHovered(bool _isRight)
        {
            m_canSwitch = false;

            m_previousIndex = m_currentHoveredPerkIndex;

            m_nextIndex = _isRight ? m_currentHoveredPerkIndex + 1 : m_currentHoveredPerkIndex - 1;

            if (m_nextIndex > m_perkUISlots.Count - 1)
            {
                m_nextIndex = 0;
            }else if (m_nextIndex < 0)
            {
                m_nextIndex = m_perkUISlots.Count - 1;
            }

            m_nextIndex = Mathf.Clamp(m_nextIndex, 0, m_perkUISlots.Count - 1);
            
            m_currentHoveredPerkIndex = m_nextIndex;

            HighlightChipAtIndex(m_currentHoveredPerkIndex);
        }

        private void HighlightChipAtIndex(int _index)
        {
            if (_index > m_perkUISlots.Count - 1 || _index < 0)
            {
                return;
            }

            m_currentHightlightPerk = m_perkUISlots[_index];

            for (int i = 0; i < m_perkUISlots.Count; i++)
            {
                if (i != m_currentHoveredPerkIndex && i != m_previousIndex)
                {
                    continue;
                }
                
                m_perkUISlots[i].SetActiveHighlight(i == m_currentHoveredPerkIndex);
            }
        }

        private void SelectPerk()
        {
            if (currentPlayer.IsNull())
            {
                return;
            }
            
            //ToDo: Selected Animation
            
            //Add to character
            currentPlayer.AddPerk(m_currentHightlightPerk.assignedPerk);
            
            //Remove from available perks
            MatchGameController.Instance.SetPerkSelected(m_currentHightlightPerk.assignedPerk);

            //Next player can choose => OR END
            SetActiveState(false);
        }
        
        #endregion
        
        
    }
}