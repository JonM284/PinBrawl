using UnityEngine;

namespace Data.AbilityDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Ability/Hook Ability Data")]
    public class HookAbilityData: AbilityData
    {
        [Header("Hook Specific")] 
        public float hookTravelTime = 0.5f;
        public float hookedPlayerReturnTime = 0.5f;

        public bool bringHookedPlayerBack;
    }
}