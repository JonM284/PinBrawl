using Data.PerkDatas;
using Runtime.Character;

namespace Runtime.Perks
{
    public interface IPerk
    {
        public BaseCharacter currentOwner { get; set; }

        public bool isInitialized { get; set; }

        public abstract void OnApply(BaseCharacter _baseCharacter, PerkDataBase _perkData);

        public abstract void OnUpdate();
        
        public abstract string GetGUID();
    }
}