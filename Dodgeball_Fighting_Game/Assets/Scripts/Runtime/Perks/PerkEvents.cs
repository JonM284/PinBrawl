using Data.PerkDatas;
using Project.Scripts.Utils;
using UnityEngine;

namespace Runtime.Perks
{
    public class PerkEvents: MonoBehaviour
    {

        private EventTriggeredPerkData m_perkData;

        private bool m_hasUsedEvent;

        public void Initialize(EventTriggeredPerkData _perkData)
        {
            if (_perkData.IsNull())
            {
                return;
            }

            m_perkData = _perkData;

            if (!m_perkData.isStackable)
            {
                return;
            }
            
            
        }

/// <summary>
/// Hi all!
/// I'm Jon, I manage an international board game cafe in Akihabara.
/// I wanted to start running small in person play-testing events for indies to come and get feedback on their projects.
/// I wanted to know if this is something people would be interested in?
/// </summary>
        
        
        public void ReviveDyingPlayer()
        {
            if (m_perkData.isOneTimePerReset && m_hasUsedEvent)
            {
                return;
            }
            
            //Do Action
        }

        //Run Faster Every time _____ happens
        public void GainPlayerBoostStack()
        {
            if (m_perkData.isOneTimePerReset && m_hasUsedEvent)
            {
                return;
            }
            
            //Do Action
        }

        //Ability Gets stronger for every stack
        public void GameAbilityBoostStack()
        {
            if (m_perkData.isOneTimePerReset && m_hasUsedEvent)
            {
                return;
            }
            
            //Do Action
        }

        //Reset
        public void ResetPerk()
        {
            m_hasUsedEvent = false;
        }
        
        
    }
}