using UnityEngine;

namespace Data.StatusDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Status/Damage Over Time Status Data")]
    public class AffectHealthOverTimeStatusData: StatusData
    {
        
        [Header("Status Specific")]
        public float amountOfTimePerTick = 1f;
        public float amountChangePerTick = 1f;

        public bool isHealing;

    }
}