using System;
using System.Collections.Generic;
using Data.AbilityDatas;
using Runtime.Character;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Data
{
    [Serializable] 
    [CreateAssetMenu(menuName = "Dodge-ball/Character/Character Data")]
    public class CharacterData: ScriptableObject
    {

        [Header("Base Character Stats")] 
        public string characterName = "Name";

        public int characterArmorAmount = 1000;

        public string characterIdentifierGUID;

        public float characterNaturalKnockbackResistance = 0.15f; 
            
        public float characterWalkSpeed = 7f;

        public float meleeChargeWalkSpeed = 4f;

        public float characterColliderRadius = 1f;

        public float ballMeleeColliderRadius = 2f;

        public float ballMeleeCooldownTimer = 1f;

        public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);

        public LayerMask ballMeleeLayerMask;
        
        public GameObject characterModelRef;
        public Sprite characterIconRef;

        [Header("Abilities: Max = 3")] 
        [Tooltip("Max Amount = 3")]
        public List<AbilityData> allCharacterAbilities = new List<AbilityData>();


        [ContextMenu("Make Identifier")]
        public void CreateGUID()
        {
            characterIdentifierGUID = System.Guid.NewGuid().ToString();
        }
        
    }
}