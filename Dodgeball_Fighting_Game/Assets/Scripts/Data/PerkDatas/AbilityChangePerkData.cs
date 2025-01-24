using System.Collections.Generic;
using System.Linq;
using Data.AbilityDatas;
using UnityEngine;

namespace Data.PerkDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Perk/Ability Change Data")]
    public class AbilityChangePerkData: PerkDataBase
    {
        //Example: object[] arg = {Cooldown, Damage, Knockback, Scale};
        //Projectile -> MaxLifetime, Speed
        //Dash -> Offset

        public bool isAllAbilityTypes;
        public List<AbilityCategories> abilityCategoryList = new List<AbilityCategories>();

        [Range(-1.0f, 1.0f)]
        public float cooldownChangeAmount = 0;
        [Range(-1.0f, 1.0f)]
        public float hitChangeAmount = 0;
        [Range(-1.0f, 1.0f)]
        public float scaleChangeAmount = 0;
        [Range(-1.0f, 1.0f)]
        public float lifeTimeChangeAmount = 0;
        [Range(-1.0f, 1.0f)]
        public float speedChangeAmount = 0;
        [Range(-1.0f, 1.0f)]
        public float rangeChangeAmount = 0;

        private string m_affectedCategories;
        
        public override string GetFormatDescription()
        {
            if(!string.IsNullOrEmpty(perkDescription))
            {
                return perkDescription;
            }
            
            perkDescription = "<u>Affected Abilities:</u> \r\n";
            m_affectedCategories = "";

            if (isAllAbilityTypes)
            {
                m_affectedCategories = "ALL\r\n";
            }
            else
            {
                if (abilityCategoryList.Count == 1)
                {
                    m_affectedCategories = $"{abilityCategoryList.FirstOrDefault().abilityCategoryName}\r\n";
                }
                else
                {
                    for (int i = 0; i < abilityCategoryList.Count; i++)
                    {
                        if (i == abilityCategoryList.Count - 1)
                        {
                            m_affectedCategories += "and ";
                        }
                        
                        m_affectedCategories += $"{abilityCategoryList[i].abilityCategoryName}";

                        if (i < abilityCategoryList.Count - 1)
                        {
                            m_affectedCategories += ", ";
                        }
                        else
                        {
                            m_affectedCategories += " \r\n";
                        }
                        
                    }
                }
            }

            perkDescription += m_affectedCategories;
            
            if (cooldownChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(cooldownChangeAmount, "Faster", "Slower");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(cooldownChangeAmount);
                perkDescription += $"{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}>cooldown<sprite name=Rate@Icon> \r\n";
            }

            if (hitChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(hitChangeAmount, "Harder", "Weaker");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(hitChangeAmount);
                perkDescription += $"hit<sprite name=Hit@Icon>{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}> \r\n";
            }
            
            if (scaleChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(scaleChangeAmount, "Bigger", "Smaller");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(scaleChangeAmount);
                perkDescription += $"{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}>Size<sprite name=Size@Icon> \r\n";
            }
            
            if (lifeTimeChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(lifeTimeChangeAmount, "Longer", "Shorter");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(lifeTimeChangeAmount);
                perkDescription += $"{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}>Duration<sprite name=Duration@Icon>\r\n";
            }
            
            if (speedChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(speedChangeAmount, "Faster", "Slower");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(speedChangeAmount);
                perkDescription += $"{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}>Attack Speed<sprite name=Speed@Icon>\r\n";
            }
            
            if (rangeChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(rangeChangeAmount, "More", "Less");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(rangeChangeAmount);
                perkDescription += $"{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}>Range<sprite name=Range@Icon>\r\n";
            }
            
            return perkDescription;
        }
        
        public override string GetPerkTypeString()
        {
            return "<color=#FFA932>ABILITY</color>";
        }
        
    }
}