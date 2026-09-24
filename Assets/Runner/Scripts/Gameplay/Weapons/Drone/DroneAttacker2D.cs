/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ドローンの自動索敵および弾丸発射による攻撃制御コンポーネント。
 */

using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Runner
{
    /// <summary>
    /// ドローン用の自動攻撃コンポーネント。
    /// 一定周期で索敵範囲内の最も近いエネミーを自動検知し、弾丸を発射して攻撃を行います。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DroneAttacker2D : MonoBehaviour, IAttacker, IFindTarget
    {
        private const int DefaultPower = 10;
        private const float DefaultInterval = 1.5f;
        private const float DefaultSearchRadius = 4.0f;
        private const float DefaultRotationSpeed = 720f;
        private const int OverlapBufferSize = 32;

        private static readonly Collider2D[] OverlapResults = new Collider2D[OverlapBufferSize];

        /// <summary>攻撃実行時に発火するイベント</summary>
        public event Action OnAttack;

        [Header("Attack Settings")]
        [Tooltip("発射する弾丸プレハブ")]
        [SerializeField] private DroneBullet bulletPrefab;

        [Tooltip("弾丸の発射起点 Transform")]
        [SerializeField] private Transform firePoint;

        [Tooltip("エネミーを自動検知する最大半径")]
        [SerializeField] private float searchRadius = DefaultSearchRadius;

        [Tooltip("検知対象のレイヤーマスク（エネミーレイヤー）")]
        [SerializeField] private LayerMask targetLayerMask = ~0;

        [Tooltip("基本攻撃力")]
        [SerializeField] private int attackPower = DefaultPower;

        [Tooltip("攻撃間隔（秒）")]
        [SerializeField] private float attackInterval = DefaultInterval;

        [Tooltip("ターゲット追従の回転速度（度/秒）")]
        [SerializeField] private float rotationSpeed = DefaultRotationSpeed;

        private float attackCooldownTimer;
        private ObjectPool<DroneBullet> bulletPool;
        private ContactFilter2D contactFilter;

        /// <summary>攻撃力</summary>
        public int AttackPower
        {
            get => attackPower;
            set => attackPower = Mathf.Max(1, value);
        }

        /// <summary>攻撃間隔（秒）</summary>
        public float AttackInterval
        {
            get => attackInterval;
            set => attackInterval = Mathf.Max(0.05f, value);
        }

        /// <summary>索敵を行う最大半径</summary>
        public float SearchRadius
        {
            get => searchRadius;
            set => searchRadius = Mathf.Max(0f, value);
        }

        /// <summary>現在攻撃可能かどうか</summary>
        public bool CanAttack => attackCooldownTimer <= 0f && bulletPrefab != null;

        /// <summary>
        /// 弾丸用オブジェクトプールおよびコンタクトフィルターを初期化する。
        /// </summary>
        private void Awake()
        {
            contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(targetLayerMask);
            contactFilter.useTriggers = true;
            InitializePool();
        }

        /// <summary>
        /// 毎フレームのクールダウンタイマー更新、向きの追従回転、および自動索敵攻撃を行う。
        /// </summary>
        private void Update()
        {
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= Time.deltaTime;
            }

            var target = FindTarget(transform.position);
            UpdateRotation(target);

            if (CanAttack && target != null)
            {
                Vector2 direction = (target.position - transform.position).normalized;
                Attack(direction);
            }
        }

        /// <summary>
        /// オブジェクトプールを破棄する。
        /// </summary>
        private void OnDestroy()
        {
            if (bulletPool != null)
            {
                bulletPool.Dispose();
                bulletPool = null;
            }
        }

        /// <summary>
        /// 索敵範囲内の最も近いエネミーを検索し、存在する場合はその方向へ攻撃を実行する。
        /// </summary>
        public void Attack()
        {
            var target = FindTarget(transform.position);
            if (target == null) return;

            Vector2 direction = (target.position - transform.position).normalized;
            Attack(direction);
        }

        /// <summary>
        /// 指定された方向へ弾丸を発射し、クールダウンを開始する。
        /// </summary>
        /// <param name="direction">攻撃方向ベクトル</param>
        public void Attack(Vector2 direction)
        {
            if (!CanAttack || direction.sqrMagnitude < 0.001f) return;

            attackCooldownTimer = attackInterval;

            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            var bullet = bulletPool != null ? bulletPool.Get() : Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            bullet.transform.position = spawnPosition;
            bullet.Initialize(direction, attackPower, ReturnBullet);

            OnAttack?.Invoke();
        }

        /// <summary>
        /// 発射した弾丸をオブジェクトプールに返却する。
        /// </summary>
        /// <param name="bullet">返却する弾丸インスタンス</param>
        public void ReturnBullet(DroneBullet bullet)
        {
            if (bullet == null) return;

            if (bulletPool != null)
            {
                bulletPool.Release(bullet);
            }
            else
            {
                Destroy(bullet.gameObject);
            }
        }

        /// <summary>
        /// 指定された基準位置から最も近い生存エネミーを検索して返す（NonAlloc 版）。
        /// </summary>
        /// <param name="origin">索敵の中心ワールド座標</param>
        /// <returns>検知された最も近い生存エネミーの Transform（未検知時は null）</returns>
        public Transform FindTarget(Vector3 origin)
        {
            int count = Physics2D.OverlapCircle(origin, searchRadius, contactFilter, OverlapResults);
            Transform closestEnemy = null;
            float closestDistSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var hit = OverlapResults[i];
                if (hit == null) continue;

                var enemy = hit.GetComponent<EnemyController>() ?? hit.GetComponentInParent<EnemyController>();
                if (enemy != null && !enemy.IsDead)
                {
                    float distSqr = (enemy.transform.position - origin).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closestEnemy = enemy.transform;
                    }
                }
            }

            return closestEnemy;
        }

        /// <summary>
        /// 最も近いエネミーの方向へスムーズに回転する。エネミーが存在しない場合は正面（0度）に戻る。
        /// </summary>
        /// <param name="target">追従回転対象の Transform</param>
        private void UpdateRotation(Transform target)
        {
            float targetAngle = 0f;

            if (target != null)
            {
                Vector2 direction = (target.position - transform.position).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                }
            }

            float currentAngle = transform.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }

        /// <summary>
        /// 弾丸インスタンスを再利用するオブジェクトプールを初期化する。
        /// </summary>
        private void InitializePool()
        {
            bulletPool = new ObjectPool<DroneBullet>(
                createFunc: CreateBullet,
                actionOnGet: OnGetBullet,
                actionOnRelease: OnReleaseBullet,
                actionOnDestroy: OnDestroyBullet,
                collectionCheck: false
            );
        }

        /// <summary>
        /// オブジェクトプール用の新しい弾丸インスタンスを生成する。
        /// </summary>
        /// <returns>生成された DroneBullet インスタンス</returns>
        private DroneBullet CreateBullet()
        {
            return Instantiate(bulletPrefab);
        }

        /// <summary>
        /// プールから取得された弾丸をアクティブ化する。
        /// </summary>
        /// <param name="bullet">対象の弾丸インスタンス</param>
        private void OnGetBullet(DroneBullet bullet)
        {
            if (bullet != null)
            {
                bullet.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// プールに返却された弾丸を非アクティブ化する。
        /// </summary>
        /// <param name="bullet">対象の弾丸インスタンス</param>
        private void OnReleaseBullet(DroneBullet bullet)
        {
            if (bullet != null)
            {
                bullet.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// プール破棄時に弾丸インスタンスを破棄する。
        /// </summary>
        /// <param name="bullet">対象の弾丸インスタンス</param>
        private void OnDestroyBullet(DroneBullet bullet)
        {
            if (bullet != null)
            {
                Destroy(bullet.gameObject);
            }
        }
    }
}

