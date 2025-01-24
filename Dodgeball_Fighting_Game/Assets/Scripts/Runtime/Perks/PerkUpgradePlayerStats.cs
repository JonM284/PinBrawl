using Data.PerkDatas;
using Project.Scripts.Utils;
using Runtime.Character;

namespace Runtime.Perks
{
    public class PerkUpgradePlayerStats: PerkEntityBase
    {

        #region Private Fields

        private bool m_hasTriggered;

        #endregion
        
        #region Accessors

        public PlayerStatsChangePerkData m_playerStatsChangeData => currentPerkData as PlayerStatsChangePerkData;

        #endregion

        #region PerkEntityBase Inherited Methods

        
        public override void OnApply(BaseCharacter _baseCharacter, PerkDataBase _perkData)
        {
            base.OnApply(_baseCharacter, _perkData);
            ApplyToCharacter();
        }

        #endregion


        #region Class Implementation

        private void ApplyToCharacter()
        {
            if(currentOwner.IsNull() || m_playerStatsChangeData.IsNull())
            {
                return;
            }
            
            object[] args = {
                m_playerStatsChangeData.baseSpeedChangeAmount,
                m_playerStatsChangeData.baseArmorChangeAmount,
                m_playerStatsChangeData.baseSizeChangeAmount
            };

            currentOwner.ChangeCharacterStatsValues(args);
        }

        #endregion
        
        
    }
}