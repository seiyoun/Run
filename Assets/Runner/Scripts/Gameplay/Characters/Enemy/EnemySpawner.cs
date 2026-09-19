/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IEnemyFactory を利用してプレイヤー周辺へエネミーを生成するシーン限定シングルトンスポナー。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Runner
{
    /// <summary>
    /// エネミーの生成を行うシーン限定シングルトンスポナー。
    /// 外部からの要求に応じてプレイヤー周辺のワールド座標を算出し、エネミーの生成を行います。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : SingletonMonoBehaviour<EnemySpawner>
    {
        private const float DefaultMinRadius = 6.0f;
        private const float DefaultMaxRadius = 10.0f;

        [Header("Spawn Settings")]
        [Tooltip("プレイヤーからの最小スポーン距離")]
        [SerializeField]
        private float spawnRadiusMin = DefaultMinRadius;

        [Tooltip("プレイヤーからの最大スポーン距離")]
        [SerializeField]
        private float spawnRadiusMax = DefaultMaxRadius;

        [Tooltip("スポーンさせる初期エネミー種別")]
        [SerializeField]
        private EnemyType spawnEnemyType = EnemyType.Salaryman;

        private IEnemyFactory enemyFactory;
        private Transform playerTransform;
        private ObjectPool<EnemyController> enemyPool;

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
        /// シングルトンの初期化およびファクトリ・オブジェクトプールの初期生成を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance) return;

            enemyFactory ??= new EnemyFactory();
            InitializePool();
        }

        /// <summary>
        /// 破棄時にファクトリおよびオブジェクトプールのリソースを解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (!IsPrimaryInstance) return;

            if (enemyPool != null)
            {
                enemyPool.Dispose();
                enemyPool = null;
            }

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
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public Task<EnemyController> SpawnEnemyAsync(CancellationToken cancellationToken = default)
        {
            return SpawnEnemyAsync(spawnEnemyType, cancellationToken);
        }

        /// <summary>
        /// 指定されたエネミー種別のエネミーを1体非同期生成（またはプールから再取得）し、初期化する。
        /// </summary>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public async Task<EnemyController> SpawnEnemyAsync(EnemyType enemyType, CancellationToken cancellationToken = default)
        {
            if (enemyFactory == null)
            {
                DebugLogger.Error("[EnemySpawner] EnemyFactory が設定されていません。");
                return null;
            }

            var spawnPos = CalculateSpawnPosition();

            if (enemyPool != null && enemyPool.CountInactive > 0)
            {
                var pooledEnemy = enemyPool.Get();
                if (pooledEnemy != null)
                {
                    if (playerTransform == null)
                    {
                        FindPlayerTransform();
                    }
                    pooledEnemy.ResetForPool(spawnPos, enemyType, playerTransform);
                    return pooledEnemy;
                }
            }

            var newEnemy = await enemyFactory.CreateEnemyAsync(spawnPos, enemyType, cancellationToken);
            return newEnemy;
        }

        /// <summary>
        /// エネミーをオブジェクトプールに返却し、非アクティブ化する。
        /// </summary>
        /// <param name="enemy">返却する EnemyController</param>
        public void ReturnEnemy(EnemyController enemy)
        {
            if (enemy == null) return;

            if (enemyPool != null)
            {
                enemyPool.Release(enemy);
            }
            else
            {
                Destroy(enemy.gameObject);
            }
        }

        /// <summary>
        /// エネミーのオブジェクトプールを初期化する。
        /// </summary>
        private void InitializePool()
        {
            enemyPool = new ObjectPool<EnemyController>(
                createFunc: () =>
                {
                    return enemyFactory?.CreateEnemy(Vector3.zero, spawnEnemyType);
                },
                actionOnGet: enemy =>
                {
                    if (enemy != null)
                    {
                        enemy.gameObject.SetActive(true);
                    }
                },
                actionOnRelease: enemy =>
                {
                    if (enemy != null)
                    {
                        enemy.gameObject.SetActive(false);
                    }
                },
                actionOnDestroy: enemy =>
                {
                    if (enemy != null)
                    {
                        Destroy(enemy.gameObject);
                    }
                },
                collectionCheck: false
            );
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
            var spawnPos = center + offset;
            spawnPos.z = 0f;
            return spawnPos;
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
    }
}
