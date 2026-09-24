/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム進行（経過時間）に応じたエネミー出現設定（ウェーブ情報）および出現比率データ定義。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// エネミー出現時の種別ごとの重み（比率）データ。
    /// </summary>
    [Serializable]
    public sealed class EnemySpawnWeight
    {
        [Tooltip("出現させるエネミー種別")]
        public EnemyType enemyType;

        [Tooltip("出現抽選における重み（1以上の整数値）")]
        public int weight = 1;

        /// <summary>エネミー種別</summary>
        public EnemyType EnemyType => enemyType;

        /// <summary>出現抽選の重み</summary>
        public int Weight => weight;

        /// <summary>
        /// デフォルトコンストラクタ（JSONデシリアライズ用）。
        /// </summary>
        public EnemySpawnWeight()
        {
        }

        /// <summary>
        /// EnemySpawnWeight のコンストラクタ。
        /// </summary>
        /// <param name="enemyType">エネミー種別</param>
        /// <param name="weight">出現抽選の重み</param>
        public EnemySpawnWeight(EnemyType enemyType, int weight)
        {
            this.enemyType = enemyType;
            this.weight = Math.Max(1, weight);
        }
    }

    /// <summary>
    /// 指定された時間帯におけるエネミースポーン設定マスターデータ。
    /// </summary>
    [Serializable]
    public sealed class WaveMasterData
    {
        /// <summary>デフォルトのリソース配置パス</summary>
        public const string DefaultResourcePath = "Data/WaveData";

        [Tooltip("ウェーブ識別ID")]
        public int waveId;

        [Tooltip("ウェーブ開始経過時間（秒）")]
        public float startTime;

        [Tooltip("ウェーブ終了経過時間（秒）")]
        public float endTime;

        [Tooltip("エネミースポーン間隔（秒）")]
        public float spawnInterval = 2.0f;

        [Tooltip("フィールド内の最大生存エネミー数")]
        public int maxAliveCount = 10;

        [Tooltip("エネミー出現比率（重み）のリスト")]
        public List<EnemySpawnWeight> enemyWeights = new List<EnemySpawnWeight>();

        /// <summary>ウェーブ識別ID</summary>
        public int WaveId => waveId;

        /// <summary>ウェーブ開始経過時間（秒）</summary>
        public float StartTime => startTime;

        /// <summary>ウェーブ終了経過時間（秒）</summary>
        public float EndTime => endTime;

        /// <summary>エネミースポーン間隔（秒）</summary>
        public float SpawnInterval => spawnInterval;

        /// <summary>フィールド内の最大生存エネミー数</summary>
        public int MaxAliveCount => maxAliveCount;

        /// <summary>エネミー出現比率リスト</summary>
        public IReadOnlyList<EnemySpawnWeight> EnemyWeights => enemyWeights;

        /// <summary>
        /// デフォルトコンストラクタ（JSONデシリアライズ用）。
        /// </summary>
        public WaveMasterData()
        {
        }

        /// <summary>
        /// 指定された経過時間がこのウェーブの対象範囲内であるかを判定する。
        /// </summary>
        /// <param name="elapsedTime">ゲーム開始からの経過時間（秒）</param>
        /// <returns>対象範囲内であれば true</returns>
        public bool IsActive(float elapsedTime)
        {
            return elapsedTime >= startTime && elapsedTime < endTime;
        }

        /// <summary>
        /// Resources から WaveData.json を同期ロードし、全 WaveMasterData のリストを生成する。
        /// </summary>
        /// <param name="path">リソースパス（デフォルト: Data/WaveData）</param>
        /// <returns>WaveMasterData のリスト</returns>
        public static List<WaveMasterData> LoadAllFromResources(string path = DefaultResourcePath)
        {
            var result = new List<WaveMasterData>();
            var textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
            {
                DebugLogger.Error($"[WaveMasterData] '{path}' のロードに失敗しました。");
                return result;
            }

            try
            {
                var container = JsonUtility.FromJson<WaveMasterDataContainer>(textAsset.text);
                if (container != null && container.waves != null && container.waves.Count > 0)
                {
                    result.AddRange(container.waves);
                    DebugLogger.Log($"[WaveMasterData] {result.Count} 件のウェーブ設定をロードしました。");
                }
                else
                {
                    DebugLogger.Warning($"[WaveMasterData] '{path}' のパース結果が空です。");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[WaveMasterData] JSONパースエラー: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// WaveData.json のリスト形式を JsonUtility でデシリアライズするための内部コンテナクラス。
        /// </summary>
        [Serializable]
        private class WaveMasterDataContainer
        {
            // ウェーブデータのリスト
            public List<WaveMasterData> waves = new List<WaveMasterData>();
        }
    }
}
