using Data.StatusDatas;
using Runtime.Character;

namespace Runtime.Statuses
{
    public class DamageIntakeChangeStatus: StatusEntityBase
    {

        #region Accessors

        protected DamageIntakeChangeStatusData m_damageIntakeModStatusData => m_statusData as DamageIntakeChangeStatusData;

        #endregion
        
        
        #region StatusEntityBase Inherited Methods

        public override void OnApply(BaseCharacter _baseCharacter)
        {
            base.OnApply(_baseCharacter);
            currentOwner.ChangeDamageIntakeMod(m_damageIntakeModStatusData.damageIntakeModifier);
        }

        public override void OnEnd()
        {
            base.OnEnd();
            currentOwner.ResetDamageIntakeMod();
        }

        #endregion
        
        
        
        
    }
}