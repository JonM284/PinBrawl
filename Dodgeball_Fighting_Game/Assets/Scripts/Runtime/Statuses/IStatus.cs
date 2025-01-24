using Runtime.Character;

namespace Runtime.Statuses
{
    public interface IStatus
    {
        public float statusTimeMax { get; set; }

        public float statusTimeCurrent { get; set; }

        public BaseCharacter currentOwner { get; set; }

        public bool isInitialized { get; set; }

        public abstract void OnApply(BaseCharacter _baseCharacter);

        public abstract void OnTick();

        public abstract void OnEnd();

        public abstract string GetGUID();
    }
}