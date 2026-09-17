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

        private static readonly Dictionary<EnemyType, EnemyMasterData> EnemyDataCache = new Dictionary<EnemyType, EnemyMasterData>();
        private static PlayerMasterData cachedPlayerData;
        private static bool isInitialized;

        /// <summary>初期化およびキャッシュが完了しているか</summary>
        public static bool IsInitialized => isInitialized;

        /// <summary>キャッシュされたプレイヤーマスターデータ</summary>
        public static PlayerMasterData PlayerMasterData => GetPlayerMasterData();

        /// <summary>キャッシュされた全エネミーマスターデータのコレクション</summary>
        public static IEnumerable<EnemyMasterData> AllEnemyMasterData => EnemyDataCache.Values;

        /// <summary>
        /// 全マスターデータ（EnemyMasterData, PlayerMasterData 等）を同期的にロードしてキャッシュする。
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;

            LoadEnemyMasterData();
            LoadPlayerMasterData();
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
        /// 指定されたエネミー種別に対応する EnemyMasterData を取得する。
        /// </summary>
        /// <param name="enemyType">取得対象のエネミー種別</param>
        /// <returns>キャッシュされた EnemyMasterData（未登録時はデフォルトデータ）</returns>
        public static EnemyMasterData GetEnemyMasterData(EnemyType enemyType)
        {
            if (EnemyDataCache.TryGetValue(enemyType, out var data))
            {
                return data;
            }

            DebugLogger.Error($"[MasterDataManager] EnemyType '{enemyType}' のデータが見つかりません。デフォルト値を返します。");
            return new EnemyMasterData();
        }

        /// <summary>
        /// 指定されたエネミー種別に対応する EnemyMasterData を取得する（GetEnemyMasterData へのエイリアス）。
        /// </summary>
        /// <param name="enemyType">取得対象のエネミー種別</param>
        /// <returns>キャッシュされた EnemyMasterData</returns>
        public static EnemyMasterData GetEnemyData(EnemyType enemyType)
        {
            return GetEnemyMasterData(enemyType);
        }

        /// <summary>
        /// キャッシュされたプレイヤーマスターデータを取得する。
        /// </summary>
        /// <returns>キャッシュされた PlayerMasterData（未登録時はデフォルトデータ）</returns>
        public static PlayerMasterData GetPlayerMasterData()
        {
            if (cachedPlayerData != null)
            {
                return cachedPlayerData;
            }

            DebugLogger.Warning("[MasterDataManager] PlayerMasterData のキャッシュが存在しません。デフォルト値を生成します。");
            cachedPlayerData = new PlayerMasterData();
            return cachedPlayerData;
        }

        /// <summary>
        /// キャッシュされたプレイヤーマスターデータを取得する（GetPlayerMasterData へのエイリアス）。
        /// </summary>
        /// <returns>キャッシュされた PlayerMasterData</returns>
        public static PlayerMasterData GetPlayerData()
        {
            return GetPlayerMasterData();
        }

        /// <summary>
        /// EnemyData.json を Resources から読み込みキャッシュに格納する。
        /// </summary>
        private static void LoadEnemyMasterData()
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
                var container = JsonUtility.FromJson<EnemyMasterDataContainer>(jsonAsset.text);
                if (container != null && container.enemies != null && container.enemies.Count > 0)
                {
                    foreach (var enemy in container.enemies)
                    {
                        EnemyDataCache[enemy.Type] = enemy;
                    }

                    DebugLogger.Log($"[MasterDataManager] {EnemyDataCache.Count} 種類のエネミーパラメータをキャッシュしました。");
                }
                else
                {
                    var singleData = JsonUtility.FromJson<EnemyMasterData>(jsonAsset.text);
                    if (singleData != null)
                    {
                        EnemyDataCache[singleData.Type] = singleData;
                        DebugLogger.Log("[MasterDataManager] 単一エネミーパラメータをキャッシュしました。");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[MasterDataManager] EnemyMasterData JSONパースエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// PlayerData.json を Resources から読み込みキャッシュに格納する。
        /// </summary>
        private static void LoadPlayerMasterData()
        {
            var jsonAsset = Resources.Load<TextAsset>(PlayerDataResourcePath);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[MasterDataManager] '{PlayerDataResourcePath}' のロードに失敗しました。デフォルト値を使用します。");
                cachedPlayerData = new PlayerMasterData();
                return;
            }

            try
            {
                cachedPlayerData = PlayerMasterData.FromJson(jsonAsset.text);
                DebugLogger.Log($"[MasterDataManager] PlayerMasterData をキャッシュしました: HP={cachedPlayerData.maxHp}, Speed={cachedPlayerData.moveSpeed}");
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[MasterDataManager] PlayerMasterData JSONパースエラー: {ex.Message}");
                cachedPlayerData = new PlayerMasterData();
            }
        }

        /// <summary>
        /// EnemyData.json のリスト形式（ルートオブジェクト）を JsonUtility でデシリアライズするための内部コンテナクラス。
        /// </summary>
        [Serializable]
        private class EnemyMasterDataContainer
        {
            [Tooltip("エネミーマスターデータのリスト")]
            public List<EnemyMasterData> enemies = new List<EnemyMasterData>();
        }
    }
}
