using System;
using Cysharp.Threading.Tasks;
using Data;
using Runtime.GameControllers;
using Runtime.UI;
using UnityEngine;

namespace Runtime.GameModes
{
    public abstract class GameModeManagerBase: MonoBehaviour
    {

        #region Serialized Fields
        
        [SerializeField] protected UIWindowDialog m_selfUIWindow;
        
        #endregion

        #region Accessors

        public bool hasEndedGame { get; protected set; }
        
        public bool hasFinishedRound { get; protected set; }

        public bool isSetBased { get; protected set; }

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            MatchGameController.Instance.AssignGameMode(this);
        }

        private void OnDisable()
        {
            MatchGameController.Instance.UnassignGameMode(this);
        }

        #endregion

        #region Class Implementation

        public virtual async UniTask Initialize(int _pointsNeededToWin = 0)
        {
            await UniTask.Yield();
        }
        
        public virtual async UniTask UpdateScores()
        {
            await UniTask.Yield();
        }
        
        public virtual async UniTask ExtraActions()
        {
            await UniTask.Yield();
        }
        
        public virtual async UniTask ShowFinalScreen()
        {
            await UniTask.Yield();
        }

        public virtual async UniTask ShowEnd()
        {
            await UniTask.Yield();
        }

        public virtual async UniTask DeInitialize()
        {
            m_selfUIWindow.Close();
        }
        
        #endregion





    }
}