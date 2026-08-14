using System.Collections.Generic;
using Data.StatusDatas;
using Runtime.Abilities;
using Runtime.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

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

        public bool isUltimateAbility;
        
        [Tooltip("Stop Character Movement while performing?")]
        public bool isHaltMovement = true;

        public ActivationType activationType = ActivationType.OnRelease;

        [Tooltip("Full amount of time required to charge or wait until ability deals max stats")]
        public float abilityActivationWaitTimeMax = 1f;

        public float reactivationTime = 1f;

        public KnockbackDirectionType KnockbackDirectionType;

        [FormerlySerializedAs("ballHitStrength")] public HitStrengthType ballHitStrengthType;

        public LayerMask collisionDetectionLayers;

        public List<AbilityCategories> abilityCategories = new List<AbilityCategories>();

        [Header("Status'")]
        public List<StatusData> applicableStatusesOnHit = new List<StatusData>();
        
        [Header("Reference")]
        public GameObject abilityGameObject;
    }
}