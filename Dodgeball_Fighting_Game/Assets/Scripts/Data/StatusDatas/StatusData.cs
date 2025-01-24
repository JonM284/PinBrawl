using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Data.StatusDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Status/Status Data")]
    public class StatusData: ScriptableObject
    {

        [Header("Visuals / Description")] 
        public string statusName = "==== Ability Name ====";
        public string statusDescription = "//// Ability Description ////";
        public Sprite statusIconRef;

        public bool isBuff;
        
        public string statusIdentifierGUID;
        
        [Header("Gameplay")]
        public float timeMax;
        public float shieldChangeOverTick;

        [Header("Reference")] public GameObject statusGameObject;

        
        [ContextMenu("Make Identifier")]
        public void CreateGUID()
        {
            statusIdentifierGUID = System.Guid.NewGuid().ToString();
        }
        
    }
}