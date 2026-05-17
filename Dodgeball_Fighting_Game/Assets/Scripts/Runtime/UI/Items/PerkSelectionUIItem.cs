using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.PerkDatas;
using DG.Tweening;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.UI.DataModels;
using Runtime.UI.Icons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Items
{
    public class PerkSelectionUIItem: MonoBehaviour
    {

        [SerializeField] private TMP_Text playerName, pointsText;
        [SerializeField] private List<Image> playerBackgrounds;
        [SerializeField] private Image playerCharacterImage;
        [SerializeField] private List<IconBase> selectedPerkImages = new();
        [SerializeField] private PlayerUIInputReader inputReader;
        [SerializeField] private GameObject mask, positionProxy;
        
        private int maxAmountOfUpgrades = 2;
        
        public PlayerUIInputReader InputReader => inputReader;

        private Stack<PerkDataBase> selectedUpgrades = new();

        public Stack<PerkDataBase> assignedUpgrades => selectedUpgrades;

        private MultiPerkSelectionDataModel mpsDataModel;

        private float disabledSize = 0.75f;

        public bool hasFinishedSelection => selectedUpgrades.Count >= maxAmountOfUpgrades;

        public bool IsWinningPlayer { get; private set; }

        public bool IsInitialized { get; private set; }

        public GameObject PositionProxy => positionProxy;

        public BaseCharacter assignedCharacter { get; private set; }

        #region Class Implementation


        public async UniTask Initialize(PlayerMatchStats playerStats, MultiPerkSelectionDataModel multiPerkSelectionDataModel, bool isWinningPlayer, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (playerStats.IsNull())
            {
                return;
            }

            mask.SetActive(false);
            transform.localPosition = Vector3.zero;
            assignedCharacter = playerStats.playerCharacter;
            playerName.text = assignedCharacter.characterData.characterName;
            pointsText.text = playerStats.roundPoints.ToString();
            playerBackgrounds.ForEach(img => img.color = assignedCharacter.playerColor);
            playerCharacterImage.sprite = assignedCharacter.characterData.characterIconRef;
            selectedUpgrades.Clear();
            selectedPerkImages.ForEach(icon => icon.RemoveIcon());
            IsWinningPlayer = isWinningPlayer;
            
            //Winning Player doesn't upgrade
            if (isWinningPlayer)
            {
                return;
            }

            transform.localScale = Vector3.one;
            mpsDataModel = multiPerkSelectionDataModel;
            inputReader.InitializeItem(assignedCharacter.GetPlayerController(), assignedCharacter, OnSelectCallback, OnCancelCallback, 
                OnHorizontalChangeCallback, OnVerticalChangeCallback);
            IsInitialized = true;
            await UniTask.Yield(cancellationToken: token);
        }

        public async UniTask WinningPlayerAnimationAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            mask.SetActive(true);
            await transform.DOScale(disabledSize, 0.5f).SetEase(Ease.InOutElastic).WithCancellation(token);
            await transform.DOLocalMoveX(500f, 1f).SetEase(Ease.InOutElastic).WithCancellation(token);
        }

        private void OnVerticalChangeCallback(Player controller, bool isUp)
        {
            mpsDataModel.UpdateVerticalPosition(controller, isUp);
        }

        private void OnHorizontalChangeCallback(Player controller, bool isRight)
        {
            mpsDataModel.UpdateHorizontalPosition(controller, isRight);
        }

        private void OnCancelCallback(Player controller)
        {
            mpsDataModel.OnCancelPressed(controller);
        }

        private void OnSelectCallback(Player controller)
        {
            mpsDataModel.OnSelectPressed(controller);
        }

        public void AssignUpgradeData(PerkDataBase newPerk)
        {
            if (newPerk.IsNull())
            {
                return;
            }
            
            selectedUpgrades.Push(newPerk);
            UpdateIcons();
        }

        public void UnAssignUpgradeData()
        {
            if (selectedUpgrades.Count == 0)
            {
                return;
            }

            selectedUpgrades.Pop();
            UpdateIcons();
        }

        private void UpdateIcons()
        {
            selectedPerkImages.ForEach(img => img.RemoveIcon());

            if (selectedUpgrades.Count == 0)
            {
                return;
            }

            var currentIndex = 0;
            foreach (var perkDataBase in selectedUpgrades)
            {
                selectedPerkImages[currentIndex].GetIcon(perkDataBase.perkIconRef).Forget();
                currentIndex++;
            }
        }

        #endregion
        
    }
}