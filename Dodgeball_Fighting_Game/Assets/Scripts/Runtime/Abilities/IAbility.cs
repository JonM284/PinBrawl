using System.Threading;
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

        public virtual async UniTask InitializeAbilityAsync(BaseCharacter _owner, AbilityData _data, bool _canUseOnStart, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
        }

        public virtual async UniTask PreLoadNecessaryObjectsAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
        }
        
        //Definitely 
        public virtual async UniTask DoAbilityAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
        }

        public abstract void ResetAbilityUse();
    }
}