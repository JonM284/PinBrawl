using Data.PerkDatas;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.Gameplay;

namespace Runtime.Perks
{
    public class PerkScriptableEventTriggered : GameEventListener, IPerk
    {
        #region IPerk Inherited Methods

        public BaseCharacter currentOwner { get; set; }
        
        public bool isInitialized { get; set; }

        public PerkDataBase currentPerkData { get; protected set; }

        public void OnApply(BaseCharacter _baseCharacter, PerkDataBase _perkData)
        {
            if (_baseCharacter.IsNull() || _perkData.IsNull())
            {
                return;
            }

            currentOwner = _baseCharacter;
            currentPerkData = _perkData;
        }

        public virtual void OnUpdate() { }

        public string GetGUID()
        {
            return currentPerkData.perkIdentifierGUID;
        }

        #endregion

        #region Class Implementation

        public void Test()
        {
            
        }
        
        #endregion
        
    }
}