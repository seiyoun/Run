/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Addressables から Prefab を読み込み、指定 Canvas 配下で生成と解放を管理する。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shiyuan.Foundation.Addressables
{
    public sealed class AddressablePrefabLoader : IDisposable
    {
        private GameObject instance;
        private string loadedAddress;
        private bool hasLoaded;

        /// <summary>
        /// 指定した Addressables キーの Prefab を読み込み、シーン直下に生成する。
        /// </summary>
        public async Task<GameObject> LoadAsync(string address, CancellationToken cancellationToken)
        {
            ValidateAddress(address);
            if (TryGetLoadedInstance(address, out var loadedInstance))
            {
                return loadedInstance;
            }

            instance = await AddressableManager.InstantiatePrefabAsync(
                address,
                Vector3.zero,
                Quaternion.identity,
                null,
                cancellationToken);
            loadedAddress = address;
            hasLoaded = true;

#if DEBUG_LOG && ADDRESSABLE_LOG
            Debug.Log($"Addressable prefab loaded. address:{loadedAddress}");
#endif
            return instance;
        }

        /// <summary>
        /// 指定した Addressables キーの Prefab を読み込み、Canvas 配下に生成する。
        /// </summary>
        public Task<GameObject> LoadAsync(string address, Canvas parentCanvas, CancellationToken cancellationToken)
        {
            if (parentCanvas == null)
            {
                throw new ArgumentNullException(nameof(parentCanvas), "親 Canvas が未設定です。");
            }

            return LoadAsync(address, parentCanvas.transform, cancellationToken);
        }

        /// <summary>
        /// 指定した Addressables キーの Prefab を読み込み、指定 Transform 配下に生成する。
        /// </summary>
        public async Task<GameObject> LoadAsync(string address, Transform parent, CancellationToken cancellationToken)
        {
            ValidateAddress(address);
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent), "親 Transform が未設定です。");
            }

            if (TryGetLoadedInstance(address, out var loadedInstance))
            {
                return loadedInstance;
            }

            instance = await AddressableManager.InstantiatePrefabAsync(address, parent, cancellationToken);
            loadedAddress = address;
            hasLoaded = true;

#if DEBUG_LOG && ADDRESSABLE_LOG
            Debug.Log($"Addressable prefab loaded. address:{loadedAddress}");
#endif
            return instance;
        }

        /// <summary>
        /// 生成した Prefab と Addressables のロードハンドルを解放する。
        /// </summary>
        public void Dispose()
        {
            if (instance != null)
            {
                AddressableManager.ReleaseInstance(instance);
                instance = null;
            }

            loadedAddress = null;
            hasLoaded = false;
        }

        /// <summary>
        /// Addressables キーが有効か確認する。
        /// </summary>
        private static void ValidateAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Addressables のキーが空です。", nameof(address));
            }
        }

        /// <summary>
        /// 既に読み込み済みなら生成済みインスタンスを取得する。
        /// </summary>
        private bool TryGetLoadedInstance(string address, out GameObject loadedInstance)
        {
            if (!hasLoaded)
            {
                loadedInstance = null;
                return false;
            }

            if (!string.Equals(loadedAddress, address, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"別の Addressables キーでロード済みです。 loaded:{loadedAddress} requested:{address}");
            }

            loadedInstance = instance;
            return true;
        }
    }
}
