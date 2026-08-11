using Runtime.Character;

namespace Runtime.Statuses
{
    public class SilenceStatus: StatusEntityBase
    {
        
        #region StatusEntityBase Inherited Methods

        public override void OnApply(BaseCharacter _baseCharacter)
        {
            base.OnApply(_baseCharacter);
            currentOwner.SetSilenceStatus(false);
        }

        public override void OnEnd()
        {
            base.OnEnd();
            currentOwner.SetSilenceStatus(true);
        }

        #endregion
    }
}