/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: フィールド上にドロップするお金（コイン/ポイント）アイテム。
 *                Attractable コンポーネントと協調動作し、接触時に所持金を増加させます。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// お金・ポイントアイテムコンポーネント。
    /// Attractable コンポーネント（MonoBehaviour）と協調動作し、非吸引時の浮遊演出および回収時の所持金加算処理を担当します。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class MoneyItem : MonoBehaviour, IItem
    {
        [Header("Money Settings")]
        [Tooltip("獲得できるお金・ポイントの額")]
        [SerializeField] private long moneyAmount = 50;

        [Header("Visual Bobbing Animation")]
        [SerializeField] private bool enableBobbing = true;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 3f;
        private IAttractable attractable;
        private Vector3 spawnPosition;
        private float bobTimer;
        private bool isCollected;
        public DropItemType ItemType => DropItemType.Money;
        public long MoneyAmount => moneyAmount;
        public IAttractable Attractable => attractable;
        public bool IsAttracted => attractable != null && attractable.IsAttracted;

        public event Action<MoneyItem, GameObject> OnItemCollected;
        /// <summary>
        /// IAttractable コンポーネントの取得、イベント購読、および初期座標の記録を行う。
        /// </summary>
        private void Awake()
        {
            attractable = GetComponent<IAttractable>();
            spawnPosition = transform.position;
            bobTimer = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (attractable != null)
            {
                attractable.OnAttractReached += HandleAttractReached;
            }
        }

        /// <summary>
        /// 非吸引時のみふんわり浮遊アニメーションを実行する。
        /// </summary>
        private void Update()
        {
            if (isCollected) return;

            // 吸引中であれば浮遊処理は一切行わず Attractable に任せる
            if (attractable != null && attractable.IsAttracted)
            {
                return;
            }

            if (enableBobbing)
            {
                bobTimer += Time.deltaTime * bobSpeed;
                float offsetY = Mathf.Sin(bobTimer) * bobHeight;
                transform.position = spawnPosition + new Vector3(0f, offsetY, 0f);
            }
        }

        /// <summary>
        /// プレイヤー等のコレクターとの接触時に自動回収を実行する。
        /// </summary>
        /// <param name="other">接触したコライダー</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponent<IMoneyCollector>() != null)
            {
                Collect(other.gameObject);
            }
        }

        /// <summary>
        /// オブジェクト破棄時にイベント購読を解除する。
        /// </summary>
        private void OnDestroy()
        {
            if (attractable != null)
            {
                attractable.OnAttractReached -= HandleAttractReached;
            }
        }
        /// <summary>
        /// プレイヤーによって回収された際の処理を実行し、GameObject を破棄する。
        /// </summary>
        /// <param name="collector">回収したプレイヤー等の GameObject</param>
        public void Collect(GameObject collector)
        {
            if (isCollected) return;
            isCollected = true;

            var moneyCollector = collector.GetComponent<IMoneyCollector>();
            if (moneyCollector != null)
            {
                moneyCollector.CollectMoney(moneyAmount);
            }
            else if (PlayerController.Instance != null)
            {
                PlayerController.Instance.CollectMoney(moneyAmount);
            }

            DebugLogger.Log($"[MoneyItem] コイン獲得！ +¥{moneyAmount} pt");

            OnItemCollected?.Invoke(this, collector);
            Destroy(gameObject);
        }

        /// <summary>
        /// アイテム金額を設定する。
        /// </summary>
        /// <param name="amount">獲得金額</param>
        public void Setup(long amount)
        {
            moneyAmount = Math.Max(1, amount);
        }
        /// <summary>
        /// Attractable コンポーネントがターゲットへ到達した際のコールバックを処理する。
        /// </summary>
        /// <param name="target">到達先ターゲット</param>
        private void HandleAttractReached(Transform target)
        {
            if (target != null)
            {
                Collect(target.gameObject);
            }
        }
    }
}
