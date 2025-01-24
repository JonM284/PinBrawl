using Data.PerkDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using UnityEngine;

namespace Runtime.Perks
{
    public class PerkEntityBase: MonoBehaviour, IPerk
    {
        
        public BaseCharacter currentOwner { get; set; }
        
        public bool isInitialized { get; set; }

        public PerkDataBase currentPerkData { get; protected set; }

        public virtual void OnApply(BaseCharacter _baseCharacter, PerkDataBase _perkData)
        {
            if (_baseCharacter.IsNull() || _perkData.IsNull())
            {
                return;
            }

            currentOwner = _baseCharacter;
            currentPerkData = _perkData;
        }

        public virtual void OnUpdate() { }

        public virtual void OnReset(){}

        public string GetGUID()
        {
            return currentPerkData.perkIdentifierGUID;
        }
    }
}