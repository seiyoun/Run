/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: アイテムオブジェクト（コイン等）を Addressables から非同期ロード・生成し、初期化を行うアイテムスポナークラス。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Addressables からアイテムオブジェクト（コイン等）を非同期生成するシングルトンスポナークラス。
    /// アイテムプレハブのインスタンス化および金額・パラメータのセットアップを一元管理します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemSpawner : SingletonMonoBehaviour<ItemSpawner>
    {
        private const string MoneyItemAddress = "MoneyItem";

        /// <summary>Game シーン破棄時に一緒に破棄させ、確実にリソースを解放する</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<ItemSpawner>.Instance != null;

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
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Addressables からコインアイテムを指定座標に非同期生成し、金額をセットアップする。
        /// </summary>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="amount">コインの獲得金額</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された MoneyItem インスタンス（失敗時は null）</returns>
        public async Task<MoneyItem> SpawnMoneyItemAsync(Vector3 position, long amount, CancellationToken cancellationToken = default)
        {
            try
            {
                var obj = await AddressableManager.InstantiatePrefabAsync(
                    MoneyItemAddress,
                    position,
                    Quaternion.identity,
                    null,
                    cancellationToken);

                if (obj != null)
                {
                    var moneyItem = obj.GetComponent<MoneyItem>();
                    if (moneyItem != null)
                    {
                        moneyItem.Setup(amount);
                        return moneyItem;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ItemSpawner] コインアイテムの生成に失敗しました: {ex.Message}");
            }

            return null;
        }
    }
}

