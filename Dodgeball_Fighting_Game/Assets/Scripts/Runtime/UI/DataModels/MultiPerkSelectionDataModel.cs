using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Character;
using Runtime.Gameplay;
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

        private class UpgradeSelectPlayerPositionInfo
        {
            public int xPos = 0, yPos = 0;
            public UpgradeUIItem currentItem;
            public bool hasFinishedSelecting = false;
            public PerkSelectionUIItem assignedUpgradeSelectionUIItem;

            public UpgradeSelectPlayerPositionInfo(PerkSelectionUIItem upgradeSelector)
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

        private Dictionary<Player, UpgradeSelectPlayerPositionInfo> posInfoByController = new();

        private int maxSelectedUpgrades = 2;

        private int amountOfPlayers;
        
        #endregion
        
        #region Accessors

        public bool isSelecting { get; private set; }

        public bool allPlayersHaveSelected { get; private set; }

        public List<Player> currentPlayerControllers { get; private set;}
        
        #endregion

        #region Class Implementation

        private void SetActiveState(bool _isActive)
        {
            isSelecting = _isActive;
        }

        public async UniTask FadeBlackScreen(bool isFadeIn, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await shadeImage.DOFade(isFadeIn ? 1f : 0f, 0.5f).ToUniTask(cancellationToken: token);
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
            await FadeBlackScreen(false, token);

            await RemoveWinnerAnimationAsync(token);
            
            await UniTask.Yield();
        }

        public async UniTask RemoveWinnerAnimationAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var roundWinner = playerUpgradeSelectors.FirstOrDefault(psui => psui.IsWinningPlayer);

            if (roundWinner.IsNull())
            {
                return;
            }

            await roundWinner.WinningPlayerAnimationAsync(token);

            await UniTask.Yield();

            roundWinner.PositionProxy.SetActive(false);
        }

        public async UniTask SetupSelectionScreenPlayers(List<PlayerMatchStats> allPlayerStats, PlayerMatchStats winningPlayerStats, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (allPlayerStats.IsNull() || allPlayerStats.Count == 0)
            {
                Debug.Log("Base Character Null");
                return;
            }
            
            foreach (var item in upgradeHolderItems)
            {
                await item.InitializeUpgrades();
            }
            
            currentPlayerControllers = new List<Player>();
            posInfoByController.Clear();
            amountOfPlayers = allPlayerStats.Count;
            
            playerUpgradeSelectors.ForEach(psui => psui.PositionProxy.gameObject.SetActive(false));

            for (var i = 0; i < allPlayerStats.Count; i++)
            {
                playerUpgradeSelectors[i].PositionProxy.gameObject.SetActive(true);
                var isWinningPlayer = allPlayerStats[i] == winningPlayerStats;
                await playerUpgradeSelectors[i].Initialize(allPlayerStats[i], this, isWinningPlayer, token);

                if (isWinningPlayer)
                {
                    continue;   
                }
                
                var playerController = allPlayerStats[i].playerCharacter.GetPlayerController();
                currentPlayerControllers.Add(playerController);
                var newPosInfo = new UpgradeSelectPlayerPositionInfo(playerUpgradeSelectors[i]);
                newPosInfo.currentItem = GetUIAtCoordinate(newPosInfo.xPos, newPosInfo.yPos);
                newPosInfo.currentItem.AddHoveringPlayer(allPlayerStats[i].playerCharacter);
                posInfoByController.Add(playerController, newPosInfo);
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

        public void UpdateHorizontalPosition(Player controller, bool isRight)
        {
            if (controller.IsNull() || !posInfoByController.ContainsKey(controller))
            {
                return;
            }

            var foundPlayerPositionInfo = posInfoByController[controller];
            
            if (foundPlayerPositionInfo.hasFinishedSelecting)
            {
                return;
            }
            
            var currentXPos = foundPlayerPositionInfo.xPos;
            var currentYPos = foundPlayerPositionInfo.yPos;
            var character = foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.assignedCharacter;
            
            if (!foundPlayerPositionInfo.currentItem.IsNull())
            {
                foundPlayerPositionInfo.currentItem.RemoveHoveringPlayer(character);
            }
            
            var nextXIndex = WrapIndex(isRight ? currentXPos + 1 : currentXPos - 1, 
                upgradeHolderItems[currentYPos].maxXIndex);

            foundPlayerPositionInfo.currentItem = GetUIAtCoordinate(nextXIndex, currentYPos);
            foundPlayerPositionInfo.xPos = nextXIndex;
            foundPlayerPositionInfo.currentItem.AddHoveringPlayer(character);
        }

        public void UpdateVerticalPosition(Player controller, bool isUp)
        {
            if (controller.IsNull() || !posInfoByController.ContainsKey(controller))
            {
                return;
            }
            
            var foundPlayerPositionInfo = posInfoByController[controller];
            
            if (foundPlayerPositionInfo.hasFinishedSelecting)
            {
                return;
            }
            
            var currentXPos = foundPlayerPositionInfo.xPos;
            var currentYPos = foundPlayerPositionInfo.yPos;
            var character = foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.assignedCharacter;
            
            if (!foundPlayerPositionInfo.currentItem.IsNull())
            {
                foundPlayerPositionInfo.currentItem.RemoveHoveringPlayer(character);
            }
            
            //Note: has to be reversed because index 0 is at the top and last index is on the bottom.
            var nextYIndex = WrapIndex(isUp ? currentYPos - 1 : currentYPos + 1, 
                upgradeHolderItems.Count);

            //Next X index might be 0 or the highest value in that row. Doesn't wrap when moving in Y axis
            var nextXIndex = ReturnLowest(currentXPos, upgradeHolderItems[nextYIndex].maxXIndex - 1);

            foundPlayerPositionInfo.currentItem = GetUIAtCoordinate(nextXIndex, nextYIndex);
            foundPlayerPositionInfo.xPos = nextXIndex;
            foundPlayerPositionInfo.yPos = nextYIndex;
            foundPlayerPositionInfo.currentItem.AddHoveringPlayer(character);
        }

        public void OnSelectPressed(Player controller)
        {
            if (controller.IsNull() || !posInfoByController.ContainsKey(controller))
            {
                return;
            }
            
            var foundPlayerPositionInfo = posInfoByController[controller];

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
            
            foundPlayerPositionInfo.currentItem.RemoveHoveringPlayer(foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.assignedCharacter);
            foundPlayerPositionInfo.currentItem = null;

            CheckAllPlayersFinished();
        }

        private void CheckAllPlayersFinished()
        {
            allPlayersHaveSelected = posInfoByController.Values.Count(ppi => ppi.hasFinishedSelecting) >=
                                     amountOfPlayers - 1;
        }
        
        public void OnCancelPressed(Player controller)
        {
            if (controller.IsNull() || !posInfoByController.ContainsKey(controller))
            {
                return;
            }

            var foundPlayerPositionInfo = posInfoByController[controller];
            foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.UnAssignUpgradeData();

            if (!foundPlayerPositionInfo.hasFinishedSelecting) return;
            
            foundPlayerPositionInfo.hasFinishedSelecting = false;
            foundPlayerPositionInfo.xPos = 0;
            foundPlayerPositionInfo.yPos = 0;
            foundPlayerPositionInfo.currentItem = GetUIAtCoordinate(0, 0);
            foundPlayerPositionInfo.currentItem.AddHoveringPlayer(foundPlayerPositionInfo.assignedUpgradeSelectionUIItem.assignedCharacter);
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
            
            foreach (var kvp in posInfoByController)
            {
                foreach (var upgradeDatas in kvp.Value.assignedUpgradeSelectionUIItem.assignedUpgrades)
                {
                    //Add to character
                    await kvp.Value.assignedUpgradeSelectionUIItem.assignedCharacter.AddPerk(upgradeDatas, token);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }

            await CleanUp();
            
            //Next player can choose => OR END
        }

        private async UniTask CleanUp()
        {
            playerUpgradeSelectors.ForEach(uhui => uhui.InputReader.ResetItem());
            currentPlayerControllers.Clear();
            posInfoByController.Clear();
            await UniTask.CompletedTask;
        }
        
        #endregion
        
    }
}