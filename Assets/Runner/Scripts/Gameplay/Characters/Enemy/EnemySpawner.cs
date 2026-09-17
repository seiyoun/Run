/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IEnemyFactory を利用してプレイヤー周辺へエネミーを生成・管理するシーン限定シングルトンスポナー。
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
    /// エネミーの生成・管理を行うシーン限定シングルトンスポナー。
    /// 外部からの要求に応じてプレイヤー周辺のワールド座標を算出し、エネミーの生成と生存数管理を行います。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : SingletonMonoBehaviour<EnemySpawner>
    {
        private const float DefaultMinRadius = 6.0f;
        private const float DefaultMaxRadius = 10.0f;
        private const int DefaultMaxActiveEnemies = 15;

        [Header("Spawn Settings")]
        [Tooltip("プレイヤーからの最小スポーン距離")]
        [SerializeField]
        private float spawnRadiusMin = DefaultMinRadius;

        [Tooltip("プレイヤーからの最大スポーン距離")]
        [SerializeField]
        private float spawnRadiusMax = DefaultMaxRadius;

        [Tooltip("最大生存エネミー数")]
        [SerializeField]
        private int maxActiveEnemies = DefaultMaxActiveEnemies;

        [Tooltip("スポーンさせる初期エネミー種別")]
        [SerializeField]
        private EnemyType spawnEnemyType = EnemyType.Salaryman;

        private IEnemyFactory enemyFactory;
        private Transform playerTransform;
        private int activeEnemyCount;

        /// <summary>Game シーン破棄時に一緒に破棄させ、確実にリソースを解放する</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<EnemySpawner>.Instance != null;

        /// <summary>
        /// EnemySpawner の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static EnemySpawner Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<EnemySpawner>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<EnemySpawner>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(EnemySpawner));
                return obj.AddComponent<EnemySpawner>();
            }
        }

        /// <summary>
        /// シングルトンの初期化およびファクトリの初期生成を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance) return;

            enemyFactory ??= new EnemyFactory();
        }

        /// <summary>
        /// 破棄時にファクトリのリソースを解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (!IsPrimaryInstance) return;

            if (enemyFactory is IDisposable disposable)
            {
                disposable.Dispose();
            }

            playerTransform = null;
            base.OnDestroy();
        }

        /// <summary>
        /// 設定された初期エネミー種別のエネミーを1体非同期生成し、初期化する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス（上限到達または失敗時は null）</returns>
        public Task<EnemyController> SpawnEnemyAsync(CancellationToken cancellationToken = default)
        {
            return SpawnEnemyAsync(spawnEnemyType, cancellationToken);
        }

        /// <summary>
        /// 指定されたエネミー種別のエネミーを1体非同期生成し、初期化する。
        /// </summary>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス（上限到達または失敗時は null）</returns>
        public async Task<EnemyController> SpawnEnemyAsync(EnemyType enemyType, CancellationToken cancellationToken = default)
        {
            if (activeEnemyCount >= maxActiveEnemies) return null;

            if (enemyFactory == null)
            {
                DebugLogger.Error("[EnemySpawner] EnemyFactory が設定されていません。");
                return null;
            }

            var spawnPos = CalculateSpawnPosition();
            var enemy = await enemyFactory.CreateEnemyAsync(spawnPos, enemyType, cancellationToken);

            if (enemy != null)
            {
                BindEnemyEvents(enemy);
            }

            return enemy;
        }

        /// <summary>
        /// プレイヤーの位置を基準にスポーンワールド座標を算出する。
        /// </summary>
        /// <returns>算出されたスポーン座標</returns>
        private Vector3 CalculateSpawnPosition()
        {
            if (playerTransform == null)
            {
                FindPlayerTransform();
            }

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
            else
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }
        }

        /// <summary>
        /// エネミーの死亡イベントを購読し、生存エネミー数を管理する。
        /// </summary>
        /// <param name="enemy">対象エネミー</param>
        private void BindEnemyEvents(EnemyController enemy)
        {
            if (enemy == null || enemy.Status == null) return;

            activeEnemyCount++;

            void OnDeadHandler()
            {
                activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
                if (enemy != null && enemy.Status != null)
                {
                    enemy.Status.OnDead -= OnDeadHandler;
                }
            }

            enemy.Status.OnDead += OnDeadHandler;
        }
    }
}
