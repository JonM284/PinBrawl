using System.Collections.Generic;
using Runtime.Gameplay;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName = "Dodge-ball/Game/Game Event")]
    public class GameEvent: ScriptableObject
    {

        private List<GameEventListener> m_listeners = new List<GameEventListener>();

        public void TriggerEvent()
        {
            if (m_listeners.Count == 0)
            {
                return;
            }
            
            foreach (var _listener in m_listeners)
            {
                _listener.OnEventTriggered();
            }
        }

        public void AddListener(GameEventListener _listener)
        {
            m_listeners.Add(_listener);
        }

        public void RemoveListener(GameEventListener _listener)
        {
            m_listeners.Remove(_listener);
        }


    }
}