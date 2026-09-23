/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: デバッグ機能用にお金（コイン）アイテムを Addressables からプレイヤー周辺に生成するユーティリティ。
 *                SANDBOX またはエディタ環境でのみ動作します。
 */

#if SANDBOX || UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// デバッグ機能用にお金（コイン）アイテムを Addressables から指定位置周辺に生成するユーティリティクラス。
    /// </summary>
    public static class ItemDebugSpawner
    {
        private const string MoneyItemAddress = "MoneyItem";
        private const float DefaultMinDistance = 2.5f;
        private const float DefaultMaxDistance = 4.5f;
        private const int DefaultSpawnCount = 5;
        private const int DefaultCoinValue = 50;
        /// <summary>
        /// 指定された中心位置の周囲に Addressables からコインアイテムをランダム生成する。
        /// </summary>
        /// <param name="center">生成の中心座標（プレイヤー位置など）</param>
        /// <param name="count">生成数</param>
        /// <param name="minDistance">最小スポーン距離(m)</param>
        /// <param name="maxDistance">最大スポーン距離(m)</param>
        /// <param name="value">コイン1枚あたりの金額/ポイント</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        public static async Task SpawnMoneyItemsAroundAsync(
            Vector3 center,
            int count = DefaultSpawnCount,
            float minDistance = DefaultMinDistance,
            float maxDistance = DefaultMaxDistance,
            int value = DefaultCoinValue,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float dist = UnityEngine.Random.Range(minDistance, maxDistance);
                var spawnPos = center + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);

                try
                {
                    var itemObj = await AddressableManager.InstantiatePrefabAsync(
                        MoneyItemAddress,
                        spawnPos,
                        Quaternion.identity,
                        null,
                        cancellationToken);

                    if (itemObj != null)
                    {
                        var moneyItem = itemObj.GetComponent<MoneyItem>();
                        if (moneyItem != null)
                        {
                            moneyItem.Setup(value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Error($"[ItemDebugSpawner] Addressables ('{MoneyItemAddress}') ロード失敗: {ex.Message}");
                }
            }

            DebugLogger.Log($"[ItemDebugSpawner] コインアイテムを {count} 個生成しました。Center: {center}");
        }
    }
}
#endif
