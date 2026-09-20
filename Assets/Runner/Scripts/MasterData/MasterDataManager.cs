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
        private static readonly Dictionary<EnemyType, EnemyMasterData> EnemyDataCache = new Dictionary<EnemyType, EnemyMasterData>();
        private static readonly Dictionary<int, StageMasterData> StageDataCache = new Dictionary<int, StageMasterData>();
        private static readonly List<ShopItemData> ShopItemDataCache = new List<ShopItemData>();
        private static PlayerMasterData cachedPlayerData;
        private static bool isInitialized;

        /// <summary>キャッシュされた全ショップアイテムマスターデータのコレクション</summary>
        public static IReadOnlyList<ShopItemData> AllShopItemData => ShopItemDataCache;

        /// <summary>
        /// ゲーム起動時に全マスターデータを非同期的にロードしてキャッシュする。
        /// </summary>
        /// <returns>非同期タスク</returns>
        public static async Task InitializeAsync()
        {
            if (isInitialized) return;

            Initialize();
            await Task.Yield();
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
            cachedPlayerData = PlayerMasterData.LoadFromResources();
            return cachedPlayerData;
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
        /// 指定されたステージ番号に対応する StageMasterData を取得する。
        /// </summary>
        /// <param name="stageId">ステージ識別番号</param>
        /// <returns>キャッシュされた StageMasterData（未登録時はデフォルトデータ）</returns>
        public static StageMasterData GetStageMasterData(int stageId)
        {
            if (StageDataCache.TryGetValue(stageId, out var data))
            {
                return data;
            }

            DebugLogger.Warning($"[MasterDataManager] StageId '{stageId}' のデータが見つかりません。デフォルトステージデータを返します。");
            if (StageDataCache.TryGetValue(1, out var defaultStage))
            {
                return defaultStage;
            }

            return new StageMasterData();
        }

        /// <summary>
        /// 全マスターデータ（EnemyMasterData, PlayerMasterData, ShopItemData, StageMasterData）を同期的にロードしてキャッシュする。
        /// </summary>
        private static void Initialize()
        {
            cachedPlayerData = PlayerMasterData.LoadFromResources();

            EnemyDataCache.Clear();
            foreach (var enemy in EnemyMasterData.LoadAllFromResources())
            {
                EnemyDataCache[enemy.Type] = enemy;
            }

            ShopItemDataCache.Clear();
            ShopItemDataCache.AddRange(ShopItemData.LoadAllFromResources());

            StageDataCache.Clear();
            foreach (var stage in StageMasterData.LoadAllFromResources())
            {
                StageDataCache[stage.StageId] = stage;
            }

            isInitialized = true;
        }
    }
}
