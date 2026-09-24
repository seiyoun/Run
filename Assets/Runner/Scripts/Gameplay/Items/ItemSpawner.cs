/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: アイテムオブジェクト（コイン等）を Addressables からロードし、オブジェクトプールで再利用管理するアイテムスポナークラス。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Runner
{
    /// <summary>
    /// Addressables からアイテムオブジェクト（コイン等）をロードし、オブジェクトプールで生成・再利用管理を行うシングルトンスポナークラス。
    /// コインアイテムのインスタンス化、金額パラメータのセットアップ、およびプール返却を一元管理します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemSpawner : SingletonMonoBehaviour<ItemSpawner>
    {
        private const string MoneyItemAddress = "MoneyItem";

        private GameObject moneyItemPrefab;
        private bool isAssetLoaded;
        private ObjectPool<MoneyItem> moneyPool;

        /// <summary>Game シーン破棄時に一緒に破棄させ、確実にリソースを解放する</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<ItemSpawner>.Instance != null;

        /// <summary>現在アクティブなコインアイテム数</summary>
        public int ActiveMoneyItemCount => moneyPool != null ? moneyPool.CountActive : 0;

        /// <summary>
        /// ItemSpawner の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static ItemSpawner Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<ItemSpawner>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<ItemSpawner>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(ItemSpawner));
                return obj.AddComponent<ItemSpawner>();
            }
        }

        /// <summary>
        /// シングルトンの初期化およびオブジェクトプールの構築を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance) return;

            InitializePool();
        }

        /// <summary>
        /// 破棄時にオブジェクトプールおよび Addressables プレハブアセットのリソースを解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (!IsPrimaryInstance) return;

            if (moneyPool != null)
            {
                moneyPool.Dispose();
                moneyPool = null;
            }

            if (isAssetLoaded)
            {
                AddressableManager.ReleaseAsset(MoneyItemAddress);
                isAssetLoaded = false;
                moneyItemPrefab = null;
            }

            base.OnDestroy();
        }

        /// <summary>
        /// コインアイテムを指定座標に非同期生成（またはプールから再取得）し、金額をセットアップする。
        /// </summary>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="amount">コインの獲得金額</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された MoneyItem インスタンス（失敗時は null）</returns>
        public async Task<MoneyItem> SpawnMoneyItemAsync(Vector3 position, long amount, CancellationToken cancellationToken = default)
        {
            if (moneyPool != null && moneyPool.CountInactive > 0)
            {
                var pooled = moneyPool.Get();
                if (pooled != null)
                {
                    pooled.ResetForPool(position, amount);
                    if (GameRecordTracker.HasInstance || GameRecordTracker.Instance != null)
                    {
                        GameRecordTracker.Instance.RegisterItem(pooled);
                    }
                    return pooled;
                }
            }

            var prefab = await GetOrLoadPrefabAsync(cancellationToken);
            if (prefab == null) return null;

            var obj = Instantiate(prefab, position, Quaternion.identity);
            var moneyItem = obj.GetComponent<MoneyItem>();
            if (moneyItem != null)
            {
                moneyItem.ResetForPool(position, amount);
                if (GameRecordTracker.HasInstance || GameRecordTracker.Instance != null)
                {
                    GameRecordTracker.Instance.RegisterItem(moneyItem);
                }
                return moneyItem;
            }

            return null;
        }

        /// <summary>
        /// コインアイテムをオブジェクトプールに返却し、非アクティブ化する。
        /// </summary>
        /// <param name="item">返却する MoneyItem</param>
        public void ReturnMoneyItem(MoneyItem item)
        {
            if (item == null) return;

            if (moneyPool != null)
            {
                moneyPool.Release(item);
            }
            else
            {
                Destroy(item.gameObject);
            }
        }

        /// <summary>
        /// コインアイテム用オブジェクトプールを初期化する。
        /// </summary>
        private void InitializePool()
        {
            moneyPool = new ObjectPool<MoneyItem>(
                createFunc: () =>
                {
                    if (moneyItemPrefab != null)
                    {
                        var instance = Instantiate(moneyItemPrefab);
                        return instance.GetComponent<MoneyItem>();
                    }
                    return null;
                },
                actionOnGet: item =>
                {
                    if (item != null)
                    {
                        item.gameObject.SetActive(true);
                    }
                },
                actionOnRelease: item =>
                {
                    if (item != null)
                    {
                        item.gameObject.SetActive(false);
                    }
                },
                actionOnDestroy: item =>
                {
                    if (item != null)
                    {
                        Destroy(item.gameObject);
                    }
                },
                collectionCheck: false
            );
        }

        /// <summary>
        /// コインプレハブを取得する。未ロードの場合は Addressables から非同期ロードしてキャッシュする。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>コインプレハブ GameObject</returns>
        private async Task<GameObject> GetOrLoadPrefabAsync(CancellationToken cancellationToken)
        {
            if (moneyItemPrefab != null) return moneyItemPrefab;

            try
            {
                moneyItemPrefab = await AddressableManager.LoadAssetAsync<GameObject>(MoneyItemAddress, cancellationToken);
                isAssetLoaded = true;
                return moneyItemPrefab;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ItemSpawner] AddressableManager ({MoneyItemAddress}) のロードに失敗しました: {ex.Message}");
                return null;
            }
        }
    }
}
