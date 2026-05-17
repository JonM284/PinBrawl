using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Data;
using DG.Tweening;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Character;
using Runtime.UI.Icons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Items
{
    public class CharacterSelectionSlotUIItem: MonoBehaviour
    {
        [SerializeField] private float upscaleAmount = 1.3f;
        [SerializeField] private IconBase characterIcon;
        [SerializeField] private Image selectionObject;
        [SerializeField] private List<PlayerMarkers> playerMarkers = new List<PlayerMarkers>();

        private List<CharacterSelectUIItem> currentHoveredCharacters = new ();
        private bool hasGrown;
        private SemaphoreSlim changeSizeSemaphoreSlim = new SemaphoreSlim(1, 1);

        #region Accessors

        public int xIndex { get; private set; }
        public int yIndex { get; private set; }

        public CharacterData assignedCharacterData { get; private set; }
        
        #endregion

        #region Class Implementation

        
        public async UniTask InitializeItem(CharacterData characterData, int _xIndex, int _yIndex)
        {
            if (characterData.IsNull())
            {
                return;
            }
            
            assignedCharacterData = characterData;
            xIndex = _xIndex;
            yIndex = _yIndex;
            await characterIcon.GetIcon(characterData.characterIconRef);
        }

        public void AddHoveringPlayer(CharacterSelectUIItem hoveringPlayer)
        {
            if (currentHoveredCharacters.IsNull() || currentHoveredCharacters.Contains(hoveringPlayer))
            {
                return;
            }

            currentHoveredCharacters.Add(hoveringPlayer);
            UpdateItem();
        }

        public void RemoveHoveringPlayer(CharacterSelectUIItem unhoveringPlayer)
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
                ? currentHoveredCharacters[0].assignedColor : Color.white);
            //HideShowSelected(currentHoveredCharacters.Count > 0);
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
                currentMarker.markerImage.color = currentHoveredCharacters[i].assignedColor;
                currentMarker.markerImage.enabled = true;
                currentMarker.markerText.text = $"P{currentHoveredCharacters[i].playerIndex + 1}";
            }
        }
        
        private void SetColor(Color newColor)
        {
            selectionObject.color = newColor;
        }
        
        private void HideShowSelected(bool isShow)
        {
            selectionObject.gameObject.SetActive(isShow);
        }

        private async UniTask AdjustSize()
        {
            await changeSizeSemaphoreSlim.WaitAsync();
            try
            {
                var newScale = hasGrown ? 1f : upscaleAmount;
                await transform.DOScale(newScale, 0.1f);
            }
            finally
            {
                hasGrown = !hasGrown;
                changeSizeSemaphoreSlim.Release();
            }
        }
        
        #endregion

        
        
    }
}