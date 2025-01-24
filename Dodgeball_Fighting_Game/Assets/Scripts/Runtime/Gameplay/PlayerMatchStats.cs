using System;
using Runtime.Character;

namespace Runtime.Gameplay
{
    [Serializable]
    public class PlayerMatchStats
    {
        public BaseCharacter playerCharacter;
        public int killsAmount;
        public int deathsAmount;
        public float dealtDamage;
        public int ballSwapTries;
        public int ballSwapSuccesses;
        public int roundPoints;
        public int generalPoints;


        public PlayerMatchStats(BaseCharacter _character)
        {
            playerCharacter = _character;
            killsAmount = 0;
            deathsAmount = 0;
            dealtDamage = 0;
            ballSwapTries = 0;
            ballSwapSuccesses = 0;
            roundPoints = 0;
            generalPoints = 0;
        }
    }
    
}