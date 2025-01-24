using System.Collections.Generic;
using System.Linq;
using Data.AbilityDatas;
using Data.PerkDatas;
using Runtime.Abilities;
using Runtime.Character;

namespace Runtime.Perks
{
    public class PerkUpgradeAbility: PerkEntityBase
    {

        #region Private Fields

        private List<AbilityBase> m_upgradedAbility = new List<AbilityBase>();

        private List<AbilityBase> m_currentlyUpgradingAbilities = new List<AbilityBase>();

        #endregion
        
        #region Accessors

        public AbilityChangePerkData m_abilityChangePerkData => currentPerkData as AbilityChangePerkData;

        #endregion
        
        public override void OnApply(BaseCharacter _baseCharacter, PerkDataBase _perkData)
        {
            base.OnApply(_baseCharacter, _perkData);

            if (m_abilityChangePerkData.isAllAbilityTypes)
            {
                UpgradeAllAbilities();
            }
            else
            {
                foreach (var _category in m_abilityChangePerkData.abilityCategoryList)
                {
                    UpgradeAbilitiesOfType(_category);
                }
            }
            
        }
        
        
        private void UpgradeAbilitiesOfType(AbilityCategories _category)
        {

            m_currentlyUpgradingAbilities =
                currentOwner.GetAllAbilities().Where(ab => ab.ContainsCategory(_category)).ToList();

            if (m_currentlyUpgradingAbilities.Count == 0)
            {
                return;
            }

            foreach (var _ability in m_currentlyUpgradingAbilities)
            {

                if (m_upgradedAbility.Count > 0 && m_upgradedAbility.Contains(_ability))
                {
                    continue;
                }
                
                //Cooldown, Knockback, Damage, Scale, lifeTime, speed, range
                object[] arg = {
                    m_abilityChangePerkData.cooldownChangeAmount,
                    m_abilityChangePerkData.hitChangeAmount,
                    m_abilityChangePerkData.scaleChangeAmount,
                    m_abilityChangePerkData.lifeTimeChangeAmount,
                    m_abilityChangePerkData.speedChangeAmount,
                    m_abilityChangePerkData.rangeChangeAmount
                };
                
                _ability.UpdateAbility(arg);
                m_upgradedAbility.Add(_ability);
            }


        }
        
        private void UpgradeAllAbilities()
        {

            foreach (var _ability in currentOwner.GetAllAbilities())
            {
                object[] arg = {
                    m_abilityChangePerkData.cooldownChangeAmount,
                    m_abilityChangePerkData.hitChangeAmount,
                    m_abilityChangePerkData.scaleChangeAmount,
                    m_abilityChangePerkData.lifeTimeChangeAmount,
                    m_abilityChangePerkData.speedChangeAmount,
                    m_abilityChangePerkData.rangeChangeAmount
                };
                
                _ability.UpdateAbility(arg);
                m_upgradedAbility.Add(_ability);
            }
            
        }
        
        
    }
}