using System;
using System.Threading;
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
            EnabledFunctions();
        }

        private void OnDisable()
        {
            DisabledFunctions();
        }

        #endregion

        #region Class Implementation

        protected virtual void EnabledFunctions()
        {
            MatchGameController.Instance.AssignGameMode(this);
        }

        protected virtual void DisabledFunctions()
        {
            MatchGameController.Instance.UnassignGameMode(this);
        }

        public virtual async UniTask Initialize(int _pointsNeededToWin, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
        }
        
        public virtual async UniTask UpdateScores(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }
        
        public virtual async UniTask ExtraActions(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }
        
        public virtual async UniTask ShowFinalScreen(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }

        public virtual async UniTask ShowEnd(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }

        public virtual async UniTask DeInitialize(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            m_selfUIWindow.Close();
        }
        
        #endregion





    }
}