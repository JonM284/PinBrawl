using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using Runtime.Character;
using Runtime.GameControllers;
using Runtime.Gameplay;
using Runtime.UI.Items;
using UnityEngine;

namespace Runtime.GameModes
{
    public class VanillaGameModeManager: GameModeManagerBase
    {

        /// <summary>
        /// Normal Game Mode:
        /// Rules: Each character has armor, shield, bunt, wack.
        /// Win Condition: Player wins enough rounds.
        /// </summary>

        #region Read-Only Fields

        private readonly string m_orbPoolIdentifier = "OrbPool@Identifier";

        #endregion
        
        #region Serialized Fields

        [SerializeField] private GameObject m_orbPrefab;
        
        [SerializeField] private GameObject m_pointsUI;
        
        [SerializeField] private GameObject m_augmentationsUI;

        [SerializeField] private int m_amountOfPerksPerPlayer = 2;

        [SerializeField] private int m_normalAmountToWin = 3;

        [SerializeField] private PerkSelectionDataModel m_perkSelectionDataModel;
        
        [SerializeField] private List<PlayerStatsBlockUIItem> m_playerStatsBlocks = new List<PlayerStatsBlockUIItem>();

        #endregion

        #region Private Fields

        private List<BaseCharacter> m_playerList = new List<BaseCharacter>();

        private int m_pointsNeededToWin;

        private PlayerMatchStats m_roundWinnerStats;

        private PlayerMatchStats m_gameWinnerStats;

        private List<PlayerMatchStats> m_playerStats = new List<PlayerMatchStats>();

        private int m_orbMax = 5, m_inRoundOrbAmount;

        #endregion

        #region GameModeManagerBase Inherited Methods

        protected override void EnabledFunctions()
        {
            /*MatchGameController.OnRoundStart += ;
            MatchGameController.OnRoundEnd += ;*/
            base.EnabledFunctions();
        }

        protected override void DisabledFunctions()
        {
            /*MatchGameController.OnRoundStart -= ;
            MatchGameController.OnRoundEnd -= ;*/
            base.DisabledFunctions();
        }

        public override async UniTask Initialize(int _pointsNeededToWin, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            for (int i = 0; i < m_orbMax; i++)
            {
                await ObjectPoolController.Instance.T_PreCreateObject(m_orbPoolIdentifier, m_orbPrefab, token);
            }
            
            // force players to create their abilities
            //IMPORTANT: m_playerList is a reference, DO NOT change anything [READ-ONLY]
            m_playerList = MatchGameController.Instance.allPlayers;

            //IMPORTANT: m_playerStats is a reference, DO NOT change anything [READ-ONLY]
            m_playerStats = MatchGameController.Instance.GetStatsList();

            m_pointsNeededToWin = _pointsNeededToWin > 0 ? _pointsNeededToWin : m_normalAmountToWin;
            
            foreach (var baseCharacter in m_playerList)
            {
                await baseCharacter.T_AssignLargeAbility(token);
            }

            await UniTask.Yield();

            for (int i = 0; i < SettingsController.Instance.GetMaxAmountOfPlayers(); i++)
            {
                if (i > m_playerList.Count - 1)
                {
                    m_playerStatsBlocks[i].gameObject.SetActive(false);
                    continue;
                }
                
                await m_playerStatsBlocks[i].Initialize(m_playerList[i], m_pointsNeededToWin, false);
                await UniTask.Yield();
            }
            
            m_pointsUI.SetActive(false);
            m_augmentationsUI.SetActive(false);
            
            await base.Initialize(m_pointsNeededToWin, token);
        }
        
        public override async UniTask UpdateScores(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            //Show Round scores
            m_pointsUI.SetActive(true);
            
            foreach (var _statsBlock in m_playerStatsBlocks)
            {
                if (_statsBlock.assignedCharacter.IsNull())
                {
                    continue;
                }
                
                var _stats = MatchGameController.Instance.GetCorrectStats(_statsBlock.assignedCharacter);
                await _statsBlock.AwardWholePoint(_stats.roundPoints);
            }

            await UniTask.WaitForSeconds(2.25f);

            m_pointsUI.SetActive(false);
            await base.UpdateScores(token);
        }

        public override async UniTask ExtraActions(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            //If a player wins enough rounds, show Sets won score
            m_augmentationsUI.SetActive(true);
            
            //ToDo: Start Animation
            
            //For each losing player, allow them to select augments
            //Get Losing players
            m_roundWinnerStats = m_playerStats.FirstOrDefault(pms => pms.playerCharacter == MatchGameController.Instance.roundWinningCharacter);

            for (int i = 0; i < m_amountOfPerksPerPlayer; i++)
            {
                foreach (var _playerStat in m_playerStats)
                {
                    if (_playerStat == m_roundWinnerStats)
                    {
                        //Winning Character Can NOT select Augments
                        Debug.Log("Skipping Winner Stats");
                        continue;
                    }

                    Debug.Log("Showing Perks for Losing Player");

                    await m_perkSelectionDataModel.SetupSelectionScreenPlayer(_playerStat.playerCharacter);
                    m_perkSelectionDataModel.SetActiveState(true);

                    await UniTask.WaitUntil(() => !m_perkSelectionDataModel.isSelecting, cancellationToken: token);
                }
            }
            
            //ToDo: End Animation
            
            m_augmentationsUI.SetActive(false);
            await base.ExtraActions(token);
        }

        public override async UniTask ShowFinalScreen(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            m_pointsUI.SetActive(true);
            
            foreach (var _statsBlock in m_playerStatsBlocks)
            {
                if (_statsBlock.assignedCharacter.IsNull())
                {
                    continue;
                }
                
                var _stats = MatchGameController.Instance.GetCorrectStats(_statsBlock.assignedCharacter);
                await _statsBlock.AwardWholePoint(_stats.roundPoints);
            }
            
            await UniTask.WaitForSeconds(1.5f, cancellationToken: token);
            
            m_pointsUI.SetActive(false);
            
            await base.ShowFinalScreen(token);
        }
        
        public override async UniTask ShowEnd(CancellationToken token)
        {
            m_pointsUI.SetActive(false);
            
            //ToDo: Winning Animation!
            
            await base.ShowEnd(token);
        }

        #endregion

        
        
    }
}