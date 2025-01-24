using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using Project.Scripts.Utils;
using Runtime.UI;
using Runtime.UI.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Runtime.GameControllers
{
    public class UIController: GameControllerBase
    {

        #region Static

        public static UIController Instance { get; private set; }

        #endregion

        #region Nested Classes

        [Serializable]
        public class CanvasByLayer
        {
            public UILayerData layer;
            public Canvas associatedCanvas;
        }
        
        [Serializable]
        public class ModalsByLayer
        {
            public UILayerData layer;
            public AssetReference modalAssetReference;
        }

        #endregion

        #region Public Fields

        public UnityEvent onBeginFadeIn;

        public UnityEvent onBeginFadeOut;

        #endregion
        
        #region Serialize Fields

        [SerializeField] private ModalsByLayer popupAssetReference;

        [SerializeField] private List<CanvasByLayer> canvasByLayers = new List<CanvasByLayer>();

        [SerializeField] private AssetReference textAssetRef;
        
        #endregion

        #region Private Fields

        private List<UIBase> m_activeUIWindows = new List<UIBase>();

        private List<UIBase> m_cachedUIWindows = new List<UIBase>();

        private List<UIPopupDialog> m_cachedPopups = new List<UIPopupDialog>();

        private List<UIPopupDialog> m_activePopups = new List<UIPopupDialog>();

        private Transform m_cachedUIPoolTransform;

        private Transform m_cachedPopupTextPool;

        private List<PopupTextItem> m_cachedPopupTexts = new List<PopupTextItem>();

        #endregion

        #region Accessors

        public Transform cachedUIPool =>
            CommonUtils.GetRequiredComponent(ref m_cachedUIPoolTransform, ()=>
            {
                var poolTransform = TransformUtils.CreatePool(this.transform, false);
                return poolTransform;
            });
        
        public Transform cachedPopupTextPool =>
            CommonUtils.GetRequiredComponent(ref m_cachedPopupTextPool, ()=>
            {
                var poolTransform = TransformUtils.CreatePool(this.transform, false);
                poolTransform.RenameTransform("Text Pool");
                return poolTransform;
            });

        #endregion

        #region GameControllerBase Inherited Methods

        public override void Initialize()
        {
            if (!Instance.IsNull())
            {
                return;
            }
            
            Instance = this;
            base.Initialize();
        }

        #endregion

        #region Class Implementation

        public void FadeBlackScreen(bool _fadeIn)
        {
            if (_fadeIn)
            {
                onBeginFadeIn?.Invoke();
            }
            else
            {
                onBeginFadeOut?.Invoke();
            }
        }

        public Transform GetParentCanvasByLayer(UILayerData _layer)
        {
            if (_layer == null)
            {
                return default;
            }

            var _foundCanvasByLayer = canvasByLayers.FirstOrDefault(cbl => cbl.layer == _layer);
            
            return _foundCanvasByLayer.associatedCanvas.transform;
        }

        public void AddUI(UILayerData _layer, GameObject _uiWindow)
        {
            if (_layer == null)
            {
                return;
            }

            var _foundCanvasByLayer = canvasByLayers.FirstOrDefault(cbl => cbl.layer == _layer);
            
            Instantiate(_uiWindow, _foundCanvasByLayer.associatedCanvas.transform);
        }

        public void AddUI(UIWindowData _uiWindowData)
        {
            if (_uiWindowData.IsNull())
            {
                Debug.LogError($"UI window data null");
                return;
            }
            
            Debug.Log($"{_uiWindowData.layerData.name}");
            
            var _cachedWindow = m_cachedUIWindows.FirstOrDefault(ui => ui.uiWindowData == _uiWindowData);
            var _foundCanvasByLayer = canvasByLayers.FirstOrDefault(cbl => cbl.layer.guid == _uiWindowData.layerData.guid);
            
            if (!_cachedWindow.IsNull())
            {
                m_cachedUIWindows.Remove(_cachedWindow);
                _cachedWindow.uiRectTransform.ResetTransform(_foundCanvasByLayer.associatedCanvas.transform);
                m_activeUIWindows.Add(_cachedWindow);
                Debug.Log("Found Window");
                return;
            }

            if (_uiWindowData.uiWindowAssetReference.IsNull())
            {
                Debug.LogError("Asset Reference NULL");
            }

            if (_foundCanvasByLayer.IsNull())
            {
                Debug.LogError("Found canvas null");
            }

            if (_foundCanvasByLayer.associatedCanvas.IsNull())
            {
                Debug.LogError("Associated Canvas null");
            }
            
            Instantiate(_uiWindowData.uiWindowAssetReference, _foundCanvasByLayer.associatedCanvas.transform);
        }

        public void AddUICallback(UIWindowData _uiWindowData, Action<GameObject> _callback)
        {
            StartCoroutine(C_AddUICallback(_uiWindowData, _callback));
        }

        private IEnumerator C_AddUICallback(UIWindowData _uiWindowData, Action<GameObject> _callback)
        {
            var _cachedWindow = m_cachedUIWindows.FirstOrDefault(ui => ui.uiWindowData == _uiWindowData);
            var _foundCanvasByLayer = canvasByLayers.FirstOrDefault(cbl => cbl.layer == _uiWindowData.layerData);
            
            if (_cachedWindow != null)
            {
                m_cachedUIWindows.Remove(_cachedWindow);
                _cachedWindow.uiRectTransform.ResetTransform(_foundCanvasByLayer.associatedCanvas.transform);
                m_activeUIWindows.Add(_cachedWindow);
                Debug.Log("Found Window");
                if (!_callback.IsNull())
                {
                    _callback?.Invoke(_cachedWindow.gameObject);
                }
                yield break;
            }
            
            
            //yield return StartCoroutine(
              //  AddressableController.Instance.C_LoadGameObject(_uiWindowData.uiWindowAssetReference, _callback, _foundCanvasByLayer.associatedCanvas.transform));
        }

        public void ReturnUIToCachedPool(UIBase _uiWindow)
        {
            if (_uiWindow == null)
            {
                return;
            }

            if (_uiWindow is UIPopupDialog popup)
            {
                if (m_activePopups.Contains(popup))
                {
                    m_activePopups.Remove(popup);
                }
                m_cachedPopups.Add(popup);
                popup.uiRectTransform.ResetTransform(cachedUIPool);
                return;
            }
            
            m_cachedUIWindows.Add(_uiWindow);
            _uiWindow.uiRectTransform.ResetTransform(cachedUIPool);
        }

        public void CreatePopup(PopupDialogData _data, Action _confirmAction, Action _closeAction = null)
        {
            var foundPopup = GetCachedUIPopup(_data);
            var _layer = popupAssetReference.layer;
            var _foundCanvasByLayer = canvasByLayers.FirstOrDefault(cbl => cbl.layer == _layer);
            
            //If the popup already exists, use same popup
            if (foundPopup != null)
            {
                m_cachedPopups.Remove(foundPopup);
                m_activePopups.Add(foundPopup);
                foundPopup.uiRectTransform.ResetTransform(_foundCanvasByLayer.associatedCanvas.transform);
                Debug.Log("Found Popup");
                return;
            }
            
            //Otherwise create a new popup
            object[] arg = {_data, _confirmAction, _closeAction};

            var handle = Addressables.LoadAssetAsync<GameObject>(popupAssetReference.modalAssetReference);
            handle.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    var newPopupObject = Instantiate(handle.Result, _foundCanvasByLayer.associatedCanvas.transform);
                    var newPopup = newPopupObject.GetComponent<UIPopupDialog>();
                    if (newPopup != null)
                    {
                        newPopup.AssignArguments(arg);
                        m_activePopups.Add(newPopup);
                    }
                }
            };
            
        }

        private UIPopupDialog GetCachedUIPopup(PopupDialogData _dialogData)
        {
            if (_dialogData == null || m_cachedPopups.Count == 0)
            {
                return default;
            }

            var cachedPopup = m_cachedPopups.FirstOrDefault(uip => uip.data == _dialogData);
            return cachedPopup;
        }

        public void CreateFloatingTextAtCursor(string _displayString, Color _color)
        {
            var foundText = m_cachedPopupTexts.FirstOrDefault();
            var _textLayer = canvasByLayers[1].associatedCanvas;
            
            //If the popup already exists, use same popup
            if (!foundText.IsNull())
            {
                m_cachedPopupTexts.Remove(foundText);
                foundText.uiRectTransform.ResetTransform(_textLayer.transform);
                foundText.uiRectTransform.anchoredPosition = Input.mousePosition / _textLayer.scaleFactor;
                foundText.SetupText(_displayString, _color, CacheTextPopup);
                Debug.Log("Found Usable Text");
                return;
            }
            
            //Otherwise create a new popup text
            var handle = Addressables.LoadAssetAsync<GameObject>(textAssetRef);
            handle.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    var newPopupTextObject = Instantiate(handle.Result, _textLayer.transform);
                    newPopupTextObject.TryGetComponent(out PopupTextItem _textItem);
                    if (!_textItem.IsNull())
                    {
                        _textItem.uiRectTransform.anchoredPosition = Input.mousePosition / _textLayer.scaleFactor;
                        _textItem.SetupText(_displayString, _color, CacheTextPopup);
                    }
                }
            };
        }
        
        private void CacheTextPopup(PopupTextItem _textItem){
            if (_textItem.IsNull())
            {
                return;
            }
            
            m_cachedPopupTexts.Add(_textItem);
            _textItem.uiRectTransform.ResetTransform(cachedPopupTextPool);
        }

        public void CloseAllPopups()
        {
            if (m_activePopups.Count == 0)
            {
                return;
            }

            var newPopuplist = CommonUtils.ToNewList(m_activePopups);

            newPopuplist.ForEach(upd => upd.Close());
        }

        #endregion
        
    }
}