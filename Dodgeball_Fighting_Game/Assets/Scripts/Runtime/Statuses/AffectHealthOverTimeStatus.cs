using Data.StatusDatas;
using Runtime.Character;
using UnityEngine;

namespace Runtime.Statuses
{
    public class AffectHealthOverTimeStatus: StatusEntityBase
    {
        
        
        #region Accessors

        protected AffectHealthOverTimeStatusData m_affectHealthOverTimeStatusData => m_statusData as AffectHealthOverTimeStatusData;

        #endregion

        #region Private Fields

        private float m_lastTickTime;

        private float m_maxTime;

        #endregion

        #region StatusEntityBase Inherited Methods

        public override void OnApply(BaseCharacter _baseCharacter)
        {
            base.OnApply(_baseCharacter);
            m_maxTime = Time.time + m_affectHealthOverTimeStatusData.timeMax;
            m_lastTickTime = Time.time;
        }

        public override void OnTick()
        {
            base.OnTick();

            if (Time.time > m_maxTime)
            {
                return;
            }

            if (Time.time - m_lastTickTime < m_affectHealthOverTimeStatusData.amountOfTimePerTick)
            {
                return;
            }

            if (m_affectHealthOverTimeStatusData.isHealing)
            {
                currentOwner.OnHeal(m_affectHealthOverTimeStatusData.amountChangePerTick);
            }
            else
            {
                currentOwner.OnDealDamage(transform, m_affectHealthOverTimeStatusData.amountChangePerTick);
            }
            
            
            m_lastTickTime = Time.time;
        }

        #endregion
        
        
        
    }
}