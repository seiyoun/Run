/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IEnemyFactory を利用して定期的にプレイヤー周辺へエネミーを生成するスポナークラス。
 */

using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// EnemyFactory を利用してプレイヤー周囲へエネミーを定期出現・管理するスポナーコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        private const float DefaultInterval = 3.0f;
        private const float DefaultMinRadius = 6.0f;
        private const float DefaultMaxRadius = 10.0f;

        [Header("Factory Reference")]
        [Tooltip("エネミー生成を担当するファクトリ")]
        [SerializeField]
        private EnemyFactory enemyFactory;

        [Header("Spawn Settings")]
        [Tooltip("自動スポーンを有効にするか")]
        [SerializeField]
        private bool autoSpawn = true;

        [Tooltip("スポーン間隔（秒）")]
        [SerializeField]
        private float spawnInterval = DefaultInterval;

        [Tooltip("プレイヤーからの最小スポーン距離")]
        [SerializeField]
        private float spawnRadiusMin = DefaultMinRadius;

        [Tooltip("プレイヤーからの最大スポーン距離")]
        [SerializeField]
        private float spawnRadiusMax = DefaultMaxRadius;

        [Tooltip("最大生存エネミー数")]
        [SerializeField]
        private int maxActiveEnemies = 15;

        private Transform playerTransform;
        private float timer;
        private int activeEnemyCount;

        /// <summary>スポーン間隔（秒）</summary>
        public float SpawnInterval
        {
            get => spawnInterval;
            set => spawnInterval = Mathf.Max(0.1f, value);
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
        /// ファクトリコンポーネントの参照取得と初期化を行う。
        /// </summary>
        private void Awake()
        {
            if (enemyFactory == null)
            {
                enemyFactory = GetComponent<EnemyFactory>();
            }
        }

        /// <summary>
        /// 初回フレームでプレイヤー参照の検索を行う。
        /// </summary>
        private void Start()
        {
            FindPlayerTransform();
        }

        /// <summary>
        /// 毎フレームのスポーンタイマー更新と生成処理を行う。
        /// </summary>
        private void Update()
        {
            if (!autoSpawn) return;

            if (playerTransform == null)
            {
                FindPlayerTransform();
                if (playerTransform == null) return;
            }

            if (activeEnemyCount >= maxActiveEnemies) return;

            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                timer = 0f;
                SpawnEnemy();
            }
        }

        /// <summary>
        /// エネミーを1体生成し、初期化する。
        /// </summary>
        /// <returns>生成された EnemyController インスタンス（生成失敗時は null）</returns>
        public EnemyController SpawnEnemy()
        {
            if (enemyFactory == null)
            {
                DebugLogger.Error("[EnemySpawner] EnemyFactory が設定されていません。");
                return null;
            }

            var spawnPos = CalculateSpawnPosition();
            var enemy = enemyFactory.CreateEnemy(spawnPos);

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
            var center = playerTransform != null ? playerTransform.position : transform.position;
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var distance = Random.Range(spawnRadiusMin, spawnRadiusMax);

            var offset = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
            return center + offset;
        }

        /// <summary>
        /// プレイヤーの Transform を検索してキャッシュする（静的インスタンス参照による O(1) 軽量アクセス）。
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

