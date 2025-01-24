using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Project.Scripts.Utils
{
    public static class GameObjectUtils
    {

        public static GameObject Clone(this GameObject objectToClone, Transform parent = null)
        {
            var clonedObject = GameObject.Instantiate(objectToClone, parent);
            return clonedObject;
        }

        public static void CloneAddressable(this AssetReference objectToClone, Transform parent)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(objectToClone);
            handle.Completed += operation =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject.Instantiate(operation.Result, parent);
                }
            };
        }

        public static GameObject CloneAddressableReturn(this AssetReference objectToClone, Transform parent)
        {
            var obj = Addressables.InstantiateAsync(objectToClone, parent);
            if (obj.IsDone)
            {
                return obj.Result;
            }

            return default;
        }

    }
}