using Runtime.Character;

namespace Runtime.Statuses
{
    public class WackSilenceStatus: StatusEntityBase
    {
        #region StatusEntityBase Inherited Methods

        public override void OnApply(BaseCharacter _baseCharacter)
        {
            base.OnApply(_baseCharacter);
            currentOwner.SilenceWack(false);
        }

        public override void OnEnd()
        {
            base.OnEnd();
            currentOwner.SilenceWack(true);
        }

        #endregion
    }
}