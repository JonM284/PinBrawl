using Runtime.GameControllers;
using Runtime.ScriptedAnimations;
using Runtime.ScriptedAnimations.Transform;
using TMPro;
using UnityEngine;

namespace Runtime.UI.Items
{
    public class DamageTextUIItem: MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField] private RelativeTransformPositionAnimation textAnimation;

        [SerializeField] private AnimationsBase fadeAnimation;

        [SerializeField] private TMP_Text damageText;

        [SerializeField] private int criticalDamageThreshold = 20;

        [SerializeField] private Color damageColor;

        [SerializeField] private Color healingColor;

        #endregion

        #region Class Implementation

        public void Initialize(int _damageAmount)
        {
            string _symbol = "";
            if (_damageAmount < 0)
            {
                _symbol = "+";
                damageText.color = healingColor;
            }else if (_damageAmount > criticalDamageThreshold)
            {
                _symbol = "-";
                damageText.color = damageColor;
            }
            
            damageText.text = $"{_symbol}{_damageAmount}";
            textAnimation.Initialize();
            fadeAnimation.Play();
        }

        public void Close()
        {
            JuiceGameController.Instance.CacheDamageText(this);
        }

        #endregion

    }
}