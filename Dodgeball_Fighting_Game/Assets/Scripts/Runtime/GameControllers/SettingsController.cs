using Data;
using Project.Scripts.Utils;
using UnityEngine;

namespace Runtime.GameControllers
{
    public class SettingsController: GameControllerBase
    {
        
        #region Static

        public static SettingsController Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [SerializeField] private SettingsData _settingsData;

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

        public int GetMaxAmountOfPlayers()
        {
            return _settingsData.maxAmountOfPlayer;
        }
        
        public ColorblindOptions GetCurrentColorblindOption()
        {
            return _settingsData.colorblindOptions;
        }

        public Color GetColorByPlayerIndex(int _index)
        {
            return _settingsData.colorsByPlayerIndex[_index];
        }
        
        public Color GetMidColorByPlayerIndex(int _index)
        {
            return _settingsData.midColorsByPlayerIndex[_index];
        }

        public Color GetDarkColorByPlayerIndex(int _index)
        {
            return _settingsData.darkColorsByPlayerIndex[_index];
        }

        public Gradient GetGradientByPlayerIndex(int _index)
        {
            return _settingsData.gradientsByPlayerIndex[_index];
        }

        public float GetPvpDamageEnergyAmount() => _settingsData.pvpDamageEnergyAddAmount;

        public float GetHitBallEnergyAmount() => _settingsData.hitBallEnergyAddAmount;

        public float GetPlayerKillEnergyAmount() => _settingsData.playerKillEnergyAddAmount;

        public Sprite GetWackIcon() => _settingsData.wackIcon;

        #endregion




    }
}