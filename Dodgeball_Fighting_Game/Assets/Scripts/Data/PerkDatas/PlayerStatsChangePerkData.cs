using UnityEngine;

namespace Data.PerkDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Perk/Player Stats Change Data")]
    public class PlayerStatsChangePerkData: PerkDataBase
    {
        [Range(-1.0f, 1.0f)]
        public float baseSpeedChangeAmount;
        [Range(-1.0f, 1.0f)]
        public float baseArmorChangeAmount;
        [Range(-1.0f, 1.0f)]
        public float baseSizeChangeAmount;
        
        public override string GetFormatDescription()
        {
            if(!string.IsNullOrEmpty(perkDescription))
            {
                return perkDescription;
            }
            
            if (baseSpeedChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(baseSpeedChangeAmount, "More", "Less");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(baseSpeedChangeAmount);
                perkDescription += $"{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}>Move SPEED\r\n";
            }

            if (baseArmorChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(baseArmorChangeAmount, "More", "Less");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(baseArmorChangeAmount);
                perkDescription += $"{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}>Player ARMOR\r\n";
            }

            if (baseSizeChangeAmount != 0)
            {
                m_pos_or_neg = GetPosNegWithArgs(baseSizeChangeAmount, "More", "Less");
                m_IncreaseDecreaseSpriteName = GetIncreaseDecreaseSpriteName(baseSizeChangeAmount);
                perkDescription += $"{m_pos_or_neg}<sprite name={m_IncreaseDecreaseSpriteName}>WACK SIZE\r\n";
            }
            
            return perkDescription;
        }
        
        public override string GetPerkTypeString()
        {
            return "<color=#00FF00>STATS</color>";
        }
        
    }
}