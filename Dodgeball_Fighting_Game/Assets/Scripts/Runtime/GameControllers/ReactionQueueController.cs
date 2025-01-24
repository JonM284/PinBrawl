using System;
using System.Collections;
using System.Collections.Generic;
using Project.Scripts.Utils;
using Runtime.Character;
using UnityEngine;

namespace Runtime.GameControllers
{
    public class ReactionQueueController: GameControllerBase
    {

        #region Static

        public static ReactionQueueController Instance { get; private set; }

        #endregion

        #region Nested Classes
        
        [Serializable]
        public class CharacterReactor
        {
            public IReactor reactor;
            public Action reactionToPerform;
        }
        
        #endregion
        
        #region Private Fields

        private List<CharacterReactor> m_queuedReactions = new List<CharacterReactor>();
        
        private Coroutine m_reactionCoroutine;
        
        #endregion

        #region Accessors

        public bool isDoingReactions => !m_reactionCoroutine.IsNull();

        #endregion

        #region GameControllerBase Inherited Methods

        public override void Initialize()
        {
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
            base.Initialize();
        }

        #endregion

        #region Class Implementation

        public void QueueReaction(IReactor _newQueuer, Action callback)
        {
            if (_newQueuer.IsNull())
            {
                return;
            }

            var _reactor = new CharacterReactor
            {
                reactor   = _newQueuer,
                reactionToPerform = callback
            };


            if (!m_queuedReactions.Contains(_reactor))
            {
                m_queuedReactions.Add(_reactor);
            }

            if (m_reactionCoroutine.IsNull())
            {
                m_reactionCoroutine = StartCoroutine(C_AllowCharacterPerformReaction());
            }
        }

        private void EndReactionQueue()
        {
            StopCoroutine(m_reactionCoroutine);
            m_reactionCoroutine = null;
        }

        private IEnumerator C_AllowCharacterPerformReaction()
        {
            yield return null;

            while (m_queuedReactions.Count > 0)
            {
                yield return null;

                var currentReactor = m_queuedReactions[0];
                
                currentReactor.reactionToPerform?.Invoke();
                yield return new WaitUntil(() => !currentReactor.reactor.isPerformingReaction);

                m_queuedReactions.Remove(currentReactor);
            }
            
            EndReactionQueue();
            
        }

        #endregion
        
        
    }
}