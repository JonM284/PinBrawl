using GameControllers;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using UnityEngine;

namespace Runtime.Gameplay
{
    public class OrbChargeAbility: OrbBase
    {
        protected override void DoInteraction(BaseCharacter _character)
        {
            if (_character.IsNull())
            {
                return;
            }
            
            _character.AddAbilityCharge();
            
            VFXController.Instance.PlayAt(endVFX, transform.position, Quaternion.identity);
            ObjectPoolController.Instance.ReturnToPool(m_poolIdentifier, gameObject);
        }
    }
}