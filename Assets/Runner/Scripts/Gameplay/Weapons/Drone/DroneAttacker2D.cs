/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ドローンの自動索敵および弾丸発射による攻撃制御コンポーネント。
 */

using System;
using UnityEngine;

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
        private const float DefaultSearchRadius = 8.0f;

        [Header("Attack Settings")]
        [Tooltip("発射する弾丸プレハブ")]
        [SerializeField] private DroneBullet bulletPrefab;

        [Tooltip("弾丸の発射起点 Transform")]
        [SerializeField] private Transform firePoint;

        [Tooltip("エネミーを自動検知する最大半径")]
        [SerializeField] private float searchRadius = DefaultSearchRadius;

        [Tooltip("基本攻撃力")]
        [SerializeField] private int attackPower = DefaultPower;

        [Tooltip("攻撃間隔（秒）")]
        [SerializeField] private float attackInterval = DefaultInterval;

        private float attackCooldownTimer;
        private SpriteRenderer spriteRenderer;

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

        /// <summary>攻撃実行時に発火するイベント</summary>
        public event Action OnAttack;

        /// <summary>
        /// コンポーネント参照を取得する。
        /// </summary>
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 毎フレームのクールダウンタイマー更新および自動索敵攻撃を行う。
        /// </summary>
        private void Update()
        {
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= Time.deltaTime;
            }

            if (CanAttack)
            {
                Attack();
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

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }

            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            var bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            bullet.Initialize(direction, attackPower);

            OnAttack?.Invoke();
        }

        /// <summary>
        /// 指定された基準位置から最も近い生存エネミーを検索して返す。
        /// </summary>
        /// <param name="origin">索敵の中心ワールド座標</param>
        /// <returns>検知された最も近い生存エネミーの Transform（未検知時は null）</returns>
        public Transform FindTarget(Vector3 origin)
        {
            var hits = Physics2D.OverlapCircleAll(origin, searchRadius);
            Transform closestEnemy = null;
            float closestDistSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                var enemy = hits[i].GetComponent<EnemyController>() ?? hits[i].GetComponentInParent<EnemyController>();
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
    }
}

