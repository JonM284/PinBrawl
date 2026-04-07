using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.PerkDatas;
using DG.Tweening;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.UI.DataModels;
using Runtime.UI.Icons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Items
{
    public class PerkSelectionUIItem: MonoBehaviour
    {

        [SerializeField] private TMP_Text playerName;
        [SerializeField] private List<Image> playerBackgrounds;
        [SerializeField] private Image playerCharacterImage;
        [SerializeField] private List<IconBase> selectedPerkImages = new();
        [SerializeField] private PlayerUIInputReader inputReader;
        [SerializeField] private GameObject mask;
        
        private int maxAmountOfUpgrades = 2;
        
        public PlayerUIInputReader InputReader => inputReader;

        private Stack<PerkDataBase> selectedUpgrades = new();

        public Stack<PerkDataBase> assignedUpgrades => selectedUpgrades;

        private MultiPerkSelectionDataModel mpsDataModel;

        private float disabledSize = 0.75f;

        public bool hasFinishedSelection => selectedUpgrades.Count >= maxAmountOfUpgrades;

        public bool IsInitialized { get; private set; }

        #region Class Implementation


        public async UniTask Initialize(BaseCharacter character, MultiPerkSelectionDataModel multiPerkSelectionDataModel, bool isWinningPlayer, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (character.IsNull())
            {
                return;
            }

            playerName.text = character.characterData.characterName;
            playerBackgrounds.ForEach(img => img.color = character.playerColor);
            playerCharacterImage.sprite = character.characterData.characterIconRef;
            selectedUpgrades.Clear();
            selectedPerkImages.ForEach(icon => icon.RemoveIcon());
            
            mask.SetActive(isWinningPlayer);
            var localScale = isWinningPlayer ? disabledSize * Vector3.one : Vector3.one;
            
            //Winning Player doesn't upgrade
            if (isWinningPlayer)
            {
                await transform.DOScale(localScale, 0.5f).WithCancellation(token);
                return;
            }
            
            transform.localScale = localScale;
            mpsDataModel = multiPerkSelectionDataModel;
            inputReader.InitializeItem(character.GetPlayerController(), character, OnSelectCallback, OnCancelCallback, 
                OnHorizontalChangeCallback, OnVerticalChangeCallback);
            IsInitialized = true;
            await UniTask.Yield(cancellationToken: token);
        }

        private void OnVerticalChangeCallback(BaseCharacter character, bool isUp)
        {
            mpsDataModel.UpdateVerticalPosition(character, isUp);
        }

        private void OnHorizontalChangeCallback(BaseCharacter character, bool isRight)
        {
            mpsDataModel.UpdateHorizontalPosition(character, isRight);
        }

        private void OnCancelCallback(BaseCharacter character)
        {
            mpsDataModel.OnCancelPressed(character);
        }

        private void OnSelectCallback(BaseCharacter character)
        {
            mpsDataModel.OnSelectPressed(character);
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