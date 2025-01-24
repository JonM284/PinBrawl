using Data.StatusDatas;
using GameControllers;
using Project.Scripts.Utils;
using Runtime.Character;
using UnityEngine;

namespace Runtime.Statuses
{
    public class StatusEntityBase: MonoBehaviour, IStatus
    {

        #region Serialized Fields

        [SerializeField] protected StatusData m_statusData;

        #endregion
        
        #region IStatus Inherited Methods

        public float statusTimeMax { get; set; }
        
        public float statusTimeCurrent { get; set; }
        
        public BaseCharacter currentOwner { get; set; }

        public bool isInitialized { get; set; }

        public virtual void OnApply(BaseCharacter _baseCharacter)
        {
            if (_baseCharacter.IsNull())
            {
                return;
            }

            currentOwner = _baseCharacter;
            statusTimeMax = m_statusData.timeMax;
            statusTimeCurrent = statusTimeMax;
            
            isInitialized = true;
        }

        public virtual void OnTick()
        {
        }

        public virtual void OnEnd()
        {
        }

        public virtual StatusData GetStatusData()
        {
            return m_statusData;
        }

        public virtual string GetGUID()
        {
            return m_statusData.statusIdentifierGUID;
        }

        #endregion
        
        
    }
}