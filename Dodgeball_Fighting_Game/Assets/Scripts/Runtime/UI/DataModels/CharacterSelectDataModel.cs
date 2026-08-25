using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data;
using Data.AbilityDatas;
using NUnit.Framework;
using Project.Scripts.Utils;
using Rewired;
using Runtime.GameControllers;
using Runtime.UI.Items;
using UnityEngine;

namespace Runtime.UI.DataModels
{
    public class CharacterSelectDataModel: MonoBehaviour
    {

        #region Read-ONLY

        private readonly string confirmActionName = "Select";
        private readonly string cancelActionName = "Cancel";

        #endregion
        
        #region Nested Classes

        private class CharacterSelectionPlayerPositionInfo
        {
            public int xPos = 0, yPos = 0;
            public int currentAbilityIndex = 0;
            public CharacterSelectionSlotUIItem currentItem;
            public bool hasFinishedSelecting = false;
            public CharacterSelectUIItem assignedSelectionUIItem;

            public CharacterSelectionPlayerPositionInfo(CharacterSelectUIItem selectUIItem)
            {
                assignedSelectionUIItem = selectUIItem;
                hasFinishedSelecting = false;
            }

            public void AddHoverToItem()
            {
                if (currentItem.IsNull())
                {
                    return;
                }
                
                currentItem.AddHoveringPlayer(assignedSelectionUIItem);
            }

            public void RemoveFromItem()
            {
                if (currentItem.IsNull())
                {
                    return;
                }
                
                currentItem.RemoveHoveringPlayer(assignedSelectionUIItem);
            }
        }
        
        #endregion
        
        #region Serialized Fields

        [SerializeField] private List<CharacterSelectUIItem> m_uiItems = new List<CharacterSelectUIItem>();
        
        [SerializeField] private List<CharacterSelectionSlotUIItem> characterSlotUiItems = new();

        [SerializeField] private int maxXIndex = 3;
        
        #endregion

        #region Private Fields

        private bool m_readyToPlay, m_loadingGame;

        //key = row index, value = character -> column index
        private Dictionary<int, List<CharacterSelectionSlotUIItem>> activeCharacterSlotsByRow = new();
        
        private Dictionary<Player, CharacterSelectionPlayerPositionInfo> posInfoByController = new();

        private List<AbilityData> allAssignableAbilities = new List<AbilityData>();

        private CancellationTokenSource cts;
        
        #endregion

        #region Accessors

        private int maxAmountOfRows => Mathf.CeilToInt(activeCharacterSlotsByRow.Keys.Count / (float)maxXIndex);

        #endregion
        
        #region Unity Events

        private void OnEnable()
        {
            ReInput.ControllerConnectedEvent += ReInputOnControllerConnectedEvent;
            ReInput.ControllerDisconnectedEvent += ReInputOnControllerDisconnectedEvent;
        }

        private void OnDisable()
        {
            ReInput.ControllerConnectedEvent -= ReInputOnControllerConnectedEvent;
            ReInput.ControllerDisconnectedEvent -= ReInputOnControllerDisconnectedEvent;
            cts.Cancel();
        }

        private void Start()
        {
            SetupScreen().Forget();
        }
        
        #endregion

        #region Class Implementation

        private async UniTask SetupScreen()
        {
            posInfoByController.Clear();

            if (!MainController.Instance.allInitialized)
            {
                await UniTask.WaitUntil(() => MainController.Instance.allInitialized);
            }
            
            cts = new CancellationTokenSource();

            var allCharacters = MatchGameController.Instance.GetAllCharacterData();

            allAssignableAbilities = MatchGameController.Instance.GetAllAbilities();

            var currentCharacterIndex = 0;
            var maxRows = Mathf.CeilToInt(allCharacters.Count / (float)maxXIndex);

            for (int y = 0; y < maxRows; y++)
            {
                var _characterSlotUIList = new List<CharacterSelectionSlotUIItem>();
                activeCharacterSlotsByRow.Add(y, _characterSlotUIList);

                for (int x = 0; x < maxXIndex; x++)
                {
                    characterSlotUiItems[currentCharacterIndex].gameObject.SetActive(currentCharacterIndex < allCharacters.Count);
                    if (currentCharacterIndex >= allCharacters.Count)
                    {
                        continue;
                    }
                    
                    await characterSlotUiItems[currentCharacterIndex]
                        .InitializeItem(allCharacters[currentCharacterIndex], x, y);
                    _characterSlotUIList.Add(characterSlotUiItems[currentCharacterIndex]);
                    
                    currentCharacterIndex++;
                }
            }
            
            for (int i = 0; i < 4; i++)
            {
                m_uiItems[i].Initialize(i, this);
                var newPosInfo = new CharacterSelectionPlayerPositionInfo(m_uiItems[i]);
                newPosInfo.currentItem = GetUIAtCoordinate(newPosInfo.xPos, newPosInfo.yPos);
                posInfoByController.Add(m_uiItems[i].assignedPlayer, newPosInfo);
            }

            ReadPlayerInputsAsync(cts.Token).Forget();
        }
        
        //ToDo: make game start automatically
        public async UniTask ReadPlayerInputsAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            while (!m_loadingGame)
            {
                if (m_loadingGame)
                {
                    break;
                }

                m_uiItems.ForEach(ui => ui.InputReader.ReadInputs());
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }
        }

        public void UpdateReadyPlayers()
        {
            m_readyToPlay = m_uiItems.Count(csi => csi.currentSelectionState == CharacterSelectUIItem.CharacterSelectionState.Ready) >= 2;
        }
        
        private CharacterSelectionSlotUIItem GetUIAtCoordinate(int xIndex, int yIndex)
        {
            if ((xIndex >= maxXIndex || xIndex < 0)
                || (yIndex >= activeCharacterSlotsByRow.Count || yIndex < 0))
            {
                return activeCharacterSlotsByRow.FirstOrDefault().Value.FirstOrDefault();
            }
            
            return activeCharacterSlotsByRow[yIndex][xIndex];
        }

        private int ReturnLowest(int index, int size)
        {
            return Mathf.Min(index, size);
        }
        
        private int WrapIndex(int index, int size)
        {
            return ((index % size) + size) % size;
        }

        public void OnSelectPressed(Player controller)
        {
            if (controller.IsNull() || !posInfoByController.ContainsKey(controller))
            {
                return;
            }
            
            var foundPlayerPositionInfo = posInfoByController[controller];
            
            if (foundPlayerPositionInfo.currentItem.IsNull() || foundPlayerPositionInfo.assignedSelectionUIItem.IsNull())
            {
                Debug.LogError($"[MultiPerkSelection] CurrentItem null?: {foundPlayerPositionInfo.currentItem.IsNull()}" +
                               $" SelectionUIItem NULL?: {foundPlayerPositionInfo.assignedSelectionUIItem.IsNull()}");
                return;
            }

            switch (foundPlayerPositionInfo.assignedSelectionUIItem.currentSelectionState)
            {
                case CharacterSelectUIItem.CharacterSelectionState.Disconnected:
                    OnSelectFromDisconnected(foundPlayerPositionInfo);
                    break;
                case CharacterSelectUIItem.CharacterSelectionState.SelectingCharacter:
                    OnSelectCharacter(foundPlayerPositionInfo);
                    break;
                case CharacterSelectUIItem.CharacterSelectionState.SelectingAbility:
                    OnSelectedAbility(foundPlayerPositionInfo);
                    break;
                case CharacterSelectUIItem.CharacterSelectionState.Ready:
                    TryStartGame();
                    break;
            }
        }

        private void OnSelectFromDisconnected(CharacterSelectionPlayerPositionInfo playerPositionInfo)
        {
            //Activate marker
            playerPositionInfo.AddHoverToItem();
            //Assign first character to UI
            playerPositionInfo.assignedSelectionUIItem.OnSelect_Disconnected(playerPositionInfo.currentItem.assignedCharacterData);
        }

        private void OnSelectCharacter(CharacterSelectionPlayerPositionInfo playerPositionInfo)
        {
            playerPositionInfo.assignedSelectionUIItem.OnSelect_Character();
            //Note: may need to remove this if the character slot is suppose to continue as selected
            playerPositionInfo.RemoveFromItem();
        }

        private void OnSelectedAbility(CharacterSelectionPlayerPositionInfo playerPositionInfo)
        {
            playerPositionInfo.assignedSelectionUIItem.OnSelect_Ability();
            playerPositionInfo.hasFinishedSelecting = true;
        }
        
        public void OnCancelPressed(Player controller)
        {
            if (controller.IsNull() || !posInfoByController.ContainsKey(controller))
            {
                return;
            }

            var foundPlayerPositionInfo = posInfoByController[controller];
            // reset selected character to null
            // foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.UnAssignUpgradeData();

            if (foundPlayerPositionInfo.IsNull())
            {
                return;
            }
            
            switch (foundPlayerPositionInfo.assignedSelectionUIItem.currentSelectionState)
            {
                case CharacterSelectUIItem.CharacterSelectionState.SelectingCharacter:
                    OnCancel_CharacterSelection(foundPlayerPositionInfo);
                    break;
                case CharacterSelectUIItem.CharacterSelectionState.SelectingAbility:
                    OnCancel_AbilitySelection(foundPlayerPositionInfo);
                    break;
                case CharacterSelectUIItem.CharacterSelectionState.Ready:
                    OnCancel_Ready(foundPlayerPositionInfo);
                    break;
            }
        }
        
        private void OnCancel_CharacterSelection(CharacterSelectionPlayerPositionInfo playerPositionInfo)
        {
            //Disconnect Player
            playerPositionInfo.RemoveFromItem();
            //Assign first character to UI
            playerPositionInfo.assignedSelectionUIItem.OnCancel_CharacterSelect();
        }

        private void OnCancel_AbilitySelection(CharacterSelectionPlayerPositionInfo playerPositionInfo)
        {
            playerPositionInfo.assignedSelectionUIItem.OnCancel_AbilitySelect();
            //Note: may need to remove this if the character slot is suppose to continue as selected
            
            playerPositionInfo.xPos = 0;
            playerPositionInfo.yPos = 0;
            playerPositionInfo.currentItem = GetUIAtCoordinate(0, 0);
            playerPositionInfo.AddHoverToItem();
        }

        private void OnCancel_Ready(CharacterSelectionPlayerPositionInfo playerPositionInfo)
        {
            playerPositionInfo.assignedSelectionUIItem.OnCancel_Ready();
            playerPositionInfo.hasFinishedSelecting = false;
        }
        
        public void OnHorizontalUpdated(Player controller, bool isRight)
        {
            if (controller.IsNull() || !posInfoByController.ContainsKey(controller))
            {
                return;
            }

            var foundPlayerPositionInfo = posInfoByController[controller];
            var canChangeHorizontal = !foundPlayerPositionInfo.IsNull() 
                                      && !foundPlayerPositionInfo.hasFinishedSelecting
                                      && foundPlayerPositionInfo.assignedSelectionUIItem.currentSelectionState 
                                          is CharacterSelectUIItem.CharacterSelectionState.SelectingAbility 
                                          or CharacterSelectUIItem.CharacterSelectionState.SelectingCharacter;
            
            if (!canChangeHorizontal)
            {
                return;
            }

            switch (foundPlayerPositionInfo.assignedSelectionUIItem.currentSelectionState)
            {
                case CharacterSelectUIItem.CharacterSelectionState.SelectingCharacter:
                    OnHorizontalChange_Character(foundPlayerPositionInfo, isRight);
                    break;
                case CharacterSelectUIItem.CharacterSelectionState.SelectingAbility:
                    OnHorizontalChange_Ability(foundPlayerPositionInfo, isRight);
                    break;
            }
        }

        private void OnHorizontalChange_Character(CharacterSelectionPlayerPositionInfo playerPositionInfo, bool isRight)
        {
            var currentXPos = playerPositionInfo.xPos;
            var currentYPos = playerPositionInfo.yPos;
            var selectionUIItem = playerPositionInfo.assignedSelectionUIItem;
                
            if (!playerPositionInfo.currentItem.IsNull())
            {
                playerPositionInfo.RemoveFromItem();
            }
            
            var nextXIndex = WrapIndex(isRight ? currentXPos + 1 : currentXPos - 1, 
                activeCharacterSlotsByRow[currentYPos].Count);

            playerPositionInfo.currentItem = GetUIAtCoordinate(nextXIndex, currentYPos);
            playerPositionInfo.xPos = nextXIndex;
            playerPositionInfo.AddHoverToItem();
            
            selectionUIItem.ChangeSelectedCharacter(playerPositionInfo.currentItem.assignedCharacterData);
        }

        private void OnHorizontalChange_Ability(CharacterSelectionPlayerPositionInfo playerPositionInfo, bool isRight)
        {
            var currentAbilityIndex = playerPositionInfo.currentAbilityIndex;
            var nextAbilityIndex = WrapIndex(isRight ? currentAbilityIndex + 1 : currentAbilityIndex - 1, 
                allAssignableAbilities.Count);
            
            playerPositionInfo.currentAbilityIndex = nextAbilityIndex;
            playerPositionInfo.assignedSelectionUIItem.ChangeSelectedAbility(allAssignableAbilities[nextAbilityIndex]);
        }
        
        public void OnVerticalUpdated(Player controller, bool isUp)
        {
            if (controller.IsNull() || !posInfoByController.ContainsKey(controller))
            {
                return;
            }
            
            var foundPlayerPositionInfo = posInfoByController[controller];
            
            if (foundPlayerPositionInfo.hasFinishedSelecting 
                || foundPlayerPositionInfo.assignedSelectionUIItem.currentSelectionState != CharacterSelectUIItem.CharacterSelectionState.SelectingCharacter)
            {
                return;
            }
            
            var currentXPos = foundPlayerPositionInfo.xPos;
            var currentYPos = foundPlayerPositionInfo.yPos;
            
            if (!foundPlayerPositionInfo.currentItem.IsNull())
            {
                foundPlayerPositionInfo.RemoveFromItem();
            }
            
            //Note: has to be reversed because index 0 is at the top and last index is on the bottom.
            var nextYIndex = WrapIndex(isUp ? currentYPos - 1 : currentYPos + 1, activeCharacterSlotsByRow.Count);

            //Next X index might be 0 or the highest value in that row. Doesn't wrap when moving in Y axis
            var nextXIndex = ReturnLowest(currentXPos, activeCharacterSlotsByRow[nextYIndex].Count - 1);

            foundPlayerPositionInfo.currentItem = GetUIAtCoordinate(nextXIndex, nextYIndex);
            foundPlayerPositionInfo.xPos = nextXIndex;
            foundPlayerPositionInfo.yPos = nextYIndex;
            foundPlayerPositionInfo.AddHoverToItem();
            
            foundPlayerPositionInfo.assignedSelectionUIItem.ChangeSelectedCharacter(foundPlayerPositionInfo.currentItem.assignedCharacterData);
        }
        
        private void TryStartGame()
        {
            if (!m_readyToPlay)
            {
                return;
            }

            if (m_loadingGame)
            {
                return;
            }

            m_loadingGame = true;
            
            //Assign all chosen players to correct players in MatchGameController
            foreach (var uiItem in m_uiItems)
            {
                if (uiItem.currentSelectionState == CharacterSelectUIItem.CharacterSelectionState.Disconnected)
                {
                    continue;
                }
                MatchGameController.Instance.AssignSelectedCharacter(uiItem.currentCharacter, uiItem.assignedPlayer, uiItem.currentAbilityData);
            }
            
            //go to game screen
            SceneController.Instance.LoadScene(SceneName.TestScene, true);
        }

        private void ReInputOnControllerDisconnectedEvent(ControllerStatusChangedEventArgs _controllerStatus)
        {
            if (_controllerStatus.controllerId >= m_uiItems.Count)
            {
                return;
            }
            
            m_uiItems[_controllerStatus.controllerId].DisconnectPlayer();
        }

        private void ReInputOnControllerConnectedEvent(ControllerStatusChangedEventArgs _controllerStatus)
        {
            if (_controllerStatus.controllerId >= m_uiItems.Count)
            {
                return;
            }
            
            m_uiItems[_controllerStatus.controllerId].Initialize(_controllerStatus.controllerId, this);
        }

        #endregion
        
        
    }
}