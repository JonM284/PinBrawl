using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Project.Scripts.Utils;
using Rewired;
using Runtime.GameControllers;
using Runtime.UI.Items;
using UnityEngine;

namespace Runtime.UI.DataModels
{
    public class CharacterSelectDataModel: MonoBehaviour
    {


        #region Serialized Fields

        [SerializeField] private List<CharacterSelectUIItem> m_uiItems = new List<CharacterSelectUIItem>();
        
        #endregion

        #region Private Fields

        private bool m_readyToPlay, m_loadingGame;

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
        }

        private void Start()
        {
            for (int i = 0; i < 4; i++)
            {
                m_uiItems[i].Initialize(i, this);
            }
        }

        #endregion

        #region Class Implementation

        public void UpdateReadyPlayers()
        {
            m_readyToPlay = m_uiItems.Count(csi => csi.isReady) >= 2;
        }

        public void StartGame()
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
            for (int i = 0; i < m_uiItems.Count; i++)
            {
                if (!m_uiItems[i].isReady || m_uiItems[i].currentCharacter.IsNull())
                {
                    continue;
                }
                
                MatchGameController.Instance.AssignSelectedCharacter(m_uiItems[i].currentCharacter, m_uiItems[i].assignedPlayer);
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