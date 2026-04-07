using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using Runtime.GameControllers;
using Runtime.Perks;
using TMPro;
using UnityEngine;

namespace Runtime.UI.Items
{
    public class UpgradeHolderUIItem: MonoBehaviour
    {

        [SerializeField] private UpgradeType upgradeType;
        [SerializeField] private List<UpgradeUIItem> upgrades = new();
        [SerializeField] private TMP_Text categoryText;

        public int maxXIndex { get; private set; }

        public async UniTask InitializeUpgrades()
        {
            var allAvailableUpgrades = MatchGameController.Instance.GetAllUpgrades()
                .Where(pdb => pdb.UpgradeType == upgradeType)
                .ToList();

            categoryText.text = upgradeType.ToString();
            
            for (var i = 0; i < upgrades.Count(); i++)
            {
                upgrades[i].gameObject.SetActive(i < allAvailableUpgrades.Count);
                if (i >= allAvailableUpgrades.Count || allAvailableUpgrades[i].IsNull())
                {
                    upgrades[i].gameObject.SetActive(false);
                    continue;
                }

                await upgrades[i].InitializeItem(allAvailableUpgrades[i]);
            }

            maxXIndex = allAvailableUpgrades.Count;
        }
        
        public UpgradeUIItem GetUpgradeItemByIndex(int index)
        {
            return index < 0 ? default : upgrades[index];
        }
    }
}