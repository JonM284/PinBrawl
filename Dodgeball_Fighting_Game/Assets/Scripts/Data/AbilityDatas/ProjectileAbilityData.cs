using Runtime.Abilities;
using Runtime.Gameplay;
using UnityEngine;

namespace Data.AbilityDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Ability/Projectile Ability Data")]
    public class ProjectileAbilityData: AbilityData
    {

        [Header("Projectile Specific")] 
        public float projectileMaxLifetime = 1f;
        public float projectileShootSpeed = 1f;
        public float projectileDetectionRange = 1f;
        public float projectileExplosionRange = 1f;

        [Header("On Projectile End")] 
        public ProjectileEndType projectileEndType;
        
        [Header("Booleans")]
        public bool isPassThroughObjects;
        public bool isMultiStage;
        public bool isSpreadShotOnStart;
        
        [Header("Multi-shot options")]
        [Tooltip("Amount of projectiles to be fired")]
        public int projectileAmount = 1;

        [Range(0, 360)]
        public float projectileSpread = 15f;
        
        public float timeBetweenShots = 0.25f;

        [Header("Multi-stage options")] 
        public int amountOfStages = 0;
        
        [Header("Slow Down")]
        public bool isSlowDownOverTime;
        public float slowDownModifier;
        public SlowDownType slowDownType;
       
        [Space]
        //ToDo: Change to Addressable System + PRELOAD when going into a match
        public GameObject projeciltePrefab;

        [Header("End Creatable")] 
        public GameObject endCreatable;

    }
}