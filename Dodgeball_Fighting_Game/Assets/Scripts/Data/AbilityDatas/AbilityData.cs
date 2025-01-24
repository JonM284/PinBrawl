using System.Collections.Generic;
using Data.StatusDatas;
using Runtime.Gameplay;
using UnityEngine;

namespace Data.AbilityDatas
{
    public class AbilityData: ScriptableObject
    {
        [Header("Visuals / Description")] 
        public string abilityName = "==== Ability Name ====";
        public string abilityDescription = "//// Ability Description ////";
        public Sprite abilityIconRef;
        
        [Header("Gameplay")]
        public float abilityCooldownTimeMax = 1f;
        public float abilityKnockbackAmount = 1f;
        public float abilityDamageAmount = 1;
        public float abilityRange = 0;
        public float abilityScale = 1;

        [Tooltip("Amount of time before character goes back to running animation")]
        public float abilityAnimationTime = 0.15f;

        [Tooltip("Stop Character Movement while performing?")]
        public bool isHaultMovement = true;

        [Tooltip("Can hit enemy player only 1 time or multiple times. IE: dash attack")]
        public bool isHitOnce;

        public bool isReactivatable;

        public float reactivationTime = 1f;

        public float knockbackDirectionMod = 1f;

        public HitStrength ballHitStrength;

        public LayerMask collisionDetectionLayers;

        public List<AbilityCategories> abilityCategories = new List<AbilityCategories>();

        [Header("Status'")]
        public List<StatusData> applicableStatusesOnHit = new List<StatusData>();
        
        [Header("Reference")]
        public GameObject abilityGameObject;
    }
}