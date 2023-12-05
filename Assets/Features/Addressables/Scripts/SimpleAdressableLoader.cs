using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CodeZash.Addressable {
    public enum INVOKE_WHEN {
        START,
        AWAKE,
        ON_ENABLE,
        ON_DISABLE,
        ON_DESTROY
    }

    public class SimpleAdressableLoader : MonoBehaviour {

        [SerializeField] private INVOKE_WHEN invokeWhen = INVOKE_WHEN.START;
        [SerializeField] private AssetReferenceGameObject assetReferenceGameObject;

        private void Awake() {
            if (invokeWhen != INVOKE_WHEN.AWAKE) return;

            LoadAsset(assetReferenceGameObject);
        }

        private void Start() {
            if (invokeWhen != INVOKE_WHEN.START) return;

            LoadAsset(assetReferenceGameObject);
        }

        private void LoadAsset(AssetReferenceGameObject assetReferenceGameObject) {
            var handler = assetReferenceGameObject.LoadAssetAsync();
            handler.Completed += OnLoadDone;
        }
        private void OnLoadDone(AsyncOperationHandle<GameObject> handle) {
            if (handle.Status == AsyncOperationStatus.Succeeded) {
                Instantiate(handle.Result);
            }
        }
    }
}