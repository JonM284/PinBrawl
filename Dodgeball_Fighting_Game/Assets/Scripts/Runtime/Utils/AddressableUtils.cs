using System.Collections.Generic;
using Runtime.Character;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Utils
{
    public static class AddressableUtils
    {

        private static string _generalIconAddress = "Assets/Images/UI_Images/";

        public static string GetIconAddress()
        {
            return _generalIconAddress;
        }
        
        public static string GetUpgradeIconAddress()
        {
            return _generalIconAddress + "Upgrades/{0}";
        }
        
    }
}