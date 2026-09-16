/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IEnemyFactory を利用して定期的にプレイヤー周辺へエネミーを生成するスポナークラス（POCO）。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runner
{
    /// <summary>
    /// EnemyFactory を利用してプレイヤー周囲へエネミーを定期出現・管理する純粋な C# スポナークラス。
    /// MonoBehaviour に依存せず、ゲームステートやループから Tick(deltaTime) を呼ぶことで駆動します。
    /// </summary>
    public sealed class EnemySpawner : IDisposable
    {
        private const float DefaultInterval = 3.0f;
        private const float DefaultMinRadius = 6.0f;
        private const float DefaultMaxRadius = 10.0f;
        private const int DefaultMaxActiveEnemies = 15;

        private IEnemyFactory enemyFactory;
        private bool autoSpawn = true;
        private float spawnInterval = DefaultInterval;
        private float spawnRadiusMin = DefaultMinRadius;
        private float spawnRadiusMax = DefaultMaxRadius;
        private int maxActiveEnemies = DefaultMaxActiveEnemies;
        private EnemyType spawnEnemyType = EnemyType.Salaryman;

        private Transform playerTransform;
        private float timer;
        private int activeEnemyCount;

        /// <summary>エネミー生成ファクトリ</summary>
        public IEnemyFactory Factory
        {
            get => enemyFactory;
            set => enemyFactory = value;
        }

        /// <summary>スポーンさせるエネミーの種類</summary>
        public EnemyType SpawnEnemyType
        {
            get => spawnEnemyType;
            set => spawnEnemyType = value;
        }

        /// <summary>スポーン間隔（秒）</summary>
        public float SpawnInterval
        {
            get => spawnInterval;
            set => spawnInterval = Mathf.Max(0.1f, value);
        }

        /// <summary>プレイヤーからの最小スポーン距離</summary>
        public float SpawnRadiusMin
        {
            get => spawnRadiusMin;
            set => spawnRadiusMin = Mathf.Max(0f, value);
        }

        /// <summary>プレイヤーからの最大スポーン距離</summary>
        public float SpawnRadiusMax
        {
            get => spawnRadiusMax;
            set => spawnRadiusMax = Mathf.Max(spawnRadiusMin, value);
        }

        /// <summary>最大生存エネミー数</summary>
        public int MaxActiveEnemies
        {
            get => maxActiveEnemies;
            set => maxActiveEnemies = Mathf.Max(1, value);
        }

        /// <summary>自動スポーンの有効フラグ</summary>
        public bool AutoSpawn
        {
            get => autoSpawn;
            set => autoSpawn = value;
        }

        /// <summary>現在のアクティブエネミー数</summary>
        public int ActiveEnemyCount => activeEnemyCount;

        /// <summary>
        /// EnemySpawner のデフォルトコンストラクタ。
        /// </summary>
        public EnemySpawner() : this(null)
        {
        }

        /// <summary>
        /// EnemySpawner のコンストラクタ。
        /// </summary>
        /// <param name="factory">エネミー生成ファクトリ（null の場合は EnemyFactory を自動生成）</param>
        /// <param name="spawnEnemyType">スポーンさせる初期エネミー種別</param>
        /// <param name="spawnInterval">スポーン間隔（秒）</param>
        /// <param name="spawnRadiusMin">最小スポーン半径</param>
        /// <param name="spawnRadiusMax">最大スポーン半径</param>
        /// <param name="maxActiveEnemies">最大生存エネミー数</param>
        public EnemySpawner(
            IEnemyFactory factory = null,
            EnemyType spawnEnemyType = EnemyType.Salaryman,
            float spawnInterval = DefaultInterval,
            float spawnRadiusMin = DefaultMinRadius,
            float spawnRadiusMax = DefaultMaxRadius,
            int maxActiveEnemies = DefaultMaxActiveEnemies)
        {
            this.enemyFactory = factory ?? new EnemyFactory();
            this.spawnEnemyType = spawnEnemyType;
            this.spawnInterval = spawnInterval;
            this.spawnRadiusMin = spawnRadiusMin;
            this.spawnRadiusMax = spawnRadiusMax;
            this.maxActiveEnemies = maxActiveEnemies;
        }

        /// <summary>
        /// スポナーを破棄し、保持するファクトリのリソースを解放する。
        /// </summary>
        public void Dispose()
        {
            if (enemyFactory is IDisposable disposable)
            {
                disposable.Dispose();
            }

            enemyFactory = null;
            playerTransform = null;
        }

        /// <summary>
        /// ゲームループ（GamePlayingState 等）から毎フレーム呼び出される更新処理。
        /// タイマーを進行させ、一定間隔で自動スポーンを実行します。
        /// </summary>
        /// <param name="deltaTime">経過時間（秒）</param>
        public void Tick(float deltaTime)
        {
            if (!autoSpawn) return;

            if (playerTransform == null)
            {
                FindPlayerTransform();
                if (playerTransform == null) return;
            }

            if (activeEnemyCount >= maxActiveEnemies) return;

            timer += deltaTime;
            if (timer >= spawnInterval)
            {
                timer = 0f;
                _ = SpawnEnemyAsync();
            }
        }

        /// <summary>
        /// 設定された EnemyType のエネミーを1体非同期生成し、初期化する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス（生成失敗時は null）</returns>
        public Task<EnemyController> SpawnEnemyAsync(CancellationToken cancellationToken = default)
        {
            return SpawnEnemyAsync(spawnEnemyType, cancellationToken);
        }

        /// <summary>
        /// 指定された EnemyType のエネミーを1体非同期生成し、パラメータを適用して初期化する。
        /// </summary>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス（生成失敗時は null）</returns>
        public async Task<EnemyController> SpawnEnemyAsync(EnemyType enemyType, CancellationToken cancellationToken = default)
        {
            if (enemyFactory == null)
            {
                DebugLogger.Error("[EnemySpawner] EnemyFactory が設定されていません。");
                return null;
            }

            var spawnPos = CalculateSpawnPosition();
            var enemy = await enemyFactory.CreateEnemyAsync(spawnPos, enemyType, cancellationToken);

            if (enemy != null)
            {
                activeEnemyCount++;
                if (enemy.Status != null)
                {
                    enemy.Status.OnDead += HandleEnemyDead;
                }
            }

            return enemy;
        }

        /// <summary>
        /// プレイヤーの位置を基準にスポーンワールド座標を算出する。
        /// </summary>
        /// <returns>算出されたスポーン座標</returns>
        private Vector3 CalculateSpawnPosition()
        {
            var center = playerTransform != null ? playerTransform.position : Vector3.zero;
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(spawnRadiusMin, spawnRadiusMax);

            var offset = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
            return center + offset;
        }

        /// <summary>
        /// プレイヤーの Transform を検索してキャッシュする。
        /// </summary>
        private void FindPlayerTransform()
        {
            if (PlayerController.Instance != null)
            {
                playerTransform = PlayerController.Instance.transform;
            }
        }

        /// <summary>
        /// エネミー死亡イベントを受信し、アクティブ数を減算する。
        /// </summary>
        private void HandleEnemyDead()
        {
            activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
        }
    }
}
