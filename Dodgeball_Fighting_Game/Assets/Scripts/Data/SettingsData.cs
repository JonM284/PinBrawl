using System.Collections.Generic;
using Runtime.VFX;
using UnityEngine;

namespace Data
{
    
    [CreateAssetMenu(menuName = "Dodge-ball/Custom Data/Settings")]
    public class SettingsData: ScriptableObject
    {

        public Sprite wackIcon;

        public ColorblindOptions colorblindOptions = ColorblindOptions.NORMAL;
        
        public Color playerSideColor = Color.cyan;

        public Color enemySideColor = Color.red;
        
        public Color neutralSideColor = Color.black;

        public int maxAmountOfPlayer = 4;

        public List<Color> colorsByPlayerIndex = new List<Color>();

        public List<Color> midColorsByPlayerIndex = new List<Color>();
        
        public List<Color> darkColorsByPlayerIndex = new List<Color>();
        
        public List<Gradient> gradientsByPlayerIndex = new List<Gradient>();

        public float playerKillEnergyAddAmount = 20f;

        public float hitBallEnergyAddAmount = 5f;

        public float pvpDamageEnergyAddAmount = 5f;

    }
}