using Runtime.Perks;
using UnityEngine;

namespace Data.PerkDatas
{
    
    [CreateAssetMenu(menuName = "Dodge-ball/Perk/Event Triggered Perk")]

    public class EventTriggeredPerkData: PerkDataBase
    {
        [Header("Event Triggered Specific")]
        public EventListenState listenState;

        public bool isOneTimePerReset;
        public bool isStackable;

        public GameObject creatableObject;
    }
}