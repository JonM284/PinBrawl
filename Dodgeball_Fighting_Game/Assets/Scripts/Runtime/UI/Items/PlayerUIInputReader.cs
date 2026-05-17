using System;
using Rewired;
using Runtime.Character;
using UnityEngine;

namespace Runtime.UI.Items
{
    public class PlayerUIInputReader: MonoBehaviour
    {

        #region Read-Only

        private readonly string confirmButton = "Select";
        private readonly string cancelButton = "Cancel";
        private readonly string horizontalAxis = "Move_Horizontal";
        private readonly string verticalAxis = "Move_Vertical";
        
        #endregion
        
        #region Actions

        //int (playerID)
        private Action<Player> OnSelectPressed;
        //int (playerID)
        private Action<Player> OnCancelPressed;
        //int (playerID), int (right/left)
        private Action<Player, bool> OnHorizontalAxisChanged;
        //int (playerID), int (up/down)
        private Action<Player, bool> OnVerticalAxisChanged;

        #endregion

        #region SerializeFields

        [SerializeField] private float axisThreshold = 0.3f;
        
        [SerializeField] private float controllerVibrationAmount = 0.75f;
        [SerializeField] private float controllerVibrationDuration = 0.15f;

        #endregion

        #region Private Fields

        private bool isInitialized;
        private bool confirmPressed, canMoveSelection;
        private float horizontalInput, verticalInput;
        
        #endregion

        #region Accessors
        
        public Player assignedPlayer { get; private set; }

        public BaseCharacter assignedCharacter { get; private set; }

        #endregion

        #region Class Implementation

        public void InitializeItem(Player _newPlayer, BaseCharacter _assignedCharacter, 
            Action<Player> _onSelectCallback, Action<Player> _onCancelCallback,
            Action<Player, bool> _onHorizontalChangeCallback, Action<Player, bool> _onVerticalChangeCallback)
        {
            assignedPlayer = _newPlayer;
            assignedCharacter = _assignedCharacter;
            OnSelectPressed = _onSelectCallback;
            OnCancelPressed = _onCancelCallback;
            OnHorizontalAxisChanged = _onHorizontalChangeCallback;
            OnVerticalAxisChanged = _onVerticalChangeCallback;
            assignedPlayer.SetVibration(0, controllerVibrationAmount, controllerVibrationDuration);
            isInitialized = true;
        }

        public void ResetItem()
        {
            OnSelectPressed = null;
            OnCancelPressed = null;
            OnHorizontalAxisChanged = null;
            OnVerticalAxisChanged = null;
            isInitialized = false;
            assignedPlayer = null;
            assignedCharacter = null;
        }
        
        public void ReadInputs()
        {
            if (!isInitialized)
            {
                return;
            }
            
            horizontalInput = assignedPlayer.GetAxis(horizontalAxis);
            verticalInput = assignedPlayer.GetAxis(verticalAxis);

            if (Mathf.Abs(horizontalInput) >= axisThreshold && canMoveSelection)
            {
                ChangeHorizontalSelected(horizontalInput >= axisThreshold);
            }else if (Mathf.Abs(verticalInput) >= axisThreshold && canMoveSelection)
            {
                ChangeVerticalSelected(verticalInput >= axisThreshold);
            }else if (Mathf.Abs(horizontalInput) < axisThreshold && Mathf.Abs(verticalInput) < axisThreshold && !canMoveSelection)
            {
                canMoveSelection = true;
                Debug.Log($"reset selection");
            }

            if (assignedPlayer.GetButtonDown(confirmButton))
            {
                OnSelectPressed?.Invoke(assignedPlayer);
            }
            
            if (assignedPlayer.GetButtonDown(cancelButton))
            {
                OnCancelPressed?.Invoke(assignedPlayer);
            }
        }
        
        private void ChangeHorizontalSelected(bool _isRight)
        {
            OnHorizontalAxisChanged?.Invoke(assignedPlayer, _isRight);
            canMoveSelection = false;
            Debug.Log($"Update Horizontal, isRight?:{_isRight}");
        }

        private void ChangeVerticalSelected(bool _isUp)
        {
            canMoveSelection = false;
            OnVerticalAxisChanged?.Invoke(assignedPlayer, _isUp);
            Debug.Log($"Update Vertical, isUp?:{_isUp}");
        }
        
        #endregion


    }
}