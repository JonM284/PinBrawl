using Runtime.Abilities;
using UnityEngine;

namespace Data.AbilityDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Ability/Melee Ability Data")]
    public class MeleeAbilityData: AbilityData
    {
        [Header("Melee Specific")] 
        
        [Tooltip("Melee is a sphere, is there a check between the player and end position ?")]
        public MeleeType meleeType;

    }
}