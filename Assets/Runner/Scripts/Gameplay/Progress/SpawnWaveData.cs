/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム進行（経過時間）に応じたエネミー出現設定（ウェーブ情報）および出現比率データ定義。
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    /// 指定された時間帯におけるエネミースポーン設定データ。
    /// </summary>
    [Serializable]
    public sealed class SpawnWaveData
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
        public SpawnWaveData()
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
        /// Resources から WaveData.json を非同期ロードし、全 SpawnWaveData のリストを生成する。
        /// </summary>
        /// <param name="path">リソースパス（デフォルト: Data/WaveData）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>SpawnWaveData のリスト</returns>
        public static async Task<List<SpawnWaveData>> LoadAllFromResourcesAsync(string path = DefaultResourcePath, CancellationToken cancellationToken = default)
        {
            var result = new List<SpawnWaveData>();
            var request = Resources.LoadAsync<TextAsset>(path);
            while (!request.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            var jsonAsset = request.asset as TextAsset;
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[SpawnWaveData] '{path}' のロードに失敗しました。");
                return result;
            }

            try
            {
                var container = JsonUtility.FromJson<SpawnWaveDataContainer>(jsonAsset.text);
                if (container != null && container.waves != null && container.waves.Count > 0)
                {
                    result.AddRange(container.waves);
                    DebugLogger.Log($"[SpawnWaveData] {result.Count} 件のウェーブ設定をロードしました。");
                }
                else
                {
                    DebugLogger.Warning($"[SpawnWaveData] '{path}' のパース結果が空です。");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[SpawnWaveData] JSONパースエラー: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 指定された waveId リストに合致するウェーブ設定データを Resources から非同期ロードして抽出する。
        /// </summary>
        /// <param name="waveIds">抽出対象の waveId リスト</param>
        /// <param name="path">リソースパス（デフォルト: Data/WaveData）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>合致した SpawnWaveData のリスト</returns>
        public static async Task<List<SpawnWaveData>> LoadByIdsAsync(IReadOnlyList<int> waveIds, string path = DefaultResourcePath, CancellationToken cancellationToken = default)
        {
            var allWaves = await LoadAllFromResourcesAsync(path, cancellationToken);
            if (waveIds == null || waveIds.Count == 0)
            {
                return allWaves;
            }

            var idSet = new HashSet<int>(waveIds);
            var filtered = new List<SpawnWaveData>();
            foreach (var wave in allWaves)
            {
                if (idSet.Contains(wave.WaveId))
                {
                    filtered.Add(wave);
                }
            }

            return filtered;
        }

        /// <summary>
        /// WaveData.json のリスト形式を JsonUtility でデシリアライズするための内部コンテナクラス。
        /// </summary>
        [Serializable]
        private class SpawnWaveDataContainer
        {
            // ウェーブデータのリスト
            public List<SpawnWaveData> waves = new List<SpawnWaveData>();
        }
    }
}

