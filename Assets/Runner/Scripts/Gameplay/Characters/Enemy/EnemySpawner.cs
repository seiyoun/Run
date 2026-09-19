/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Addressables からエネミープレハブをロード・生成し、オブジェクトプールで再利用管理するシーン限定シングルトンスポナー。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Runner
{
    /// <summary>
    /// エネミーの生成およびオブジェクトプールによる再利用管理を行うシーン限定シングルトンスポナー。
    /// 外部からの要求に応じてプレイヤー周辺のワールド座標を算出し、エネミーの生成を行います。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : SingletonMonoBehaviour<EnemySpawner>
    {
        private const string EnemyAddress = "Enemy";
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

        private GameObject enemyPrefab;
        private bool isAssetLoaded;
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
        /// シングルトンの初期化およびオブジェクトプールの初期生成を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance) return;

            InitializePool();
        }

        /// <summary>
        /// 破棄時にオブジェクトプールおよび Addressables プレハブアセットのリソースを解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (!IsPrimaryInstance) return;

            if (enemyPool != null)
            {
                enemyPool.Dispose();
                enemyPool = null;
            }

            if (isAssetLoaded)
            {
                AddressableManager.ReleaseAsset(EnemyAddress);
                isAssetLoaded = false;
                enemyPrefab = null;
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

            var prefab = await GetOrLoadPrefabAsync(cancellationToken);
            if (prefab == null)
            {
                DebugLogger.Error("[EnemySpawner] 生成対象のエネミープレハブのロードに失敗しました。");
                return null;
            }

            var newEnemy = InstantiateEnemy(prefab, spawnPos, enemyType);
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
                    if (enemyPrefab != null)
                    {
                        return InstantiateEnemy(enemyPrefab, Vector3.zero, spawnEnemyType);
                    }
                    return null;
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
        /// プレハブからエネミーをインスタンス化し、マスターデータを適用して初期化する。
        /// </summary>
        /// <param name="prefab">対象プレハブ GameObject</param>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="enemyType">エネミー種別</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        private EnemyController InstantiateEnemy(GameObject prefab, Vector3 position, EnemyType enemyType)
        {
            var instanceObj = Instantiate(prefab, position, Quaternion.identity);
            var enemyController = instanceObj.GetComponent<EnemyController>();
            if (enemyController == null)
            {
                DebugLogger.Error("[EnemySpawner] 生成されたプレハブに EnemyController がアタッチされていません。");
                return null;
            }

            if (playerTransform == null)
            {
                FindPlayerTransform();
            }

            var data = MasterDataManager.GetEnemyMasterData(enemyType);
            enemyController.ApplyData(data);
            enemyController.SetTarget(playerTransform);

            DebugLogger.Log($"[EnemySpawner] エネミーを生成しました: {enemyController.name} (Type: {enemyType}, Position: {position})");
            return enemyController;
        }

        /// <summary>
        /// エネミープレハブを取得する。未ロードの場合は AddressableManager から非同期ロードしてキャッシュする。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>エネミープレハブ GameObject</returns>
        private async Task<GameObject> GetOrLoadPrefabAsync(CancellationToken cancellationToken)
        {
            if (enemyPrefab != null) return enemyPrefab;

            try
            {
                enemyPrefab = await AddressableManager.LoadAssetAsync<GameObject>(EnemyAddress, cancellationToken);
                isAssetLoaded = true;
                return enemyPrefab;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[EnemySpawner] AddressableManager ({EnemyAddress}) のロードに失敗しました: {ex.Message}");
                return null;
            }
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
