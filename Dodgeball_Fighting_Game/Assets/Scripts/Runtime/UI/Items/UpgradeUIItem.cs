using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data.PerkDatas;
using DG.Tweening;
using NUnit.Framework;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.UI.Icons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Items
{
    public class UpgradeUIItem: MonoBehaviour
    {

        #region Nested Classes

        [Serializable]
        public class PlayerMarkers
        {
            public Image markerImage;
            public TMP_Text markerText;
        }

        #endregion
        
        [SerializeField] private IconBase upgradeIcon;
        [SerializeField] private Image selectionObject;
        [SerializeField] private List<PlayerMarkers> playerMarkers = new List<PlayerMarkers>();
        [SerializeField] private CanvasGroup descriptionCanvasGroup;
        [SerializeField] private RectTransform descriptionRectTransform;
        [SerializeField] private TMP_Text descriptionText;

        private List<BaseCharacter> currentHoveredCharacters = new ();
        private bool hasGrown;
        private SemaphoreSlim changeSizeSemaphoreSlim = new SemaphoreSlim(1, 1);

        public PerkDataBase assignedUpgradeData { get; private set; }

        public async UniTask InitializeItem(PerkDataBase upgrade)
        {
            if (upgrade.IsNull())
            {
                return;
            }
            
            assignedUpgradeData = upgrade;
            descriptionText.text = upgrade.perkDescription;
            LayoutRebuilder.MarkLayoutForRebuild(descriptionRectTransform);
            await upgradeIcon.GetIcon(upgrade.perkIconRef);
        }

        public void AddHoveringPlayer(BaseCharacter hoveringPlayer)
        {
            if (currentHoveredCharacters.IsNull() || currentHoveredCharacters.Contains(hoveringPlayer))
            {
                return;
            }

            currentHoveredCharacters.Add(hoveringPlayer);
            UpdateItem();
        }

        public void RemoveHoveringPlayer(BaseCharacter unhoveringPlayer)
        {
            if (currentHoveredCharacters.IsNull() || currentHoveredCharacters.Count == 0 || !currentHoveredCharacters.Contains(unhoveringPlayer))
            {
                return;
            }

            currentHoveredCharacters.Remove(unhoveringPlayer);
            UpdateItem();
        }

        private void UpdateItem()
        {
            SetColor(currentHoveredCharacters.Count > 0 && !currentHoveredCharacters[0].IsNull() 
                ? currentHoveredCharacters[0].playerColor : Color.white);
            HideShowSelected(currentHoveredCharacters.Count > 0);
            UpdatePlayerMarkers();

            if ((currentHoveredCharacters.Count > 0 && !hasGrown) || (currentHoveredCharacters.Count == 0 && hasGrown))
            {
                AdjustSize().Forget();
            }
        }

        private void UpdatePlayerMarkers()
        {
            playerMarkers.ForEach(marker =>
            {
                marker.markerImage.enabled = false;
                marker.markerText.text = string.Empty;
            });

            if (currentHoveredCharacters.Count == 0)
            {
                return;
            }
            
            for (var i = 0; i < currentHoveredCharacters.Count; i++)
            {
                var currentMarker = playerMarkers[i];
                currentMarker.markerImage.color = currentHoveredCharacters[i].playerColor;
                currentMarker.markerImage.enabled = true;
                currentMarker.markerText.text = $"P{currentHoveredCharacters[i].GetPlayerIndex() + 1}";
            }
        }
        
        private void SetColor(Color newColor)
        {
            selectionObject.color = newColor;
        }
        
        private void HideShowSelected(bool isShow)
        {
            selectionObject.gameObject.SetActive(isShow);
            descriptionCanvasGroup.alpha = isShow ? 1f : 0f;
        }

        private async UniTask AdjustSize()
        {
            await changeSizeSemaphoreSlim.WaitAsync();
            try
            {
                var newScale = hasGrown ? 1f : 1.5f;
                await transform.DOScale(newScale, 0.1f);
            }
            finally
            {
                hasGrown = !hasGrown;
                changeSizeSemaphoreSlim.Release();
            }
        }
        
    }
}