using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Data;
using GameControllers;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.ScriptedAnimations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Runtime.LevelManagers
{
    public abstract class BaseLevelManager: MonoBehaviour
    {
        
        #region Serialized Fields

        [SerializeField] private SceneName m_testGameModeSceneName;
        
        [SerializeField] private TMP_Text m_initialCountdownText;

        [SerializeField] private AnimationsBase m_initialAnimation;
        
        [SerializeField] private Transform m_levelMax;
        [SerializeField] private Transform m_levelMin;

        [SerializeField] private Transform m_healthBarParent, m_playerInfoUIParent;

        [SerializeField] private List<Transform> m_fourPlayerLocations = new List<Transform>();
        [SerializeField] private List<Transform> m_twoPlayerLocations = new List<Transform>();

        [SerializeField] private List<Transform> m_ballSpawnLocations = new List<Transform>();

        [Header("Tutorial")] 
        [SerializeField] private GameObject m_tutorial_walls_twoPlayer;
        [SerializeField] private GameObject m_tutorial_walls_fourPlayer;
        
        [SerializeField] private GameObject m_tutorial_UI;

        [SerializeField] private List<Transform> m_tut_ballSpawn_Locations_twoPlayer = new List<Transform>();
        [SerializeField] private List<Transform> m_tut_ballSpawn_Locations_fourPlayer = new List<Transform>();
        [SerializeField] private List<Transform> m_tut_dummySpawn_Locations_twoPlayer = new List<Transform>();
        [SerializeField] private List<Transform> m_tut_dummySpawn_Locations_fourPlayer = new List<Transform>();
        
        
        [Header("End Game Screen")]
        [SerializeField] private GameObject m_endScreen;

        [SerializeField] private Image m_characterImage;
        [SerializeField] private Image m_background;
        [SerializeField] private Image m_halfToneBackground;
        [SerializeField] private TMP_Text m_characterName;
        
        #endregion

        #region Private Fields

        private bool m_isMatchEnded;

        #endregion
        
        #region Unity Events

        private void Start()
        {
            SetupMatch();
        }

        #endregion

        #region Class Implementation

        private async UniTask SetupMatch()
        {
            m_endScreen.SetActive(false);
            
            VFXController.Instance.PreloadCommonVFX();
            
            //In regular game this is oK
            MatchGameController.Instance.AssignLevelManager(this);

            m_isMatchEnded = false;
        }

        public (Transform _max, Transform _min) GetLevelMinMax()
        {
            return (m_levelMax, m_levelMin);
        }

        public Transform GetPlayerSpawnLocation(int _amountOfPlayers, int _index)
        {
            if (_index > 3)
            {
                return null;
            }
            
            return _amountOfPlayers <= 2 ? m_twoPlayerLocations[_index] : m_fourPlayerLocations[_index];
        }

        public Transform GetDummySpawnLocation(int _amountOfPlayers, int _index)
        {
            if (_index > 3)
            {
                return null;
            }
            
            return _amountOfPlayers <= 2 ? m_tut_dummySpawn_Locations_twoPlayer[_index] : m_tut_dummySpawn_Locations_fourPlayer[_index];
        }

        public Transform GetTutorialBallSpawnLocation(int _amountOfPlayers, int _index)
        {
            return _amountOfPlayers <= 2
                ? m_tut_ballSpawn_Locations_twoPlayer[_index]
                : m_tut_ballSpawn_Locations_fourPlayer[_index];
        }

        public Transform GetRandomBallSpawnPos()
        {
            return m_ballSpawnLocations.Count == 1
                ? m_ballSpawnLocations[0]
                : m_ballSpawnLocations[Random.Range(0, m_ballSpawnLocations.Count)];
        }

        public Transform GetCanvasTransform()
        {
            return m_healthBarParent;
        }

        [ContextMenu("Manual Start")]
        public void ManualStartMatch()
        {
            MatchGameController.Instance.AssignLevelManager(this);
        }

        public void SetTutorialActive(bool _isActive, int _amountOfPlayers)
        {
            m_tutorial_walls_twoPlayer.SetActive(_isActive && _amountOfPlayers <= 2);
            m_tutorial_walls_fourPlayer.SetActive(_isActive && _amountOfPlayers > 2);
            SetTutorialUIActive(_isActive);
        }

        public void SetTutorialUIActive(bool _isActive)
        {
            m_tutorial_UI.SetActive(_isActive);
        }
        
        
        public async UniTask T_Countdown()
        {
            int _countDownNum = 3;
            
            for (int i = 0; i < 3; i++)
            {
                m_initialCountdownText.text = _countDownNum.ToString();
                m_initialCountdownText.gameObject.SetActive(true);
                m_initialAnimation.Play();
                await UniTask.WaitUntil(() => !m_initialAnimation.isPlaying);
                _countDownNum--;
                m_initialCountdownText.gameObject.SetActive(false);
            }
            
            m_initialCountdownText.gameObject.SetActive(false);
        }

        public async UniTask EndMatch(BaseCharacter _baseCharacter)
        {
            ChangeWinningCharacterImage(_baseCharacter);
            
            m_isMatchEnded = true;
            m_endScreen.SetActive(true);

            await UniTask.WaitUntil(() => Rewired.ReInput.players.AllPlayers.Any(p => p.GetButtonLongPress("Start_Action")));

            EndGame();
        }

        private void ChangeWinningCharacterImage(BaseCharacter _baseCharacter)
        {
            if (_baseCharacter.IsNull())
            {
                return;
            }

            m_characterName.text = _baseCharacter.characterData.characterName;
            m_characterImage.sprite = _baseCharacter.characterData.characterIconRef;
            m_background.color = SettingsController.Instance.GetColorByPlayerIndex(_baseCharacter.GetPlayerIndex());
            m_halfToneBackground.color = SettingsController.Instance.GetDarkColorByPlayerIndex(_baseCharacter.GetPlayerIndex());
        }

        public void EndGame()
        {
            MatchGameController.Instance.EndGame();
        }
        
      
        public Transform GetStaticHealthParent()
        {
            return m_playerInfoUIParent;
        }
        #endregion

    }
}