/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Addressables のロード、生成、参照解放を一元管理する。
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = UnityEngine.Debug;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Shiyuan.Foundation.Addressables
{
    public static class AddressableManager
    {
        private sealed class AssetEntry
        {
            public AsyncOperationHandle Handle { get; set; }
            public Type AssetType { get; set; }
            public int ReferenceCount { get; set; }
        }

        private static readonly Dictionary<string, AssetEntry> assetEntries = new();
        private static readonly Dictionary<GameObject, string> instanceAddresses = new();

    /// <summary>
    /// 指定したアドレスのアセットを読み込み、参照カウントを増やして返す。
    /// </summary>
        public static async Task<T> LoadAssetAsync<T>(string address, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Addressables のアドレスが空です。", nameof(address));
            }

            var entry = GetOrCreateEntry<T>(address);
            entry.ReferenceCount++;

            try
            {
                while (!entry.Handle.IsDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                if (entry.Handle.Status != AsyncOperationStatus.Succeeded || entry.Handle.Result == null)
                {
                    ReleaseAsset(address);
                    throw new InvalidOperationException($"Addressable asset load failed. address:{address}");
                }

                if (entry.Handle.Result is not T asset)
                {
                    ReleaseAsset(address);
                    throw new InvalidCastException($"Addressable asset type mismatch. address:{address} expected:{typeof(T).Name} actual:{entry.Handle.Result.GetType().Name}");
                }

#if DEBUG_LOG && ADDRESSABLE_LOG
                Debug.Log($"Addressable asset loaded. address:{address} refCount:{entry.ReferenceCount}");
#endif
                return asset;
            }
            catch
            {
                ReleaseAsset(address);
                throw;
            }
        }

    /// <summary>
    /// 指定したアドレスの Prefab を読み込み、シーン上に生成する。
    /// </summary>
        public static async Task<GameObject> InstantiatePrefabAsync(
            string address,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            CancellationToken cancellationToken)
        {
            var prefab = await LoadAssetAsync<GameObject>(address, cancellationToken);
            var instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            instance.name = $"{prefab.name}_Instance";
            instanceAddresses[instance] = address;
            return instance;
        }

    /// <summary>
    /// 指定したアドレスの Prefab を読み込み、指定 Transform 配下に生成する。
    /// </summary>
        public static async Task<GameObject> InstantiatePrefabAsync(
            string address,
            Transform parent,
            CancellationToken cancellationToken)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent), "親 Transform が未設定です。");
            }

            var prefab = await LoadAssetAsync<GameObject>(address, cancellationToken);
            var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.name = $"{prefab.name}_Instance";
            instanceAddresses[instance] = address;
            return instance;
        }

    /// <summary>
    /// 指定したアドレスの参照カウントを減らし、不要になったロードハンドルを解放する。
    /// </summary>
        public static void ReleaseAsset(string address)
        {
            if (string.IsNullOrWhiteSpace(address) || !assetEntries.TryGetValue(address, out var entry))
            {
                return;
            }

            entry.ReferenceCount = Math.Max(0, entry.ReferenceCount - 1);
            RemoveEntryIfUnused(address, entry);
        }

    /// <summary>
    /// マネージャー経由で生成したインスタンスを破棄し、対応するアセット参照を解放する。
    /// </summary>
        public static void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instanceAddresses.Remove(instance, out var address))
            {
                UnityEngine.Object.Destroy(instance);
                ReleaseAsset(address);
                return;
            }

            UnityEngine.Object.Destroy(instance);
        }

    /// <summary>
    /// 管理中のインスタンスとロードハンドルをすべて解放する。
    /// </summary>
        public static void ReleaseAll()
        {
            foreach (var instance in instanceAddresses.Keys)
            {
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }

            instanceAddresses.Clear();

            foreach (var entry in assetEntries.Values)
            {
                if (entry.Handle.IsValid())
                {
                    UnityAddressables.Release(entry.Handle);
                }
            }

            assetEntries.Clear();
        }

    /// <summary>
    /// 指定したアドレスのロード情報を取得し、未登録なら新しくロードを開始する。
    /// </summary>
        private static AssetEntry GetOrCreateEntry<T>(string address) where T : UnityEngine.Object
        {
            if (assetEntries.TryGetValue(address, out var entry))
            {
                if (entry.AssetType != typeof(T))
                {
                    throw new InvalidOperationException($"Addressable asset is already loaded with another type. address:{address} loaded:{entry.AssetType.Name} requested:{typeof(T).Name}");
                }

                return entry;
            }

            var handle = UnityAddressables.LoadAssetAsync<T>(address);
            entry = new AssetEntry
            {
                Handle = handle,
                AssetType = typeof(T),
                ReferenceCount = 0
            };
            assetEntries.Add(address, entry);
            return entry;
        }

    /// <summary>
    /// 参照されていないロードハンドルを Addressables へ返却し、管理対象から外す。
    /// </summary>
        private static void RemoveEntryIfUnused(string address, AssetEntry entry)
        {
            if (entry.ReferenceCount > 0)
            {
                return;
            }

            if (entry.Handle.IsValid())
            {
                UnityAddressables.Release(entry.Handle);
            }

            assetEntries.Remove(address);
#if DEBUG_LOG && ADDRESSABLE_LOG
            Debug.Log($"Addressable asset released. address:{address}");
#endif
        }
    }
}
