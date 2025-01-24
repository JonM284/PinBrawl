using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace Runtime.Misc
{
    public class MainMenuManager: MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField] private float m_cameraMoveAnimDuration = 0.1f;
        
        [SerializeField] private Transform m_mainMenuLoc;
        [SerializeField] private Transform m_characterSelectLoc;

        [SerializeField] private GameObject m_mainMenuHolder;
        [SerializeField] private GameObject m_characterSelectMenuHolder;

        [SerializeField] private Transform m_camera;

        #endregion


        #region Class Implementation

        public void OnPlayButtonPress()
        {
            m_camera.DOMove(m_characterSelectLoc.position, m_cameraMoveAnimDuration);
            m_camera.DORotate(m_characterSelectLoc.localEulerAngles, m_cameraMoveAnimDuration);
            m_mainMenuHolder.SetActive(false);
            m_characterSelectMenuHolder.SetActive(true);
        }

        public void OnReturnToMainMenu()
        {
            m_camera.DOMove(m_mainMenuLoc.position, m_cameraMoveAnimDuration);
            m_camera.DORotate(m_mainMenuLoc.localEulerAngles, m_cameraMoveAnimDuration);
            m_mainMenuHolder.SetActive(true);
            m_characterSelectMenuHolder.SetActive(false);
        }

        public void OnQuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#endif
            
            Application.Quit();
        }
        
        
        #endregion
        

    }
}