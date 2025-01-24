using Runtime.Abilities;
using Runtime.Gameplay;
using UnityEngine;

namespace Data.AbilityDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Ability/Dash Ability Data")]
    public class DashAbilityData: AbilityData
    {

        [Header("Dash Specific")] 
        
        public DashReturnPointEntity previousLeftPoint;

        public MovementType movementType;

        public AnimationCurve jumpCurve;
        
        public float dashSpeed = 1;


    }
}