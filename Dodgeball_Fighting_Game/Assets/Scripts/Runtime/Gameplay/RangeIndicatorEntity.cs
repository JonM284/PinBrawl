using Runtime.GameControllers;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class RangeIndicatorEntity: MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField] private LineRenderer m_lineRenderer;

        #endregion
        
        #region Class Implementation

        public void Initialize(Vector3 _startPos, Vector3 _endPos, float _scale, float _duration)
        {
            m_lineRenderer.SetPosition(0, _startPos);
            m_lineRenderer.SetPosition(1, _endPos);

            m_lineRenderer.startWidth = _scale;
            m_lineRenderer.endWidth = _scale;
            
            TickGameController.Instance.CreateNewTimer($"lineRend{_startPos.ToString()}", _duration, false, Close);
        }

        public void Close()
        {
            JuiceGameController.Instance.CacheRangeIndicator(this);
        }

        #endregion
        
        
    }
}