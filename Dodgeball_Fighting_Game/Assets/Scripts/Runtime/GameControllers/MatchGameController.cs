using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Data;
using Data.PerkDatas;
using Project.Scripts.Utils;
using Rewired;
using Runtime.Character;
using Runtime.GameModes;
using Runtime.Gameplay;
using Runtime.LevelManagers;
using Runtime.Perks;
using Runtime.UI.Items;
using Unity.Mathematics;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Runtime.GameControllers
{
    public class MatchGameController: GameControllerBase
    {
        
        #region Static

        public static MatchGameController Instance { get; private set; }

        #endregion

        #region Nested Classes

        [Serializable]
        public class SelectedCharactersByPlayer
        {
            public CharacterData characterData;
            public Player assignedPlayer;

            public SelectedCharactersByPlayer(CharacterData _data, Player _player)
            {
                characterData = _data;
                assignedPlayer = _player;
            }
        }

        [Serializable]
        public class PlayerDeathByTime
        {
            public BaseCharacter deadCharacter;
            public float timeOfDeath;

            public PlayerDeathByTime(BaseCharacter _baseCharacter, float _timeOfDeath)
            {
                deadCharacter = _baseCharacter;
                timeOfDeath = _timeOfDeath;
            }
        }

        #endregion

        #region Actions

        public static event Action OnRoundStart;

        public static event Action OnNewSetStart;
        
        #endregion

        #region Serialized Fields

        [SerializeField] private UIWindowData m_testGameMode;

        [SerializeField] private int m_testCharacterID;

        [SerializeField] private int m_amountOfCharacters = 2;

        [SerializeField] private float m_roundResetTimer = 3f;
        
        [SerializeField] private BallBehavior m_ballPrefab;

        [SerializeField] private BaseCharacter m_characterPrefab;

        [SerializeField] private PracticeDummyCharacter m_dummyPrefab;

        [SerializeField] private CharacterData m_dummyCharacterData;

        [SerializeField] private LayerMask m_groundMask, m_wallMask;

        [SerializeField] private PlayerHealthDataModel m_playerHealthDataModel;
        
        [SerializeField] private List<CharacterData> m_allCharacters = new List<CharacterData>();

        [SerializeField] private List<PerkDataBase> m_allPerks = new List<PerkDataBase>();

        [SerializeField] private int m_pointsNeededToWin = 3;

        [SerializeField] private bool m_isShowTutorial;

        #endregion

        #region Private Fields

        private Transform m_deadCharacterPool;

        private BaseLevelManager m_currentLevelManager;

        private GameModeManagerBase m_currentGameMode;

        private bool m_roundHasEnded, m_gameHasEnded;

        private List<PlayerMatchStats> m_playersStats = new List<PlayerMatchStats>();

        private List<PracticeDummyCharacter> m_tutorialDummies = new List<PracticeDummyCharacter>();

        private List<PracticeDummyCharacter> m_cachedTutorialDummies = new List<PracticeDummyCharacter>();
        
        private List<PracticeDummyCharacter> m_killedPracticeDummies = new List<PracticeDummyCharacter>();

        private List<SelectedCharactersByPlayer> m_selectedCharacters = new List<SelectedCharactersByPlayer>();

        private List<BaseCharacter> m_inMatchCharacters = new List<BaseCharacter>();

        private List<BaseCharacter> m_currentlyAliveCharacters = new List<BaseCharacter>();
        
        private List<PlayerDeathByTime> m_currentDeadCharacter = new List<PlayerDeathByTime>();

        private List<BallBehavior> m_createdBalls = new List<BallBehavior>();

        private List<BallBehavior> m_cachedBalls = new List<BallBehavior>();
        
        private int m_maxPossibleWeight, m_currentWeight, m_randomNum;

        private PerkDataBase m_randomPerk;
        
        private List<PerkDataBase> m_orderedPerks = new List<PerkDataBase>();

        private List<PerkDataBase> m_perksOfRarity = new List<PerkDataBase>();

        private List<PerkDataBase> m_randomPerkList = new List<PerkDataBase>();
        
        private List<PerkDataBase> m_availableMatchPerks = new List<PerkDataBase>();

        private List<PlayerHealthDataModel> m_usedHealthBars = new List<PlayerHealthDataModel>();
        
        #endregion
        
        #region Accessors

        public Transform deadCharacterPool => CommonUtils.GetRequiredComponent(ref m_deadCharacterPool, () => TransformUtils.CreatePool(transform, false));

        public List<PlayerMatchStats> playerMatchStats => m_playersStats;

        public BaseCharacter roundWinningCharacter { get; private set; }
        
        public BaseCharacter matchWinningCharacter { get; private set; }

        public List<BaseCharacter> allPlayers => m_inMatchCharacters;
        
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

        #region Unity Events

        private void OnEnable()
        {
            BaseCharacter.OnPlayerDeath += BaseCharacterOnPlayerDeath;
        }

        private void OnDisable()
        {
            BaseCharacter.OnPlayerDeath -= BaseCharacterOnPlayerDeath;
        }

        #endregion
        
        
        #region Class Implementation

        
        private void BaseCharacterOnPlayerDeath(BaseCharacter _dyingCharacter, BaseCharacter _attacker, Vector3 _deathPosition)
        {
            if (_dyingCharacter.IsNull())
            {
                return;
            }
            
            //Hide Character
            _dyingCharacter.gameObject.transform.ResetTransform(deadCharacterPool);

            //Dying Character Stats, death: + 1
            AddDeathPoint(_dyingCharacter);
            
            //Attacking Character Stats, Kills: + 1
            AddKillPoint(_attacker);

            if (m_currentlyAliveCharacters.Contains(_dyingCharacter))
            {
                m_currentlyAliveCharacters.Remove(_dyingCharacter);
            }
            
            m_currentDeadCharacter.Add(new PlayerDeathByTime(_dyingCharacter, Time.time));
            
            CheckPlayersAlive();
        }


        private void CheckPlayersAlive()
        {
            if (m_currentlyAliveCharacters.Count > 1)
            {
                return;
            }
            
            //remove ball
            m_createdBalls.ForEach(bb =>
            {
                bb.transform.ResetTransform(deadCharacterPool);
                bb.ResetBall();
                m_cachedBalls.Add(bb);
            });
            
            m_createdBalls.Clear();

            roundWinningCharacter = m_currentlyAliveCharacters.FirstOrDefault();

            if (roundWinningCharacter.IsNull())
            {
                //orderByDescending => 3,2,1 lowest is last, highest value is first
                roundWinningCharacter = m_currentDeadCharacter.OrderByDescending(pdt => pdt.timeOfDeath).FirstOrDefault().deadCharacter;
            }
            
            //Show round update
            AddRoundPoint(roundWinningCharacter);
            
            ResetAllForRound(false);
        }
        
        public async UniTask StartMatch()
        {
            m_roundHasEnded = false;
            m_gameHasEnded = false;

            SetInitialAvailablePerks();
            
            //Create Characters -> Create Health bars
            await CreateCharacters();

            if (!m_isShowTutorial)
            {
                await CreateBall();
            }
            
            //Create Game Mode
            UIController.Instance.AddUI(m_testGameMode);

            if (m_currentGameMode.IsNull())
            {
                await UniTask.WaitUntil(() => !m_currentGameMode.IsNull());
            }
            
            //Add points needed to win a setting
            await m_currentGameMode.Initialize(m_pointsNeededToWin);
            
            UIController.Instance.FadeBlackScreen(false);
            
            if (m_isShowTutorial)
            {
                await StartTutorial();
            }
            
            await UniTask.WaitForSeconds(1.25f);

            if (m_isShowTutorial)
            {
                return;
            }
            
            //Start Countdown -> Start Match
            //Countdown
            await m_currentLevelManager.T_Countdown();
            
            OnRoundStart?.Invoke();
        }

        private async UniTask ResetAllForRound(bool _isSkipCelebration)
        {
            if (!_isSkipCelebration)
            {
                //small wait before celebration
                await UniTask.WaitForSeconds(0.25f);
            
                //celebration => BM time
                CameraUtils.SetCameraZoom(0.35f);
                CameraUtils.SetCameraTrackPos(roundWinningCharacter.transform, true);
            
                //Wait for time
                await UniTask.WaitForSeconds(m_roundResetTimer);
            }

            //Reset Camera
            CameraUtils.SetCameraZoom(0.85f);
            CameraUtils.SetCameraTrackPos(Vector3.zero, false);
            
            //Reset all remaining characters positions
            //Note: character controller being enabled will now allow you to manually change the transform.position of an object
            foreach (var _aliveCharacter in m_currentlyAliveCharacters)
            {
                _aliveCharacter.transform.ResetTransform(deadCharacterPool);
            }
            
            //Reset Players => Positions, Alive ,Health, Etc.
            m_currentlyAliveCharacters.Clear();
            m_currentDeadCharacter.Clear();
            
            foreach (var _validCharacter in m_inMatchCharacters)
            {
                m_currentlyAliveCharacters.Add(_validCharacter);
            }
            
            for (int i = 0; i < m_currentlyAliveCharacters.Count; i++)
            {
                m_currentlyAliveCharacters[i].ResetCharacter();
                
                m_currentlyAliveCharacters[i].transform.position =
                    m_currentLevelManager.GetPlayerSpawnLocation(m_inMatchCharacters.Count, i).position;
                
                if (!m_currentlyAliveCharacters[i].transform.parent.IsNull())
                {
                    m_currentlyAliveCharacters[i].transform.parent = null;
                }
                
                m_currentlyAliveCharacters[i].Pause_UnPause_Character(true);
            }
            
            //Do Gameplay Related things
            await m_currentGameMode.UpdateScores();

            if (m_roundHasEnded && !m_gameHasEnded)
            {
                await m_currentGameMode.ExtraActions();
                m_roundHasEnded = false;
                //ResetAllPlayersRoundsScores();
            }else if (m_gameHasEnded)
            {
                matchWinningCharacter = m_playersStats.FirstOrDefault(pms => pms.roundPoints >= m_pointsNeededToWin).playerCharacter;
                await m_currentGameMode.ShowFinalScreen();
                //Game over screen
                await m_currentLevelManager.EndMatch(matchWinningCharacter);
                return;
            }
            
            //Reset Ball
            await CreateBall();

            //Countdown
            await m_currentLevelManager.T_Countdown();
            
            foreach (var _character in m_currentlyAliveCharacters)
            {
                _character.Pause_UnPause_Character(false);
            }
            
            //Let Characters Move, take down invisible walls => Maybe?
            OnRoundStart?.Invoke();
        }

        private void ResetAllPlayersRoundsScores()
        {
            foreach (var _stat in m_playersStats)
            {
                _stat.roundPoints = 0;
            }
        }
        
        private async UniTask CreateBall()
        {
            await UniTask.Yield();
            
            var _ball = GetBall();

            if (!_ball.transform.parent.IsNull())
            {
                _ball.transform.parent = null;
            }

            _ball.Initialize(m_currentLevelManager.GetLevelMinMax()._min.position, m_currentLevelManager.GetLevelMinMax()._max.position);
            
            _ball.transform.position = m_currentLevelManager.GetRandomBallSpawnPos().position;
            
            m_createdBalls.Add(_ball);
        }

        private BallBehavior GetBall()
        {
            if (m_cachedBalls.Count <= 0)
            {
                return Instantiate(m_ballPrefab);
            }
            
            var firstBall = m_cachedBalls.FirstOrDefault();
            m_cachedBalls.Remove(firstBall);
            return firstBall;
        }

        public void AssignGameMode(GameModeManagerBase _gameModeManager)
        {
            if (_gameModeManager.IsNull())
            {
                return;
            }

            m_currentGameMode = _gameModeManager;
        }

        public void UnassignGameMode()
        {
            m_currentGameMode = null;
        }

        public void UnassignGameMode(GameModeManagerBase _gameModeManager)
        {
            if (_gameModeManager.IsNull())
            {
                return;
            }

            if (_gameModeManager != m_currentGameMode)
            {
                return;
            }

            m_currentGameMode = null;
        }

        public async UniTask EndGame()
        {
            m_currentGameMode.DeInitialize();
            
            UnassignGameMode();
            UnassignLevelManager();

            foreach (var _character in m_inMatchCharacters)
            {
                Destroy(_character.gameObject);
            }

            foreach (var _healthBar in m_usedHealthBars)
            {
                Destroy(_healthBar.gameObject);
            }
            
            m_usedHealthBars.Clear();
            m_currentlyAliveCharacters.Clear();
            m_selectedCharacters.Clear();
            m_currentDeadCharacter.Clear();
            m_inMatchCharacters.Clear();
            m_playersStats.Clear();
            
            SceneController.Instance.LoadScene(SceneName.MainMenu, false);
        }
        
        public void AssignLevelManager(BaseLevelManager _newLevelManager)
        {
            m_currentLevelManager = _newLevelManager;
            
            //TODO: REMOVE WHEN ABLE TO CHOOSE CHARACTERS
            if (m_selectedCharacters.Count <= 0)
            {
                for (int i = 0; i < m_amountOfCharacters; i++)
                {
                    m_selectedCharacters.Add(new SelectedCharactersByPlayer(m_allCharacters[m_testCharacterID], ReInput.players.GetPlayer(i)));
                }
            }
            
            Debug.Log("Starting Match");

            StartMatch();
        }

        //end game
        public void UnassignLevelManager()
        {
            m_currentLevelManager = null;
        }

        public void AssignSelectedCharacter(CharacterData _selectedCharacter, Player _assigningPlayer)
        {
            m_selectedCharacters.Add(new SelectedCharactersByPlayer(_selectedCharacter, _assigningPlayer));
        }

        public void AssignSelectedCharacters(List<CharacterData> _selectedCharacters)
        {
            for (int i = 0; i < _selectedCharacters.Count; i++)
            {
                AssignSelectedCharacter(_selectedCharacters[i], ReInput.players.GetPlayer(i));
            }
        }

        private async UniTask CreateCharacters()
        {
            if (m_selectedCharacters.IsNull() || m_selectedCharacters.Count == 0)
            {
                Debug.Log("Selected Characters = NULL");
                return;
            }
            
            for(int i = 0; i < m_selectedCharacters.Count; i++)
            {
                var _newlyCreatedBaseCharacter = Instantiate(m_characterPrefab, m_currentLevelManager
                    .GetPlayerSpawnLocation(m_selectedCharacters.Count, i).position, quaternion.identity);
                
                var _characterModel = Instantiate(m_selectedCharacters[i].characterData.characterModelRef,
                    _newlyCreatedBaseCharacter.GetCharacterModelParent());
                
                _characterModel.transform.parent = _newlyCreatedBaseCharacter.GetCharacterModelParent();

                var _healthBar = Instantiate(m_playerHealthDataModel, m_currentLevelManager.GetCanvasTransform());
                
                await _newlyCreatedBaseCharacter.InitializeCharacter(m_selectedCharacters[i].characterData, 
                    i, m_selectedCharacters[i].assignedPlayer, m_groundMask, m_wallMask);
                
                _healthBar.Initialize(_newlyCreatedBaseCharacter, m_selectedCharacters[i].characterData.characterArmorAmount,
                    100f, m_currentLevelManager.GetStaticHealthParent());

                m_usedHealthBars.Add(_healthBar);
                
                m_inMatchCharacters.Add(_newlyCreatedBaseCharacter);
                m_currentlyAliveCharacters.Add(_newlyCreatedBaseCharacter);
                
                m_playersStats.Add(new PlayerMatchStats(_newlyCreatedBaseCharacter));
                
                Debug.Log("Created Character");
            }
            
        }

        private void AddKillPoint(BaseCharacter _character)
        {
            if (_character.IsNull())
            {
                return;
            }

            var _foundStats = GetCorrectStats(_character);

            if (_foundStats.IsNull())
            {
                Debug.Log("<color=red>Can't find player in stats</color>");
                return;
            }

            _foundStats.killsAmount++;
        }

        private void AddDeathPoint(BaseCharacter _character)
        {
            if (_character.IsNull())
            {
                return;
            }

            var _foundStats = GetCorrectStats(_character);

            if (_foundStats.IsNull())
            {
                Debug.Log("<color=red>Can't find player in stats</color>");
                return;
            }

            _foundStats.deathsAmount++;
        }

        /// <summary>
        /// 2-3 rounds in 1 Set
        /// </summary>
        /// <param name="_character"></param>
        public void AddRoundPoint(BaseCharacter _character)
        {
            if (_character.IsNull())
            {
                return;
            }

            var _foundStats = GetCorrectStats(_character);

            if (_foundStats.IsNull())
            {
                Debug.Log("<color=red>Can't find player in stats</color>");
                return;
            }

            _foundStats.roundPoints++;
            
            m_roundHasEnded = true;

            if (_foundStats.roundPoints >= m_pointsNeededToWin)
            {
                m_gameHasEnded = true;
            }
        }

        public void SetPointsNeededToWin(int _pointsNeeded)
        {
            m_pointsNeededToWin = _pointsNeeded;
        }

        public List<PlayerMatchStats> GetStatsList()
        {
            return m_playersStats;
        }

        public PlayerMatchStats GetCorrectStats(BaseCharacter _character)
        {
            return m_playersStats.FirstOrDefault(pms => pms.playerCharacter == _character);
        }

        public int GetCharacterAmount() => m_allCharacters.Count;
        
        public CharacterData GetCharacterDataAtIndex(int _index)
        {
            return m_allCharacters[_index];
        }

        #region Perks ------------------------
        
        public List<PerkDataBase> GetRandomPerkSet(int _amountOfPerks)
        {
            m_randomPerkList.Clear();

            ResetOrderedPerks();

            for (int i = 0; i < _amountOfPerks; i++)
            {
                m_randomPerkList.Add(GetRandomPerk());
            }

            return m_randomPerkList;
        }

        public List<PerkDataBase> GetRandomWithGuarantee(int _amountOfPerks, int _amountOfGuaranteePerks ,PerkRarity _perkRarity)
        {
            m_randomPerkList.Clear();
            
            ResetOrderedPerks();
            
            for (int i = 0; i < _amountOfGuaranteePerks; i++)
            {
                if (!HasPerksOfRarity(_perkRarity))
                {
                    break;
                }
                
                m_randomPerkList.Add(GetRandomPerkOfRarity(_perkRarity));
            }

            var _amountLeftOver = _amountOfPerks - m_randomPerkList.Count;

            for (int i = 0; i < _amountLeftOver; i++)
            {
                m_randomPerkList.Add(GetRandomPerk());
            }

            return m_randomPerkList;
        }

        public bool HasPerksOfRarity(PerkRarity _perkRarity)
        {
            return m_orderedPerks.Any(pdb => pdb.perkRarity == _perkRarity);
        }

        public PerkDataBase GetRandomPerkOfRarity(PerkRarity _perkRarity)
        {
            m_perksOfRarity.Clear();
            
            m_perksOfRarity = m_orderedPerks.Where(pdb => pdb.perkRarity == _perkRarity).ToList();
            
            m_perksOfRarity.OrderByDescending(pdb => pdb.rarityWeight);
            
            m_randomPerk = m_perksOfRarity.FirstOrDefault();

            if (m_perksOfRarity.IsNull() || m_perksOfRarity.Count == 0)
            {
                return null;
            }

            m_randomNum = Random.Range(1, GetMaxWeightOfPerks(m_perksOfRarity) + 1);
            
            m_currentWeight = 0;
            
            foreach (var _perk in m_perksOfRarity)
            {
                m_currentWeight += _perk.rarityWeight;
                if (m_randomNum <= m_currentWeight)
                {
                    m_randomPerk = _perk;
                    break;
                }
            }

            m_perksOfRarity.Remove(m_randomPerk);

            return m_randomPerk;
            
        }
        
        public PerkDataBase GetRandomPerk()
        {
            //default value
            m_randomPerk = m_orderedPerks.FirstOrDefault();
            
            //+1 because max value is exclusive
            m_randomNum = Random.Range(1, GetMaxWeightOfPerks(m_orderedPerks) + 1);

            m_currentWeight = 0;
            foreach (var _perk in m_orderedPerks)
            {
                m_currentWeight += _perk.rarityWeight;
                if (m_randomNum <= m_currentWeight)
                {
                    m_randomPerk = _perk;
                    break;
                }
            }

            m_orderedPerks.Remove(m_randomPerk);

            return m_randomPerk;
        }

        public void SetPerkSelected(PerkDataBase _perkData)
        {
            if (!m_availableMatchPerks.Contains(_perkData))
            {
                return;
            }
            
            m_availableMatchPerks.Remove(_perkData);
        }

        private void ResetOrderedPerks()
        {
            m_orderedPerks.Clear();
            m_orderedPerks = m_availableMatchPerks.ToList();
            m_orderedPerks.OrderByDescending(pdb => pdb.rarityWeight);            
        }

        private void SetInitialAvailablePerks()
        {
            m_availableMatchPerks.Clear();
            m_availableMatchPerks = m_allPerks.ToList();
        }

        public int GetMaxWeightOfPerks(List<PerkDataBase> _perks)
        {
            foreach (var _perkData in _perks)
            {
                m_maxPossibleWeight += _perkData.rarityWeight;
            }

            return m_maxPossibleWeight;
        }

        private async UniTask StartTutorial()
        {

            m_tutorialDummies.Clear();
            
            for(int i = 0; i < m_selectedCharacters.Count; i++)
            {
                var _newDummyCharacter = GetTutorialDummy(i);
                
                await _newDummyCharacter.InitializeCharacter(m_dummyCharacterData, 
                    -1, null, m_groundMask, m_wallMask);
                
                m_tutorialDummies.Add(_newDummyCharacter);
                
                var _ball = GetBall();

                if (!_ball.transform.parent.IsNull())
                {
                    _ball.transform.parent = null;
                }

                _ball.Initialize(m_currentLevelManager.GetLevelMinMax()._min.position, m_currentLevelManager.GetLevelMinMax()._max.position);
            
                _ball.transform.position = m_currentLevelManager.GetTutorialBallSpawnLocation(m_selectedCharacters.Count, i).position;
            
                m_createdBalls.Add(_ball);
                
                Debug.Log("Created Tutorial Dummy and Ball");
            }
            
            m_currentLevelManager.SetTutorialActive(true, m_selectedCharacters.Count);

            m_killedPracticeDummies.Clear();
            
            await UniTask.WaitUntil(() => ReInput.players.AllPlayers.Any(p => p.GetButtonDown("Select")));
            
            m_currentLevelManager.SetTutorialUIActive(false);

            OnRoundStart?.Invoke();
            
        }

        private PracticeDummyCharacter GetTutorialDummy(int _index)
        {
            if (m_cachedTutorialDummies.Count <= 0)
            {
                return Instantiate(m_dummyPrefab, m_currentLevelManager
                    .GetDummySpawnLocation(m_selectedCharacters.Count, _index).position, quaternion.identity);
            }
            
            var _tutDummy = m_cachedTutorialDummies.FirstOrDefault();
            m_cachedTutorialDummies.Remove(_tutDummy);
            _tutDummy.transform.parent = null;
            _tutDummy.transform.position = m_currentLevelManager
                .GetDummySpawnLocation(m_selectedCharacters.Count, _index).position;
            return _tutDummy;
        }

        private async UniTask EndTutorial()
        {
            //m_currentlyAliveCharacters.ForEach(bc => bc.Pause_UnPause_Character(true));
            
            //remove ball
            m_createdBalls.ForEach(bb =>
            {
                bb.transform.ResetTransform(deadCharacterPool);
                bb.ResetBall();
                m_cachedBalls.Add(bb);
            });
            
            m_createdBalls.Clear();
            
            m_tutorialDummies.ForEach(pdc =>
            {
                pdc.gameObject.transform.ResetTransform(deadCharacterPool);
                m_cachedTutorialDummies.Add(pdc);
            });
            
            m_tutorialDummies.Clear();
            
            m_currentLevelManager.SetTutorialActive(false, m_selectedCharacters.Count);

            ResetAllForRound(true);
        }

        public void TempKillDummyCharacter(PracticeDummyCharacter _character, Vector3 _deathLocation, Vector3 _deathDirection, Vector3 _spawnLocation)
        {
            if (_character.IsNull())
            {
                return;
            }

            if (!m_killedPracticeDummies.Contains(_character))
            {
                m_killedPracticeDummies.Add(_character);
            }

            _character.transform.position = _spawnLocation;
            _character.OnRevive();

            if (m_killedPracticeDummies.Count >= m_selectedCharacters.Count)
            {
                EndTutorial();
            }
        }

        #endregion
        
        
        #endregion
        
        
    }
}