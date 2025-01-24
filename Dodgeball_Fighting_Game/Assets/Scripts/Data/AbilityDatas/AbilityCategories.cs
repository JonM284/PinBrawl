using UnityEngine;

namespace Data.AbilityDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Ability/Ability Category Data")]
    public class AbilityCategories: ScriptableObject
    {
        public string abilityCategoryName;
        public string abilityCategoryGUID;
        public Sprite abilityIcon;
        
        [ContextMenu("Make Identifier")]
        public void CreateGUID()
        {
            abilityCategoryGUID = System.Guid.NewGuid().ToString();
        }
        
    }
}