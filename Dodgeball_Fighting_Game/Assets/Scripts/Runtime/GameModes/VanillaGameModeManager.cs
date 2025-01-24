using Cysharp.Threading.Tasks;

namespace Runtime.GameModes
{
    public class VanillaGameModeManager: GameModeManagerBase
    {
        
        /// <summary>
        /// Normal Game Mode:
        /// Rules: Each character has armor, shield, bunt, wack.
        /// Win Condition: Player wins enough rounds.
        /// </summary>
        

        #region GameModeManagerBase Inherited Methods

        public override async UniTask Initialize(int _pointsNeededToWin)
        {
            await base.Initialize(_pointsNeededToWin);
            
        }
        
        public override async UniTask UpdateScores()
        {
            
            await base.UpdateScores();
        }
        
        public override async UniTask ShowFinalScreen()
        {
            
            await base.ShowFinalScreen();
        }

        #endregion

        
        
    }
}