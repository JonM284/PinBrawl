using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Data.PerkDatas;
using Project.Scripts.Utils;
using Runtime.Perks;
using Runtime.ScriptedAnimations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Items
{
    public class PerkChipsUIItem: MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField] private TMP_Text m_perkName;
        [SerializeField] private TMP_Text m_perkDescription;
        [SerializeField] private TMP_Text m_perkType;
        [SerializeField] private Image m_perkIcon;
        [SerializeField] private Image m_perkTypeIcon;
        [SerializeField] private Image m_perkImageBackground;
        
        [SerializeField] private GameObject m_inactiveImage;
        
        [SerializeField] private List<PerkColorByRarity> m_allColors = new List<PerkColorByRarity>();
        
        [SerializeField] private AnimationsBase m_hoverAnimation;

        #endregion

        #region Private Fields

        private PerkColorByRarity m_assignedColorByRarity;

        #endregion

        #region Accessors

        public PerkDataBase assignedPerk { get; private set; }

        #endregion
        
        public async UniTask SetupPerk(PerkDataBase _perkData)
        {
            if (_perkData.IsNull())
            {
                gameObject.SetActive(false);
                return;
            }

            assignedPerk = _perkData;
            m_perkName.text = assignedPerk.perkName;
            m_perkDescription.text = assignedPerk.GetFormatDescription();
            m_perkType.text = assignedPerk.GetPerkTypeString();
            m_perkIcon.sprite = assignedPerk.perkIconRef;

            SetBackgroundColor();
        }

        private void SetBackgroundColor()
        {
            if (m_allColors.Count == 0)
            {
                return;
            }
            
            m_assignedColorByRarity = m_allColors.FirstOrDefault(pcbr => pcbr.PerkRarity == assignedPerk.perkRarity);

            if (m_assignedColorByRarity.IsNull())
            {
                return;
            }
            
            m_perkImageBackground.color = m_assignedColorByRarity.perkColor;
        }

        public void SetActiveHighlight(bool _isActive)
        {
            m_inactiveImage.SetActive(!_isActive);

            if (_isActive)
            {
                m_hoverAnimation.Play();
            }
            else
            {
                m_hoverAnimation.PlayReverse();
            }
            
        }
        
    }
}