using System.Threading;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Utils;

namespace Runtime.UI.Icons
{
    public class IconBase: MonoBehaviour
    {

        [SerializeField] protected Image targetImage;

        public async UniTask GetIcon(string iconName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (targetImage.IsNull())
            {
                return;
            }
            
            var icon = await Addressables.LoadAssetAsync<Sprite>(string.Format(AddressableUtils.GetIconAddress(), iconName))
                .WithCancellation(token);

            if (icon.IsNull())
            {
                targetImage.gameObject.SetActive(false);
                return;
            }

            targetImage.sprite = icon;
        }
        
        public async UniTask GetIcon(string folderName, string iconName, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (targetImage.IsNull())
            {
                return;
            }
            
            var icon = await Addressables.LoadAssetAsync<Sprite>(string.Format(folderName, iconName))
                .WithCancellation(token);

            if (icon.IsNull())
            {
                targetImage.gameObject.SetActive(false);
                return;
            }

            targetImage.sprite = icon;
        }

        public async UniTask GetIcon(Sprite icon)
        {
            if (targetImage.IsNull())
            {
                return;
            }

            targetImage.sprite = icon;
            await UniTask.CompletedTask;
        }

        public void RemoveIcon()
        {
            targetImage.sprite = null;
        }


    }
}