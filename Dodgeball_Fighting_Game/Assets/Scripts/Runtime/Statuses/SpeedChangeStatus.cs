using Data.StatusDatas;
using Project.Scripts.Utils;
using Runtime.Character;

namespace Runtime.Statuses
{
    public class SpeedChangeStatus: StatusEntityBase
    {

        #region Accessors

        protected SpeedChangeStatusData m_speedChangeStatusData => m_statusData as SpeedChangeStatusData;

        #endregion

        #region StatusEntityBase Inherited Methods

        public override void OnApply(BaseCharacter _baseCharacter)
        {
            base.OnApply(_baseCharacter);

            if (_baseCharacter.IsNull())
            {
                return;
            }
            
            currentOwner.ChangeCurrentCharacterMovementSpeed(m_speedChangeStatusData.speedChangeModifier);
        }

        public override void OnEnd()
        {
            base.OnEnd();
            
            currentOwner.ResetCharacterMovementSpeed();
        }

        #endregion
        
        
        
    }
}