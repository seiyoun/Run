/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: GameProgressManager のウェーブ設定とイベントを監視し、時間帯に応じたエネミー定期生成を統括するディレクター。
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runner
{
    /// <summary>
    /// GameProgressManager からのウェーブ情報イベント（出現間隔・重み比率・上限数）を受け取り、
    /// タイマー管理および重み付き抽選を行って EnemySpawner へ生成指示を出すディレクター。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawnDirector : SingletonMonoBehaviour<EnemySpawnDirector>
    {
        [Tooltip("初回スポーンまでの待機時間（秒）")]
        [SerializeField] private float initialSpawnDelay = 1.0f;

        private SpawnWaveData currentWave;
        private float spawnTimer;
        private bool isSpawningActive;
        private bool isSpawningInProgress;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<EnemySpawnDirector>.Instance != null;

        /// <summary>EnemySpawnDirector の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。</summary>
        public new static EnemySpawnDirector Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<EnemySpawnDirector>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<EnemySpawnDirector>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(EnemySpawnDirector));
                return obj.AddComponent<EnemySpawnDirector>();
            }
        }

        /// <summary>現在エネミースポーンが稼働中であるか</summary>
        public bool IsSpawningActive => isSpawningActive;

        /// <summary>現在適用中のウェーブ設定データ</summary>
        public SpawnWaveData CurrentWave => currentWave;

        /// <summary>
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 毎フレームのスポーンタイマーを更新し、間隔到達時にスポーン処理をトリガーする。
        /// </summary>
        private void Update()
        {
            if (!isSpawningActive || currentWave == null) return;
            if (GameProgressManager.Instance != null && !GameProgressManager.Instance.IsProgressing) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= currentWave.SpawnInterval)
            {
                spawnTimer = 0f;
                TriggerSpawnAsync();
            }
        }

        /// <summary>
        /// 破棄時に購読解除および停止処理を行う。
        /// </summary>
        protected override void OnDestroy()
        {
            StopSpawning();
            base.OnDestroy();
        }

        /// <summary>Game シーン破棄時に一緒に破棄させる</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>
        /// エネミーの自動定期スポーンを開始し、GameProgressManager のウェーブイベントを購読する。
        /// </summary>
        public void StartSpawning()
        {
            if (isSpawningActive) return;

            isSpawningActive = true;
            spawnTimer = currentWave != null ? Mathf.Max(0f, currentWave.SpawnInterval - initialSpawnDelay) : 0f;

            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.OnWaveChanged += HandleWaveChanged;
                if (GameProgressManager.Instance.CurrentWave != null)
                {
                    HandleWaveChanged(GameProgressManager.Instance.CurrentWave);
                }
            }

            DebugLogger.Log("[EnemySpawnDirector] エネミーの定期出現制御を開始しました。");
        }

        /// <summary>
        /// エネミーの自動定期スポーンを停止し、イベント購読を解除する。
        /// </summary>
        public void StopSpawning()
        {
            if (!isSpawningActive) return;

            isSpawningActive = false;
            currentWave = null;

            if (GameProgressManager.HasInstance && GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.OnWaveChanged -= HandleWaveChanged;
            }

            DebugLogger.Log("[EnemySpawnDirector] エネミーの定期出現制御を停止しました。");
        }

        /// <summary>
        /// ウェーブ切り替わりイベントのハンドラ。最新のスポーン設定を適用する。
        /// </summary>
        /// <param name="newWave">切り替わった新しいウェーブデータ</param>
        private void HandleWaveChanged(SpawnWaveData newWave)
        {
            currentWave = newWave;
            if (currentWave != null)
            {
                DebugLogger.Log($"[EnemySpawnDirector] 新しいウェーブ設定を受信しました。間隔: {currentWave.SpawnInterval}s, 最大生存数: {currentWave.MaxAliveCount}");
            }
        }

        /// <summary>
        /// 生存数上限を確認し、重み付き抽選を行ってエネミーを1体非同期生成する。
        /// </summary>
        private async void TriggerSpawnAsync()
        {
            if (isSpawningInProgress || currentWave == null) return;

            if (EnemySpawner.Instance != null && EnemySpawner.Instance.ActiveEnemyCount >= currentWave.MaxAliveCount)
            {
                return;
            }

            var chosenType = SelectRandomEnemyType(currentWave.EnemyWeights);

            isSpawningInProgress = true;
            try
            {
                if (EnemySpawner.Instance != null)
                {
                    await EnemySpawner.Instance.SpawnEnemyAsync(chosenType, destroyCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // オブジェクト破棄に伴う正常なキャンセル
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[EnemySpawnDirector] エネミースポーン実行中に例外が発生しました: {ex.Message}");
            }
            finally
            {
                isSpawningInProgress = false;
            }
        }

        /// <summary>
        /// 出現重み設定リストに基づき、重み付きランダム抽選でエネミー種別を決定する。
        /// </summary>
        /// <param name="weights">エネミー出現重みリスト</param>
        /// <returns>抽選されたエネミー種別</returns>
        private EnemyType SelectRandomEnemyType(IReadOnlyList<EnemySpawnWeight> weights)
        {
            if (weights == null || weights.Count == 0)
            {
                return EnemyType.Salaryman;
            }

            int totalWeight = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += weights[i].Weight;
            }

            if (totalWeight <= 0)
            {
                return weights[0].EnemyType;
            }

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulative += weights[i].Weight;
                if (roll < cumulative)
                {
                    return weights[i].EnemyType;
                }
            }

            return weights[0].EnemyType;
        }
    }
}

