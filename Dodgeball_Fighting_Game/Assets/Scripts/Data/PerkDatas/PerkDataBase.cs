using Runtime.Perks;
using UnityEngine;

namespace Data.PerkDatas
{
    [CreateAssetMenu(menuName = "Dodge-ball/Perk/Perk Data")]
    public class PerkDataBase: ScriptableObject
    {
        
        [Header("Visuals / Description")] 
        public string perkName = "==== Perk Name ====";
        public string perkDescription;
        public Sprite perkIconRef;

        public PerkRarity perkRarity;
        public int rarityWeight;
        
        public string perkIdentifierGUID;

        protected readonly string m_increaseSprite = "More{0}@Icon";
        protected readonly string m_decreaseSprite = "Less{0}@Icon";

        protected readonly string m_positiveColorHex = "#BDD283";
        protected readonly string m_negativeColorHex = "#E57673";


        protected string m_pos_or_neg = "";
        protected string m_IncreaseDecreaseSpriteName = "";

        protected float m_slightUpperThresh = 0.1f;
        protected float m_midUpperThresh = 0.25f;
        protected float m_highUpperThresh = 0.5f;

        protected float cachedCheckFloat;

        [Header("Reference")] public GameObject perkGameObject;
        
        [ContextMenu("Make Identifier")]
        public void CreateGUID()
        {
            perkIdentifierGUID = System.Guid.NewGuid().ToString();
        }
        
        [ContextMenu("Assign Random Rarity")]
        public void CreateRandomRarity()
        {
            rarityWeight = Random.Range(1, 100);
        }

        public virtual string GetFormatDescription()
        {
            return perkDescription;
        }

        protected string GetModifierWord(float _modifierAmount)
        {
            if (_modifierAmount < m_slightUpperThresh)
            {
                return "Slightly ";
            }
            
            if (_modifierAmount >= m_slightUpperThresh && _modifierAmount < m_midUpperThresh)
            {
                return "";
            }
            
            if (_modifierAmount >= m_midUpperThresh && _modifierAmount < m_highUpperThresh)
            {
                return "A lot ";
            }

            return "Loads ";
        }

        public int GetCorrectLevel(float _modifierAmount)
        {
            if (_modifierAmount < m_slightUpperThresh)
            {
                return 1;
            }
            
            if (_modifierAmount >= m_slightUpperThresh && _modifierAmount < m_midUpperThresh)
            {
                return 2;
            }
            
            if (_modifierAmount >= m_midUpperThresh && _modifierAmount < m_highUpperThresh)
            {
                return 3;
            }

            return 4;
        }

        public string GetIncreaseDecreaseSpriteName(float _amount)
        {
            cachedCheckFloat = Mathf.Abs(_amount);
            return _amount > 0 ?
                string.Format(m_increaseSprite, GetCorrectLevel(cachedCheckFloat)) 
                : string.Format(m_decreaseSprite, GetCorrectLevel(cachedCheckFloat));
        }

        public string GetPosNegWithArgs(float _amount, string _moreArg, string _lessArg)
        {
            cachedCheckFloat = Mathf.Abs(_amount);
            return _amount > 0 ? 
                $"<color={m_positiveColorHex}>{GetModifierWord(cachedCheckFloat)}{_moreArg}</color>" 
                : $"<color={m_negativeColorHex}>{GetModifierWord(cachedCheckFloat)}{_lessArg}</color>";
        }

        public virtual string GetPerkTypeString()
        {
            return "";
        }

        [ContextMenu("Clear Description")]
        public void ClearDescription()
        {
            perkDescription = "";
        }
        
    }
}