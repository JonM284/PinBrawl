using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.ScriptedAnimations;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Items
{
    public class PlayerStatsBlockUIItem: MonoBehaviour
    {


        #region Serialized Fields

        [SerializeField] private Image m_characterIcon;

        [SerializeField] private Image m_characterIconBackground;
        
        [SerializeField] private Image m_blockBackground;

        [SerializeField] private List<Image> m_pointImageParents = new List<Image>();
        
        [SerializeField] private List<Image> m_actualPointImages = new List<Image>();

        [SerializeField] private List<AnimationsBase> m_pointAnimation = new List<AnimationsBase>();

        #endregion

        #region Private Fields

        private int m_amountToWin, m_amountToWinSet;

        private int m_currentPartialPoints, m_currentWholePoints;

        private float m_animationTimeMax = 0.15f;
        
        #endregion

        #region Accessors

        public BaseCharacter assignedCharacter { get; private set; }

        #endregion

        #region Class Implementation

        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="_character">Connected Character</param>
        /// <param name="_pointsToWin">Amount of Points needed to win</param>
        /// <param name="_isSetBased">Incoming Points are towards rounds or sets</param>
        public async UniTask Initialize(BaseCharacter _character, int _pointsToWin, bool _isSetBased, int _pointsForSet = 2)
        {
            if (_character.IsNull())
            {
                return;
            }

            assignedCharacter = _character;
            m_amountToWin = _pointsToWin;
            
            m_characterIcon.sprite = assignedCharacter.characterData.characterIconRef;

            m_amountToWinSet = _pointsForSet;

            m_characterIconBackground.color = assignedCharacter.playerColor;
            m_blockBackground.color = assignedCharacter.playerMidColor;

            for (int i = 0; i < m_pointImageParents.Count; i++)
            {
                await UniTask.Yield();

                m_pointImageParents[i].gameObject.SetActive(i < m_amountToWin);

                if (i >= m_amountToWin)
                {
                    continue;
                }

                if (m_actualPointImages[i].IsNull())
                {
                    Debug.Log("Can't find actual point");
                    continue;
                }

                m_pointImageParents[i].color = assignedCharacter.playerDarkColor;
                m_actualPointImages[i].color = assignedCharacter.playerColor;
                m_actualPointImages[i].transform.localScale = _isSetBased ? Vector3.one : Vector3.zero;
                m_actualPointImages[i].fillAmount = _isSetBased ? 0 : 1;
                m_actualPointImages[i].gameObject.SetActive(_isSetBased);
            }
        }

        public async UniTask AwardPartialPoint(int _newPointsAmount)
        {
            if (_newPointsAmount == m_currentPartialPoints)
            {
                return;
            }
            
            
            m_currentPartialPoints = _newPointsAmount;
            
            m_actualPointImages[m_currentWholePoints].fillAmount = m_currentPartialPoints / (float)m_amountToWinSet;
            
            //play animation?
            
            if (m_actualPointImages[m_currentWholePoints].fillAmount < 1)
            {
                return;
            }

            m_currentWholePoints++;
        }

        public async UniTask AwardWholePoint(int _newPointsAmount)
        {
            if (_newPointsAmount == m_currentWholePoints)
            {
                return;
            }
            
            for (int i = 0; i < m_amountToWin; i++)
            {

                if (m_actualPointImages[i].gameObject.activeSelf && i >= _newPointsAmount)
                {
                    m_pointAnimation[i].PlayReverse();
                    await UniTask.WaitUntil(() => !m_pointAnimation[i].isPlaying);
                    m_actualPointImages[i].gameObject.SetActive(false);
                }else if (!m_actualPointImages[i].gameObject.activeSelf && i <_newPointsAmount)
                {
                    m_actualPointImages[i].gameObject.SetActive(true);
                    m_pointAnimation[i].Play();
                    await UniTask.WaitUntil(() => !m_pointAnimation[i].isPlaying);
                }
                
            }

            m_currentWholePoints = _newPointsAmount;
        }

        #endregion



    }
}