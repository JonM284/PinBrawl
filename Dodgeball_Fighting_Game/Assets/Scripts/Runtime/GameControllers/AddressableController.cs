using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Project.Scripts.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Runtime.GameControllers
{
    public class AddressableController: GameControllerBase
    {

        #region Static

        public static AddressableController Instance;

        #endregion

        #region Private Fields

        

        #endregion

        #region GameControllerBase Inherited Methods

        public override void Initialize()
        {
            Instance = this;
            base.Initialize();
        }

        #endregion
        
        #region Class Implementation

        public async UniTask<GameObject> T_LoadGameObject(AssetReference _assetReference, Action<GameObject> callback, Transform parent)
        {
            return await Addressables.LoadAssetAsync<GameObject>(_assetReference);
        }

        /// <summary>
        /// Load Asset reference, return GameObject. UniTask
        /// </summary>
        /// <param name="_assetReference">Addressable to Load</param>
        /// <returns>Addressable result as GameObject.</returns>
        public IEnumerator C_LoadGameObject(AssetReference _assetReference, Action<GameObject> callback, Transform parent)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(_assetReference);
            
            Debug.Log("<color=#00FF00>Loading GameObject</color>");

            if (!handle.IsDone)
            {
                yield return handle;
            }
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (!callback.IsNull())
                {
                    var instantiatedGO = handle.Result.Clone(parent);
                    callback?.Invoke(instantiatedGO);
                }
            }else{
                Debug.LogError($"Could not load addressable, {_assetReference.Asset.name}", _assetReference.Asset);
                Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Load Asset reference, return GameObject. UniTask
        /// </summary>
        /// <param name="_assetReference">Addressable Texture to Load</param>
        /// <param name="callback">What to do with return value</param>>
        public IEnumerator C_LoadTexture(AssetReference _assetReference, Action<Texture> callback)
        {
            var handle = Addressables.LoadAssetAsync<Texture>(_assetReference);
            
            Debug.Log("<color=#00FF00>Loading GameObject</color>");

            if (!handle.IsDone)
            {
                yield return handle;
            }
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (!callback.IsNull())
                {
                    callback?.Invoke(handle.Result);
                }
            }else{
                Debug.LogError($"Could not load addressable, {_assetReference.Asset.name}", _assetReference.Asset);
                Addressables.Release(handle);
            }
        }

        #endregion

    }
}