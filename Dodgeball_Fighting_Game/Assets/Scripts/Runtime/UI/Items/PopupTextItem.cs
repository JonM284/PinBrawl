using System;
using Project.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime.UI.Items
{
    public class PopupTextItem: MonoBehaviour
    {

        #region Actions

        private Action<PopupTextItem> OnEndTextDisplay;

        #endregion
        
        #region Public Fields

        public UnityEvent onStartTextDisplay;

        #endregion

        #region Serialied Fields

        [SerializeField] private TMP_Text _text;

        #endregion
        
        #region Private Fields

        private RectTransform m_uiRectTransform;

        #endregion

        #region Accessors

        public RectTransform uiRectTransform => CommonUtils.GetRequiredComponent(ref m_uiRectTransform, () =>
        {
            var rt = GetComponent<RectTransform>();
            return rt;
        });

        #endregion

        #region Class Implementation

        public void SetupText(string displayString, Color _desiredColor, Action<PopupTextItem> _endAction)
        {
            _text.text = displayString;
            _text.color = _desiredColor;

            onStartTextDisplay?.Invoke();

            if (!_endAction.IsNull())
            {
                OnEndTextDisplay = _endAction;
            }
        }

        public void EndText()
        {
            OnEndTextDisplay?.Invoke(this);
        }

        #endregion



    }
}