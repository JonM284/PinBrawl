using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Character;
using Runtime.UI.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.DataModels
{
    public class MultiPerkSelectionDataModel: MonoBehaviour
    {
        
        #region Read-ONLY

        private readonly string confirmActionName = "Select";
        private readonly string cancelActionName = "Cancel";

        #endregion
        
        #region Nested Classes

        private class PlayerPositionInfo
        {
            public int xPos = 0, yPos = 0;
            public UpgradeUIItem currentItem;
            public bool hasFinishedSelecting = false;
            public PerkSelectionUIItem assignedUpgradeSelectionUIItem;

            public PlayerPositionInfo(PerkSelectionUIItem upgradeSelector)
            {
                assignedUpgradeSelectionUIItem = upgradeSelector;
                hasFinishedSelecting = false;
            }
        }
        
        #endregion
        
        #region Serialized Fields

        [SerializeField] private List<PerkSelectionUIItem> playerUpgradeSelectors = new();

        [SerializeField] private List<UpgradeHolderUIItem> upgradeHolderItems = new();

        [SerializeField] private Image background, shadeImage;
        
        #endregion

        #region Private Fields
        
        
        private CancellationTokenSource cts = new CancellationTokenSource();

        private Dictionary<BaseCharacter, PlayerPositionInfo> posInfoByCharacter = new();

        private int maxSelectedUpgrades = 2;

        
        #endregion
        
        #region Accessors

        public bool isSelecting { get; private set; }

        public bool allPlayersHaveSelected { get; private set; }

        public List<Player> currentPlayerControllers { get; private set;}

        public List<BaseCharacter> currentPlayers { get; private set; }

        #endregion

        #region Class Implementation

        private void SetActiveState(bool _isActive)
        {
            isSelecting = _isActive;
        }

        public async UniTask PreSetupScreen(BaseCharacter winningCharacter, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            background.color = winningCharacter.playerColor;
            SetActiveState(true);
            await UniTask.Yield();
        }

        public async UniTask PlayOpeningAnimation(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            //ToDo: animation        

            await shadeImage.DOFade(0f, 0.5f).ToUniTask(cancellationToken: token);
            
            await UniTask.Yield();
        }

        public async UniTask SetupSelectionScreenPlayers(List<BaseCharacter> selectingCharacters, BaseCharacter winningPlayer, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (selectingCharacters.IsNull() || selectingCharacters.Count == 0)
            {
                Debug.Log("Base Character Null");
                return;
            }
            
            currentPlayers = new List<BaseCharacter>();
            currentPlayerControllers = new List<Player>();
            posInfoByCharacter.Clear();
            
            currentPlayers = selectingCharacters.ToList();

            for (var i = 0; i < currentPlayers.Count; i++)
            {
                var isWinningPlayer = currentPlayers[i] == winningPlayer;
                await playerUpgradeSelectors[i].Initialize(currentPlayers[i], this, isWinningPlayer, token);

                if (isWinningPlayer)
                {
                    continue;   
                }
                
                var playerController = currentPlayers[i].GetPlayerController();
                currentPlayerControllers.Add(playerController);
                var newPosInfo = new PlayerPositionInfo(playerUpgradeSelectors[i]);
                newPosInfo.currentItem = GetUIAtCoordinate(newPosInfo.xPos, newPosInfo.yPos);
                newPosInfo.currentItem.AddHoveringPlayer(currentPlayers[i]);
                posInfoByCharacter.Add(currentPlayers[i], newPosInfo);
            }

            foreach (var item in upgradeHolderItems)
            {
                await item.InitializeUpgrades();
            }
        }

        public async UniTask ReadPlayerInputs(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            allPlayersHaveSelected = false;

            var usablePlayerUpgradeSelectors = playerUpgradeSelectors.Where(t => t.IsInitialized).ToList();
            
            while (!allPlayersHaveSelected)
            {
                if (allPlayersHaveSelected)
                {
                    break;
                }

                usablePlayerUpgradeSelectors.ForEach(ui => ui.InputReader.ReadInputs());
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
            }
            
            SetActiveState(false);
        }

        public void UpdateHorizontalPosition(BaseCharacter baseCharacter, bool isRight)
        {
            if (baseCharacter.IsNull() || !posInfoByCharacter.ContainsKey(baseCharacter))
            {
                return;
            }

            var foundPlayerPositionInfo = posInfoByCharacter[baseCharacter];
            
            if (foundPlayerPositionInfo.hasFinishedSelecting)
            {
                return;
            }
            
            var currentXPos = foundPlayerPositionInfo.xPos;
            var currentYPos = foundPlayerPositionInfo.yPos;
            
            if (!foundPlayerPositionInfo.currentItem.IsNull())
            {
                foundPlayerPositionInfo.currentItem.RemoveHoveringPlayer(baseCharacter);
            }
            
            var nextXIndex = WrapIndex(isRight ? currentXPos + 1 : currentXPos - 1, 
                upgradeHolderItems[currentYPos].maxXIndex);

            foundPlayerPositionInfo.currentItem = GetUIAtCoordinate(nextXIndex, currentYPos);
            foundPlayerPositionInfo.xPos = nextXIndex;
            foundPlayerPositionInfo.currentItem.AddHoveringPlayer(baseCharacter);
        }

        public void UpdateVerticalPosition(BaseCharacter baseCharacter, bool isUp)
        {
            if (baseCharacter.IsNull() || !posInfoByCharacter.ContainsKey(baseCharacter))
            {
                return;
            }
            
            var foundPlayerPositionInfo = posInfoByCharacter[baseCharacter];
            
            if (foundPlayerPositionInfo.hasFinishedSelecting)
            {
                return;
            }
            
            var currentXPos = foundPlayerPositionInfo.xPos;
            var currentYPos = foundPlayerPositionInfo.yPos;
            
            if (!foundPlayerPositionInfo.currentItem.IsNull())
            {
                foundPlayerPositionInfo.currentItem.RemoveHoveringPlayer(baseCharacter);
            }
            
            //Note: has to be reversed because index 0 is at the top and last index is on the bottom.
            var nextYIndex = WrapIndex(isUp ? currentYPos - 1 : currentYPos + 1, 
                upgradeHolderItems.Count);

            //Next X index might be 0 or the highest value in that row. Doesn't wrap when moving in Y axis
            var nextXIndex = ReturnLowest(currentXPos, upgradeHolderItems[nextYIndex].maxXIndex - 1);

            foundPlayerPositionInfo.currentItem = GetUIAtCoordinate(nextXIndex, nextYIndex);
            foundPlayerPositionInfo.xPos = nextXIndex;
            foundPlayerPositionInfo.yPos = nextYIndex;
            foundPlayerPositionInfo.currentItem.AddHoveringPlayer(baseCharacter);
        }

        public void OnSelectPressed(BaseCharacter baseCharacter)
        {
            if (baseCharacter.IsNull() || !posInfoByCharacter.ContainsKey(baseCharacter))
            {
                return;
            }
            
            var foundPlayerPositionInfo = posInfoByCharacter[baseCharacter];

            if (foundPlayerPositionInfo.hasFinishedSelecting)
            {
                return;
            }
            
            if (foundPlayerPositionInfo.currentItem.IsNull() || foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.IsNull())
            {
                Debug.LogError($"[MultiPerkSelection] CurrentItem null?: {foundPlayerPositionInfo.currentItem.IsNull()}" +
                               $" SelectionUIItem NULL?: {foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.IsNull()}");
                return;
            }
            
            foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.AssignUpgradeData(foundPlayerPositionInfo.currentItem.assignedUpgradeData);

            var playerHasReachedMax = foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.assignedUpgrades.Count >=
                                      maxSelectedUpgrades;
            
            foundPlayerPositionInfo.hasFinishedSelecting = playerHasReachedMax;

            if (!playerHasReachedMax) return;
            
            foundPlayerPositionInfo.currentItem.RemoveHoveringPlayer(baseCharacter);
            foundPlayerPositionInfo.currentItem = null;

            CheckAllPlayersFinished();
        }

        private void CheckAllPlayersFinished()
        {
            allPlayersHaveSelected = posInfoByCharacter.Values.Count(ppi => ppi.hasFinishedSelecting) >=
                                     currentPlayers.Count - 1;
        }
        
        public void OnCancelPressed(BaseCharacter baseCharacter)
        {
            if (baseCharacter.IsNull() || !posInfoByCharacter.ContainsKey(baseCharacter))
            {
                return;
            }

            var foundPlayerPositionInfo = posInfoByCharacter[baseCharacter];
            foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.UnAssignUpgradeData();

            if (!foundPlayerPositionInfo.hasFinishedSelecting) return;
            
            foundPlayerPositionInfo.hasFinishedSelecting = false;
            foundPlayerPositionInfo.xPos = 0;
            foundPlayerPositionInfo.yPos = 0;
            foundPlayerPositionInfo.currentItem = GetUIAtCoordinate(0, 0);
            foundPlayerPositionInfo.currentItem.AddHoveringPlayer(baseCharacter);
        }
        
        private UpgradeUIItem GetUIAtCoordinate(int xIndex, int yIndex)
        {
            return upgradeHolderItems[yIndex].GetUpgradeItemByIndex(xIndex);
        }

        private int ReturnLowest(int index, int size)
        {
            return Mathf.Min(index, size);
        }
        
        private int WrapIndex(int index, int size)
        {
            return ((index % size) + size) % size;
        }

        public async UniTask UpgradeAllPlayers(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            
            foreach (var kvp in posInfoByCharacter)
            {
                foreach (var upgradeDatas in kvp.Value.assignedUpgradeSelectionUIItem.assignedUpgrades)
                {
                    //Add to character
                    await kvp.Key.AddPerk(upgradeDatas, token);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }

            await CleanUp();
            
            //Next player can choose => OR END
        }

        private async UniTask CleanUp()
        {
            currentPlayers.Clear();
            currentPlayerControllers.Clear();
            posInfoByCharacter.Clear();
            await UniTask.CompletedTask;
        }
        
        #endregion
        
    }
}