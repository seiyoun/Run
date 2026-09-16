/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム開始時にマスターデータ（エネミーデータ、プレイヤーデータ等）をロードし、メモリ上にパラメータ設定をキャッシュするマスタデータマネージャ。
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲーム全体のマスターデータ（エネミー、プレイヤー等）の事前キャッシュと高速取得を担う静的マネージャクラス。
    /// ゲーム開始時に JSON パラメータの一括ロードを行い、画像等の重いアセットは保持しません。
    /// </summary>
    public static class MasterDataManager
    {
        private const string EnemyDataResourcePath = "Data/EnemyData";
        private const string PlayerDataResourcePath = "Data/PlayerData";

        private static readonly Dictionary<EnemyType, EnemyData> EnemyDataCache = new Dictionary<EnemyType, EnemyData>();
        private static PlayerData cachedPlayerData;
        private static bool isInitialized;

        /// <summary>初期化およびキャッシュが完了しているか</summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>キャッシュされたプレイヤーデータ</summary>
        public static PlayerData PlayerData => GetPlayerData();

        /// <summary>
        /// 全マスターデータ（EnemyData, PlayerData 等）を同期的にロードしてキャッシュする。
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;

            LoadEnemyData();
            LoadPlayerData();
            isInitialized = true;
        }

        /// <summary>
        /// ゲーム初期化ステート等から呼び出される非同期ロード初期化処理。
        /// </summary>
        /// <returns>非同期タスク</returns>
        public static async Task InitializeAsync()
        {
            if (isInitialized) return;

            Initialize();
            await Task.Yield();
        }

        /// <summary>
        /// 指定されたエネミー種別に対応する EnemyData を取得する。
        /// </summary>
        /// <param name="enemyType">取得対象のエネミー種別</param>
        /// <returns>キャッシュされた EnemyData（未登録時はデフォルトデータ）</returns>
        public static EnemyData GetEnemyData(EnemyType enemyType)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (EnemyDataCache.TryGetValue(enemyType, out var data))
            {
                return data;
            }

            DebugLogger.Error($"[MasterDataManager] EnemyType '{enemyType}' のデータが見つかりません。デフォルト値を返します。");
            return new EnemyData();
        }

        /// <summary>
        /// キャッシュされたプレイヤーデータを取得する。
        /// </summary>
        /// <returns>キャッシュされた PlayerData（未登録時はデフォルトデータ）</returns>
        public static PlayerData GetPlayerData()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (cachedPlayerData != null)
            {
                return cachedPlayerData;
            }

            DebugLogger.Warning("[MasterDataManager] PlayerData のキャッシュが存在しません。デフォルト値を生成します。");
            cachedPlayerData = new PlayerData();
            return cachedPlayerData;
        }

        /// <summary>
        /// EnemyData.json を Resources から読み込みキャッシュに格納する。
        /// </summary>
        private static void LoadEnemyData()
        {
            EnemyDataCache.Clear();

            var jsonAsset = Resources.Load<TextAsset>(EnemyDataResourcePath);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[MasterDataManager] '{EnemyDataResourcePath}' のロードに失敗しました。");
                return;
            }

            try
            {
                var dataList = JsonUtility.FromJson<EnemyDataList>(jsonAsset.text);
                if (dataList != null && dataList.enemies != null && dataList.enemies.Count > 0)
                {
                    foreach (var enemy in dataList.enemies)
                    {
                        EnemyDataCache[enemy.Type] = enemy;
                    }

                    DebugLogger.Log($"[MasterDataManager] {EnemyDataCache.Count} 種類のエネミーパラメータをキャッシュしました。");
                }
                else
                {
                    var singleData = JsonUtility.FromJson<EnemyData>(jsonAsset.text);
                    if (singleData != null)
                    {
                        EnemyDataCache[singleData.Type] = singleData;
                        DebugLogger.Log("[MasterDataManager] 単一エネミーパラメータをキャッシュしました。");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[MasterDataManager] EnemyData JSONパースエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// PlayerData.json を Resources から読み込みキャッシュに格納する。
        /// </summary>
        private static void LoadPlayerData()
        {
            var jsonAsset = Resources.Load<TextAsset>(PlayerDataResourcePath);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[MasterDataManager] '{PlayerDataResourcePath}' のロードに失敗しました。デフォルト値を使用します。");
                cachedPlayerData = new PlayerData();
                return;
            }

            try
            {
                cachedPlayerData = PlayerData.FromJson(jsonAsset.text);
                DebugLogger.Log($"[MasterDataManager] PlayerData をキャッシュしました: HP={cachedPlayerData.maxHp}, Speed={cachedPlayerData.moveSpeed}");
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[MasterDataManager] PlayerData JSONパースエラー: {ex.Message}");
                cachedPlayerData = new PlayerData();
            }
        }
    }
}
