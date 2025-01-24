using UnityEngine;

namespace Data.StatusDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Status/Speed Change Status Data")]
    public class SpeedChangeStatusData: StatusData
    {
        [Header("Status Specific")] 
        [Tooltip("Speed will be changed to this much, for example 0.8 = 80% current speed, 2.0 = 2x current speed")]
        [Range(0f, 3f)]
        public float speedChangeModifier = 1f;
    }
}