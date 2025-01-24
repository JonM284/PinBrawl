using Cysharp.Threading.Tasks;
using Data.AbilityDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using UnityEngine;

namespace Runtime.Abilities
{
    public interface IAbility
    {

        public bool canUseAbility { get; set; }

        public float abilityCooldownCurrent { get; set; }

        public float abilityCooldownMax { get; set; }

        public Vector3 aimDirection { get; set; }

        public BaseCharacter currentOwner { get; set; }

        public abstract void InitializeAbility(BaseCharacter _owner, AbilityData _data);

        public abstract UniTask PreLoadNecessaryObjects();
        
        //Definitely 
        public abstract UniTask DoAbility();

        public abstract void ResetAbilityUse();
    }
}