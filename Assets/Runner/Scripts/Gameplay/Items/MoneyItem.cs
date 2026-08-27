/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: フィールド上にドロップするお金（コイン/ポイント）アイテム。
 *                プレイヤーに近づくと加速度的に吸い込まれ、接触時に所持金を増加させます。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// お金・ポイントアイテムコンポーネント。
    /// IItem（回収処理）および IAttractable（プレイヤー吸引処理）を実装します。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class MoneyItem : MonoBehaviour, IItem, IAttractable
    {
        [Header("Money Settings")]
        [Tooltip("獲得できるお金・ポイントの額")]
        [SerializeField] private long moneyAmount = 50;

        [Header("Attract (Magnet) Settings")]
        [Tooltip("吸い込み初速")]
        [SerializeField] private float initialAttractSpeed = 6f;
        [Tooltip("吸い込み加速度")]
        [SerializeField] private float attractAcceleration = 18f;
        [Tooltip("最大吸い込み速度")]
        [SerializeField] private float maxAttractSpeed = 25f;

        [Header("Visual Bobbing Animation")]
        [SerializeField] private bool enableBobbing = true;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 3f;

        private Transform targetTransform;
        private bool isAttracted = false;
        private float currentAttractSpeed = 0f;
        private Vector3 spawnPosition;
        private float bobTimer = 0f;
        private bool isCollected = false;

        public DropItemType ItemType => DropItemType.Money;
        public long MoneyAmount => moneyAmount;
        public bool IsAttracted => isAttracted;
        public Transform Target => targetTransform;

        public event Action<MoneyItem, GameObject> OnItemCollected;

        private void Awake()
        {
            spawnPosition = transform.position;
            bobTimer = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            // コライダーが Trigger になっていることを保証
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // 1. 吸引状態の移動処理 (AttractTo が呼ばれた時のみ実行)
            if (isAttracted && targetTransform != null)
            {
                currentAttractSpeed = Mathf.Min(currentAttractSpeed + (attractAcceleration * Time.deltaTime), maxAttractSpeed);
                var dir = (targetTransform.position - transform.position).normalized;
                transform.position += dir * (currentAttractSpeed * Time.deltaTime);

                // ターゲットに極めて近接した場合は即時回収
                if (Vector2.Distance(transform.position, targetTransform.position) < 0.35f)
                {
                    Collect(targetTransform.gameObject);
                }
            }
            else
            {
                // 2. 非吸引時: ふんわり浮遊アニメーション
                if (enableBobbing)
                {
                    bobTimer += Time.deltaTime * bobSpeed;
                    float offsetY = Mathf.Sin(bobTimer) * bobHeight;
                    transform.position = spawnPosition + new Vector3(0f, offsetY, 0f);
                }
            }
        }

        #region IAttractable Implementation

        /// <summary>
        /// ターゲットへの吸い寄せを開始する。
        /// </summary>
        public void AttractTo(Transform target, float initialSpeed = 0f)
        {
            if (target == null || isCollected) return;

            targetTransform = target;
            isAttracted = true;
            currentAttractSpeed = initialSpeed > 0f ? initialSpeed : initialAttractSpeed;
        }

        /// <summary>
        /// 吸引を中止する。
        /// </summary>
        public void StopAttract()
        {
            isAttracted = false;
            targetTransform = null;
            spawnPosition = transform.position;
        }

        #endregion

        #region IItem Implementation

        /// <summary>
        /// プレイヤーによって回収された際の処理。
        /// </summary>
        public void Collect(GameObject collector)
        {
            if (isCollected) return;
            isCollected = true;

            // お金の加算
            var moneyCollector = collector.GetComponent<IMoneyCollector>();
            if (moneyCollector != null)
            {
                moneyCollector.CollectMoney(moneyAmount);
            }
            else if (GameHUDView.Instance != null && GameHUDView.Instance.PointStepHUD != null)
            {
                GameHUDView.Instance.PointStepHUD.AddPoints(moneyAmount);
            }

            DebugLogger.Log($"[MoneyItem] コイン獲得！ +¥{moneyAmount} pt");

            OnItemCollected?.Invoke(this, collector);

            // 回収演出・破棄
            Destroy(gameObject);
        }

        #endregion

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;

            // プレイヤーとの接触判定
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponent<IMoneyCollector>() != null)
            {
                Collect(other.gameObject);
            }
        }

        /// <summary>
        /// アイテム金額・初期パラメータを設定する。
        /// </summary>
        public void Setup(long amount)
        {
            moneyAmount = Math.Max(1, amount);
        }
    }
}

