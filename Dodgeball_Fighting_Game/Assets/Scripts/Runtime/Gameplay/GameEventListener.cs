using Data;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.Gameplay
{
    public class GameEventListener: MonoBehaviour
    {

        [SerializeField] private GameEvent m_gameEvent;

        public UnityEvent onEventTriggered;

        private void OnEnable()
        {
            m_gameEvent.AddListener(this);
        }

        private void OnDisable()
        {
            m_gameEvent.RemoveListener(this);
        }

        public void OnEventTriggered()
        {
            onEventTriggered?.Invoke();
        }
    }
}